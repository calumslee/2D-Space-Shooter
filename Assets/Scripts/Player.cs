using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    [Header("Health and Damage")]
    [SerializeField]
    private int _playerLives = 3;
    [SerializeField]
    private int _maxLives = 3;
    private bool _isShieldActive;
    [SerializeField]
    private GameObject _shieldVisual;
    [SerializeField]
    private int _shieldStrength;
    [SerializeField]
    private int _maxShieldStrength = 3;
    [SerializeField]
    private GameObject[] _damageVisuals;
    [SerializeField]
    private GameObject _explosionPrefab;

    [Header("Score")]
    [SerializeField]
    private int _score;

    [Header("Speed")]
    [SerializeField]
    private float _speed = 4.0f;
    [SerializeField]
    private float _speedMultiplier = 2.0f;
    [SerializeField]
    private float _speedBoostCooldown = 5.0f;
    [SerializeField]
    private GameObject _speedVisual;
    private bool _hasStalled;
    [SerializeField]
    private float _stallTime = 5.0f;

    [SerializeField]
    private GameObject _thrusterVisual;
    [SerializeField]
    private float _thrusterMultiplier = 1.5f;
    private float _thrusterLevel = 5.0f;
    [SerializeField]
    private float _thrusterCooldown = 5.0f;
    private bool _thrusterOn;

    [Header("Lasers")]
    [SerializeField]
    private int _ammoCount = 15;
    [SerializeField]
    private int _maxAmmo = 30;
    [SerializeField]
    private float _fireRate = 0.5f;
    private float _canFire = -1.0f;

    [SerializeField]
    private GameObject _laserContainer;
    [SerializeField]
    private GameObject _laserPrefab;
    private Vector3 _laserOffset = new Vector3(0f, 1.0f, 0f);

    [SerializeField]
    private GameObject _tripleShotPrefab;
    private bool _isTripleShotActive;
    [SerializeField]
    private float _tripleShotCooldown = 5.0f;

    [SerializeField]
    private GameObject _superShot;
    [SerializeField]
    private float _superShotCooldown = 5.0f;

    [SerializeField]
    private GameObject[] _missileHolders;
    [SerializeField]
    private GameObject _missilePrefab;
    private List<GameObject> _activeMissiles = new List<GameObject>();

    [Header("Audio")]
    [SerializeField]
    private AK.Wwise.Event _noAmmoAudio;
    [SerializeField]
    private AK.Wwise.Event _superLaserAudio, _stopSuperLaserAudio;
    [SerializeField]
    private AK.Wwise.Event _healthAudio;
    [SerializeField]
    private AK.Wwise.Event _explosionAudio;
    [SerializeField]
    private AK.Wwise.Event _thrusterAudio, _stopThrusterAudio;
    [SerializeField]
    private AK.Wwise.Event _speedAudio, _stopSpeedAudio;
    [SerializeField]
    private AK.Wwise.Event _shieldAudio, _stopShieldAudio;

    [SerializeField]
    private AK.Wwise.Event _stopSFX;

    [Header("Misc")]
    [SerializeField]
    private GameObject _powerupContainer;
    private float _magnetLevel = 3.0f;
    private float _magnetCooldown = 7.0f;
    private bool _magnetOn;

    private SpawnManager _spawnManager;
    private UIManager _uiManager;
    private CameraShake _cameraShake;

    private void Start()
    {
        transform.position = new Vector3(0, -3, 0);

        _spawnManager = GameObject.Find("SpawnManager").GetComponent<SpawnManager>();
        _uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        _cameraShake = GameObject.Find("Main Camera").GetComponent<CameraShake>();

        if (_spawnManager == null)
        {
            Debug.LogError("Spawn Manager is Null!");
        }

        if (_uiManager == null)
        {
            Debug.LogError("UI Manager is Null!");
        }

        if (_cameraShake == null)
        {
            Debug.LogError("Camera Shake Script is Null!");
        }

        _uiManager.UpdateScoreUI(_score);
        _uiManager.UpdateAmmoUI(_ammoCount, _maxAmmo);
        _uiManager.UpdateThrusterUI(_thrusterLevel);
        _uiManager.UpdateMagnetUI(_magnetLevel);
    }

    private void Update()
    {
        MagnetCheck();

        if (_hasStalled == false)
        {
            Movement();
            ThrusterCheck();
        }

        if (Input.GetKeyDown(KeyCode.Space) && Time.time > _canFire)
        {
            WeaponSelect();
        }
    }

    private void Movement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontalInput, verticalInput, 0);

        transform.Translate(direction * _speed * Time.deltaTime);

        float _yClamp = Mathf.Clamp(transform.position.y, -3.0f, 2.5f);                               //vertical bounds
        transform.position = new Vector3(transform.position.x, _yClamp, 0);
        //horizontal wrapping
        if (transform.position.x >= 11.3f)
        {
            transform.position = new Vector3(-11.3f, transform.position.y, 0);
        }
        else if (transform.position.x <= -11.3f)
        {
            transform.position = new Vector3(11.3f, transform.position.y, 0);
        }
    }

    public void Stall()
    {
        StartCoroutine(StallPowerDownRoutine());
    }

    private IEnumerator StallPowerDownRoutine()
    {
        _hasStalled = true;
        _uiManager.EngineDisplayUI(_hasStalled);
        yield return new WaitForSeconds(_stallTime);
        _hasStalled = false;
        _uiManager.EngineDisplayUI(_hasStalled);
    }

    private void ThrusterCheck()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && _thrusterLevel > 0)
        {
            StopCoroutine(ThrusterOff());
            StartCoroutine(ThrusterOn());
        }
    }

    private IEnumerator ThrusterOn()
    {
        _thrusterOn = true;
        _speed *= _thrusterMultiplier;
        _thrusterAudio.Post(this.gameObject);
        _thrusterVisual.SetActive(true);

        while (Input.GetKey(KeyCode.LeftShift) && _thrusterLevel > 0)
        {
            yield return null;
            _thrusterLevel -= Time.deltaTime;
            _uiManager.UpdateThrusterUI(_thrusterLevel);
        }

        StartCoroutine(ThrusterOff());
    }

    private IEnumerator ThrusterOff()
    {
        _thrusterOn = false;
        _speed /= _thrusterMultiplier;
        _stopThrusterAudio.Post(this.gameObject);
        _thrusterVisual.SetActive(false);

        yield return new WaitForSeconds(_thrusterCooldown);
            
        while (_thrusterLevel < 5 && _thrusterOn == false)
        {
            yield return null;
            _thrusterLevel += Time.deltaTime;
            _uiManager.UpdateThrusterUI(_thrusterLevel);
        }
    }

    private void WeaponSelect()
    {
        if (_activeMissiles.Count > 0)
        {
            FireMissile();
        }
        else
        {
            FireLaser();
        }
    }

    public void AddScore(int _points)
    {
        _score += _points;
        _uiManager.UpdateScoreUI(_score);
    }

    public void MissilesActive()
    {
        foreach (GameObject _holder in _missileHolders)
        {
            if (_holder.GetComponentInChildren<Missile>() == null)
            {
                GameObject _missile = Instantiate(_missilePrefab, _holder.transform.position, Quaternion.identity);
                _activeMissiles.Add(_missile);
                _missile.transform.parent = _holder.transform;
            }
        }
    }

    private void FireMissile()
    {
        _canFire = Time.time + _fireRate;                                                           //Cooldown System

        GameObject _missileToFire = _activeMissiles[Random.Range(0, _activeMissiles.Count)];
        Missile _missile = _missileToFire.GetComponent<Missile>();
        _activeMissiles.Remove(_missileToFire);

        _missile.Fire();
    }

    private void FireLaser()
    {
        if (_ammoCount > 0)
        {
            Vector3 _laserPos = transform.position + _laserOffset;
            _canFire = Time.time + _fireRate;                                                           //Cooldown System

            if (_isTripleShotActive == true)
            {
                GameObject _tripleShot = Instantiate(_tripleShotPrefab, transform.position, Quaternion.identity);
                _tripleShot.transform.parent = _laserContainer.transform;                               //GameObject Nesting
            }
            else
            {
                GameObject _laser = Instantiate(_laserPrefab, _laserPos, Quaternion.identity);
                _laser.transform.parent = _laserContainer.transform;                                    //GameObject Nesting
            }

            _ammoCount--;
            _uiManager.UpdateAmmoUI(_ammoCount, _maxAmmo);
        }
        else
        {
            _canFire = Time.time + _fireRate;                                                           //Cooldown System
            _noAmmoAudio.Post(this.gameObject);
        }
    }

    public void AddAmmo(int _ammoPowerupCount)
    {
        _ammoCount += _ammoPowerupCount;

        if (_ammoCount >= _maxAmmo)
        {
            _ammoCount = _maxAmmo;
        }

        _uiManager.UpdateAmmoUI(_ammoCount, _maxAmmo);
    }

    public void TripleShotActive()
    {
        _isTripleShotActive = true;
        StartCoroutine(TripleShotPowerDownRoutine());
    }

    private IEnumerator TripleShotPowerDownRoutine()
    {
        yield return new WaitForSeconds(_tripleShotCooldown);
        _isTripleShotActive = false;
    }

    public void SuperShotActive()
    {
        StartCoroutine(SuperShotCoroutine());
    }

    private IEnumerator SuperShotCoroutine()
    {
        _superShot.SetActive(true);
        _superLaserAudio.Post(this.gameObject);
        yield return new WaitForSeconds(_superShotCooldown);
        _superShot.SetActive(false);
        _stopSuperLaserAudio.Post(this.gameObject);
    }

    public void SpeedBoostActive()
    {
        _speed *= _speedMultiplier;
        _speedVisual.SetActive(true);
        _speedAudio.Post(this.gameObject);
        StartCoroutine(SpeedBoostPowerDownRoutine());
    }

    private IEnumerator SpeedBoostPowerDownRoutine()
    {
        yield return new WaitForSeconds(_speedBoostCooldown);
        _speed /= _speedMultiplier;
        _speedVisual.SetActive(false);
        _stopSpeedAudio.Post(this.gameObject);
    }

    private void MagnetCheck()
    {
        if (Input.GetKeyDown(KeyCode.C) && _magnetLevel > 0)
        {
            StopCoroutine(MagnetOff());
            StartCoroutine(MagnetOn());
        }
    }

    private IEnumerator MagnetOn()
    {
        _magnetOn = true;
        
        Powerup[] _powerups = _powerupContainer.GetComponentsInChildren<Powerup>();

        while (Input.GetKey(KeyCode.C) && _magnetLevel > 0)
        {
            yield return null;
            _magnetLevel -= Time.deltaTime;
            _uiManager.UpdateMagnetUI(_magnetLevel);

            foreach (Powerup _powerup in _powerups)
            {
                if (_powerup != null)
                {
                    _powerup.MagnetActive(true);
                }
            }
        }

        foreach (Powerup _powerup in _powerups)
        {
            if (_powerup != null)
            {
                _powerup.MagnetActive(false);
            }
        }

        StartCoroutine(MagnetOff());
    }

    private IEnumerator MagnetOff()
    {
        _magnetOn = false;

        yield return new WaitForSeconds(_magnetCooldown);

        while (_magnetLevel < 3 && _magnetOn == false)
        {
            yield return null;
            _magnetLevel += Time.deltaTime;
            _uiManager.UpdateMagnetUI(_magnetLevel);
        }
    }

    public void ShieldStrength(int _strength)
    {        
        _shieldStrength += _strength;

        if (_shieldStrength <= _maxShieldStrength)
        {
            _stopShieldAudio.Post(this.gameObject);
            if (_shieldStrength >= 1)
            {
                _shieldAudio.Post(this.gameObject);
            }

            switch (_shieldStrength)
            {
                case 0:
                    _isShieldActive = false;
                    _shieldVisual.SetActive(false);
                    break;
                case 1:
                    _isShieldActive = true;
                    _shieldVisual.SetActive(true);
                    _shieldVisual.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    break;
                case 2:
                    _shieldVisual.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
                    break;
                case 3:
                    _shieldVisual.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
                    break;
                default:
                    _shieldStrength = 3;
                    break;
            }
        }
        else if (_shieldStrength > _maxShieldStrength)
        {
            _shieldStrength = _maxShieldStrength;
        }
    }

    public void AddHealth(int _healthPowerupCount)
    {
        if (_playerLives < _maxLives)
        {
            _playerLives += _healthPowerupCount;

            if (_playerLives >= _maxLives)
            {
                _playerLives = _maxLives;
            }

            _healthAudio.Post(this.gameObject);
            DamageVisuals();
            _uiManager.UpdateLivesUI(_playerLives);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Enemy Laser" || other.tag == "Enemy Missile" || other.tag == "Rear Laser" || other.tag == "Mine")
        {
            Destroy(other.gameObject);
            _explosionAudio.Post(this.gameObject);
            TakeDamage();
        }
    }
    
    public void TakeDamage()
    {
        if (_isShieldActive == true)
        {
            ShieldStrength(-1);
            return;
        }

        _cameraShake.TakeDamage();
        _playerLives--;
        DamageVisuals();
        _uiManager.UpdateLivesUI(_playerLives);
    }

    private void DamageVisuals()
    {
        switch (_playerLives)
        {
            default:
                _damageVisuals[1].SetActive(false);
                _damageVisuals[0].SetActive(false);
                break;
            case 2:
                _damageVisuals[1].SetActive(true);
                _damageVisuals[0].SetActive(false);
                break;
            case 1:
                _damageVisuals[1].SetActive(true);
                _damageVisuals[0].SetActive(true);
                break;
            case 0:
                _spawnManager.OnPlayerDeath();
                _stopSFX.Post(this.gameObject);
                Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
                Destroy(this.gameObject);
                break;
        }
    }
}
