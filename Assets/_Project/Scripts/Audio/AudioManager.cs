using UnityEngine;
using System.Collections.Generic;
using BIT.Events;

// ============================================================================
// AUDIOMANAGER.CS - Gestor centralizado de audio (Requisito 2.9)
// ============================================================================
// Este script gestiona TODOS los sonidos del juego de forma centralizada.
// Cualquier sistema que necesite reproducir un sonido pasa por aquí.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// El AudioManager usa el patrón SINGLETON para que haya una única instancia
// accesible desde cualquier parte del código. También se suscribe a los
// GameEvents para reproducir sonidos automáticamente cuando ocurren eventos.
//
// Ventajas de centralizar el audio:
// 1. Control del volumen global desde un solo punto
// 2. Evitar sonidos superpuestos o saturados
// 3. Pool de AudioSources para optimización
// 4. Fácil de desactivar para tests
//
// Requisito 2.9: "El videojuego debe incorporar audio asociado a eventos"
// ============================================================================

namespace BIT.Audio
{
    /// <summary>
    /// Gestor central de audio del juego.
    /// Patrón Singleton con pool de AudioSources.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ====================================================================
        // SECCIÓN 1: SINGLETON
        // ====================================================================

        /// <summary>
        /// Instancia única del AudioManager
        /// </summary>
        public static AudioManager Instance { get; private set; }

        // ====================================================================
        // SECCIÓN 2: CONFIGURACIÓN DE VOLUMEN
        // ====================================================================

        [Header("=== CONTROL DE VOLUMEN ===")]
        [Tooltip("Volumen maestro (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 1f;

        [Tooltip("Volumen de efectos de sonido (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _sfxVolume = 1f;

        [Tooltip("Volumen de música (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _musicVolume = 0.7f;

        // ====================================================================
        // SECCIÓN 3: CLIPS DE AUDIO
        // ====================================================================

        [Header("=== SONIDOS DEL JUGADOR ===")]
        [Tooltip("Sonido al recibir daño")]
        [SerializeField] private AudioClip _playerHurtSound;

        [Tooltip("Sonido al atacar")]
        [SerializeField] private AudioClip _playerAttackSound;

        [Tooltip("Sonido de pasos")]
        [SerializeField] private AudioClip _footstepSound;

        [Tooltip("Sonido al morir")]
        [SerializeField] private AudioClip _playerDeathSound;

        [Header("=== SONIDOS DE OBJETOS ===")]
        [Tooltip("Sonido al recoger item")]
        [SerializeField] private AudioClip _pickupSound;

        [Tooltip("Sonido al empujar caja")]
        [SerializeField] private AudioClip _pushSound;

        [Tooltip("Sonido de moneda")]
        [SerializeField] private AudioClip _coinSound;

        [Header("=== SONIDOS DE UI ===")]
        [Tooltip("Sonido de clic en botón")]
        [SerializeField] private AudioClip _buttonClickSound;

        [Tooltip("Sonido de menú")]
        [SerializeField] private AudioClip _menuSound;

        [Header("=== MÚSICA ===")]
        [Tooltip("Música de fondo del juego")]
        [SerializeField] private AudioClip _backgroundMusic;

        [Tooltip("Música de Game Over")]
        [SerializeField] private AudioClip _gameOverMusic;

        [Header("=== EVENTOS A ESCUCHAR ===")]
        [Tooltip("Evento de daño al jugador")]
        [SerializeField] private GameEventSO _onPlayerDamageEvent;

        [Tooltip("Evento de ataque del jugador")]
        [SerializeField] private GameEventSO _onPlayerAttackEvent;

        [Tooltip("Evento de recoger objeto")]
        [SerializeField] private GameEventSO _onPickupEvent;

        // ====================================================================
        // SECCIÓN 4: POOL DE AUDIOSOURCES
        // ====================================================================

        [Header("=== CONFIGURACIÓN DEL POOL ===")]
        [Tooltip("Número de AudioSources en el pool")]
        [SerializeField] private int _poolSize = 10;

        private List<AudioSource> _audioSourcePool;
        private AudioSource _musicSource;
        private int _currentPoolIndex = 0;

        // ====================================================================
        // SECCIÓN 5: INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Mantenemos el AudioManager entre escenas
            DontDestroyOnLoad(gameObject);

            // Inicializamos el pool de AudioSources
            InitializeAudioPool();

            // Creamos el AudioSource para música
            CreateMusicSource();
        }

        private void Start()
        {
            // Suscribimos a los eventos
            SubscribeToEvents();

            // Iniciamos la música de fondo
            PlayBackgroundMusic();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Crea el pool de AudioSources para efectos de sonido.
        /// Un pool evita crear/destruir objetos constantemente.
        /// </summary>
        private void InitializeAudioPool()
        {
            _audioSourcePool = new List<AudioSource>();

            for (int i = 0; i < _poolSize; i++)
            {
                GameObject sourceObj = new GameObject($"AudioSource_Pool_{i}");
                sourceObj.transform.SetParent(transform);

                AudioSource source = sourceObj.AddComponent<AudioSource>();
                source.playOnAwake = false;

                _audioSourcePool.Add(source);
            }

            Debug.Log($"[AudioManager] Pool inicializado con {_poolSize} AudioSources");
        }

        /// <summary>
        /// Crea el AudioSource dedicado para música de fondo.
        /// </summary>
        private void CreateMusicSource()
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);

            _musicSource = musicObj.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.priority = 0; // Máxima prioridad
        }

        // ====================================================================
        // SECCIÓN 6: SUSCRIPCIÓN A EVENTOS
        // ====================================================================

        /// <summary>
        /// Se suscribe a los GameEvents para reproducir sonidos automáticamente.
        /// </summary>
        private void SubscribeToEvents()
        {
            // Daño al jugador
            if (_onPlayerDamageEvent != null)
            {
                _onPlayerDamageEvent.RegisterListener(OnPlayerDamage);
            }

            // Ataque del jugador
            if (_onPlayerAttackEvent != null)
            {
                _onPlayerAttackEvent.RegisterListener(OnPlayerAttack);
            }

            // Recoger objeto
            if (_onPickupEvent != null)
            {
                _onPickupEvent.RegisterListener(OnPickup);
            }

            Debug.Log("[AudioManager] Suscrito a eventos del juego");
        }

        private void UnsubscribeFromEvents()
        {
            if (_onPlayerDamageEvent != null)
            {
                _onPlayerDamageEvent.UnregisterListener(OnPlayerDamage);
            }

            if (_onPlayerAttackEvent != null)
            {
                _onPlayerAttackEvent.UnregisterListener(OnPlayerAttack);
            }

            if (_onPickupEvent != null)
            {
                _onPickupEvent.UnregisterListener(OnPickup);
            }
        }

        // ====================================================================
        // SECCIÓN 7: CALLBACKS DE EVENTOS
        // ====================================================================

        private void OnPlayerDamage()
        {
            PlaySFX(_playerHurtSound);
        }

        private void OnPlayerAttack()
        {
            PlaySFX(_playerAttackSound);
        }

        private void OnPickup()
        {
            PlaySFX(_pickupSound);
        }

        // ====================================================================
        // SECCIÓN 8: REPRODUCCIÓN DE EFECTOS DE SONIDO
        // ====================================================================

        /// <summary>
        /// Reproduce un efecto de sonido usando el pool.
        /// </summary>
        /// <param name="clip">AudioClip a reproducir</param>
        /// <param name="volumeMultiplier">Multiplicador de volumen (0-1)</param>
        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            // Obtenemos un AudioSource del pool
            AudioSource source = GetAvailableAudioSource();

            if (source != null)
            {
                source.clip = clip;
                source.volume = _masterVolume * _sfxVolume * volumeMultiplier;
                source.pitch = 1f;
                source.Play();
            }
        }

        /// <summary>
        /// Reproduce un efecto con pitch aleatorio (variación natural).
        /// </summary>
        public void PlaySFXWithRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableAudioSource();

            if (source != null)
            {
                source.clip = clip;
                source.volume = _masterVolume * _sfxVolume;
                source.pitch = Random.Range(minPitch, maxPitch);
                source.Play();
            }
        }

        /// <summary>
        /// Reproduce un sonido en una posición 3D específica.
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, _masterVolume * _sfxVolume * volumeMultiplier);
        }

        /// <summary>
        /// Obtiene un AudioSource disponible del pool (round-robin).
        /// </summary>
        private AudioSource GetAvailableAudioSource()
        {
            // Sistema round-robin: vamos usando cada AudioSource en orden
            AudioSource source = _audioSourcePool[_currentPoolIndex];

            // Avanzamos al siguiente índice
            _currentPoolIndex = (_currentPoolIndex + 1) % _poolSize;

            return source;
        }

        // ====================================================================
        // SECCIÓN 9: MÚSICA
        // ====================================================================

        /// <summary>
        /// Inicia la música de fondo.
        /// </summary>
        public void PlayBackgroundMusic()
        {
            if (_backgroundMusic == null || _musicSource == null) return;

            _musicSource.clip = _backgroundMusic;
            _musicSource.volume = _masterVolume * _musicVolume;
            _musicSource.Play();

            Debug.Log("[AudioManager] Música de fondo iniciada");
        }

        /// <summary>
        /// Cambia la música de fondo con fade.
        /// </summary>
        public void ChangeMusic(AudioClip newMusic, float fadeDuration = 1f)
        {
            StartCoroutine(CrossfadeMusic(newMusic, fadeDuration));
        }

        /// <summary>
        /// Crossfade entre la música actual y la nueva.
        /// </summary>
        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newMusic, float duration)
        {
            float startVolume = _musicSource.volume;
            float elapsed = 0f;

            // Fade out
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration / 2f));
                yield return null;
            }

            // Cambiamos el clip
            _musicSource.clip = newMusic;
            _musicSource.Play();

            // Fade in
            elapsed = 0f;
            float targetVolume = _masterVolume * _musicVolume;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (duration / 2f));
                yield return null;
            }
        }

        /// <summary>
        /// Reproduce la música de Game Over.
        /// </summary>
        public void PlayGameOverMusic()
        {
            if (_gameOverMusic != null)
            {
                ChangeMusic(_gameOverMusic, 0.5f);
            }
        }

        /// <summary>
        /// Pausa/reanuda la música.
        /// </summary>
        public void ToggleMusic(bool play)
        {
            if (play)
            {
                _musicSource.UnPause();
            }
            else
            {
                _musicSource.Pause();
            }
        }

        // ====================================================================
        // SECCIÓN 10: CONTROL DE VOLUMEN
        // ====================================================================

        /// <summary>
        /// Establece el volumen maestro.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        /// <summary>
        /// Establece el volumen de efectos.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Establece el volumen de música.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        private void UpdateMusicVolume()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = _masterVolume * _musicVolume;
            }
        }

        // ====================================================================
        // SECCIÓN 11: MÉTODOS DE CONVENIENCIA
        // ====================================================================

        /// <summary>
        /// Reproduce el sonido de clic de botón.
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySFX(_buttonClickSound);
        }

        /// <summary>
        /// Reproduce el sonido de moneda.
        /// </summary>
        public void PlayCoinSound()
        {
            PlaySFXWithRandomPitch(_coinSound);
        }

        /// <summary>
        /// Reproduce el sonido de pasos.
        /// </summary>
        public void PlayFootstep()
        {
            PlaySFXWithRandomPitch(_footstepSound, 0.8f, 1.2f);
        }
    }
}
