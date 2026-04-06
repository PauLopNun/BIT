using UnityEngine;
using System.Collections.Generic;
using System.IO;

// ============================================================================
// SAVESYSTEM.CS - Sistema de guardado con JSON (Requisito Bonus 3.2)
// ============================================================================
// Este script implementa el sistema de ranking de jugadores usando JSON.
// Guarda el nombre del jugador y su puntuación máxima en un archivo local.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// JSON (JavaScript Object Notation) es un formato de texto estructurado
// muy usado para guardar datos. Unity tiene JsonUtility para convertir
// objetos C# a JSON y viceversa (serialización/deserialización).
//
// El archivo se guarda en Application.persistentDataPath, que es una
// carpeta específica del sistema operativo donde las aplicaciones
// pueden escribir datos que persisten entre sesiones.
// - Windows: %USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>
// - Android: /data/data/<package_name>/files
//
// Requisito 3.2: "Ranking de jugadores con nombre + puntuación guardados"
// ============================================================================

namespace BIT.Core
{
    /// <summary>
    /// Sistema de guardado y carga de datos del juego.
    /// Utiliza JSON para persistencia.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        // ====================================================================
        // SECCIÓN 1: SINGLETON
        // ====================================================================

        public static SaveSystem Instance { get; private set; }

        // ====================================================================
        // SECCIÓN 2: CONFIGURACIÓN
        // ====================================================================

        [Header("=== CONFIGURACIÓN ===")]
        [Tooltip("Nombre del archivo de guardado")]
        [SerializeField] private string _saveFileName = "bit_savedata.json";

        [Tooltip("Número máximo de entradas en el ranking")]
        [SerializeField] private int _maxRankingEntries = 10;

        // ====================================================================
        // SECCIÓN 3: DATOS A GUARDAR
        // ====================================================================

        // Datos cargados en memoria
        private SaveData _currentSaveData;

        // Ruta completa del archivo
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, _saveFileName);

        // ====================================================================
        // SECCIÓN 4: INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cargamos los datos al iniciar
            LoadData();
        }

        // ====================================================================
        // SECCIÓN 5: CARGA DE DATOS
        // ====================================================================

        /// <summary>
        /// Carga los datos del archivo JSON.
        /// Si no existe, crea datos nuevos.
        /// </summary>
        public void LoadData()
        {
            Debug.Log($"[SaveSystem] Intentando cargar desde: {SaveFilePath}");

            if (File.Exists(SaveFilePath))
            {
                try
                {
                    // Leemos el contenido del archivo
                    string jsonContent = File.ReadAllText(SaveFilePath);

                    // Convertimos JSON a objeto C#
                    _currentSaveData = JsonUtility.FromJson<SaveData>(jsonContent);

                    Debug.Log($"[SaveSystem] Datos cargados. Entradas en ranking: {_currentSaveData.ranking.Count}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SaveSystem] Error al cargar: {e.Message}");
                    CreateNewSaveData();
                }
            }
            else
            {
                Debug.Log("[SaveSystem] No existe archivo de guardado. Creando nuevo...");
                CreateNewSaveData();
            }
        }

        /// <summary>
        /// Crea una estructura de datos vacía.
        /// </summary>
        private void CreateNewSaveData()
        {
            _currentSaveData = new SaveData();
            _currentSaveData.ranking = new List<RankingEntry>();

            // Guardamos el archivo vacío
            SaveData();
        }

        // ====================================================================
        // SECCIÓN 6: GUARDADO DE DATOS
        // ====================================================================

        /// <summary>
        /// Guarda los datos actuales en el archivo JSON.
        /// </summary>
        public void SaveData()
        {
            try
            {
                // Convertimos el objeto a JSON formateado
                string jsonContent = JsonUtility.ToJson(_currentSaveData, true);

                // Escribimos en el archivo
                File.WriteAllText(SaveFilePath, jsonContent);

                Debug.Log($"[SaveSystem] Datos guardados en: {SaveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Error al guardar: {e.Message}");
            }
        }

        // ====================================================================
        // SECCIÓN 7: GESTIÓN DEL RANKING
        // ====================================================================

        /// <summary>
        /// Añade una nueva entrada al ranking.
        /// Ordena automáticamente y mantiene solo las mejores.
        /// </summary>
        /// <param name="playerName">Nombre del jugador</param>
        /// <param name="score">Puntuación obtenida</param>
        /// <returns>Posición en el ranking (1-based), o -1 si no entró</returns>
        public int AddRankingEntry(string playerName, int score)
        {
            // Creamos la nueva entrada
            RankingEntry newEntry = new RankingEntry
            {
                playerName = playerName,
                score = score,
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            // Añadimos a la lista
            _currentSaveData.ranking.Add(newEntry);

            // Ordenamos de mayor a menor puntuación
            _currentSaveData.ranking.Sort((a, b) => b.score.CompareTo(a.score));

            // Encontramos la posición de la nueva entrada
            int position = _currentSaveData.ranking.FindIndex(e =>
                e.playerName == newEntry.playerName &&
                e.score == newEntry.score &&
                e.date == newEntry.date) + 1;

            // Si hay más entradas del máximo, eliminamos las peores
            while (_currentSaveData.ranking.Count > _maxRankingEntries)
            {
                _currentSaveData.ranking.RemoveAt(_currentSaveData.ranking.Count - 1);
            }

            // Si la entrada quedó fuera del ranking
            if (position > _maxRankingEntries)
            {
                position = -1;
            }

            // Guardamos
            SaveData();

            Debug.Log($"[SaveSystem] Nueva entrada: {playerName} - {score} pts (Posición: {position})");

            return position;
        }

        /// <summary>
        /// Obtiene el ranking completo.
        /// </summary>
        public List<RankingEntry> GetRanking()
        {
            return _currentSaveData?.ranking ?? new List<RankingEntry>();
        }

        /// <summary>
        /// Obtiene la mejor puntuación registrada.
        /// </summary>
        public int GetHighScore()
        {
            if (_currentSaveData.ranking.Count > 0)
            {
                return _currentSaveData.ranking[0].score;
            }
            return 0;
        }

        /// <summary>
        /// Verifica si una puntuación entraría en el ranking.
        /// </summary>
        public bool WouldMakeRanking(int score)
        {
            if (_currentSaveData.ranking.Count < _maxRankingEntries)
            {
                return true;
            }

            int lowestScore = _currentSaveData.ranking[_currentSaveData.ranking.Count - 1].score;
            return score > lowestScore;
        }

        /// <summary>
        /// Limpia todo el ranking.
        /// </summary>
        public void ClearRanking()
        {
            _currentSaveData.ranking.Clear();
            SaveData();

            Debug.Log("[SaveSystem] Ranking limpiado");
        }

        // ====================================================================
        // SECCIÓN 8: DATOS DEL ÚLTIMO JUGADOR
        // ====================================================================

        /// <summary>
        /// Guarda el nombre del último jugador (para autocompletar).
        /// </summary>
        public void SetLastPlayerName(string name)
        {
            _currentSaveData.lastPlayerName = name;
            SaveData();
        }

        /// <summary>
        /// Obtiene el nombre del último jugador.
        /// </summary>
        public string GetLastPlayerName()
        {
            return _currentSaveData?.lastPlayerName ?? "";
        }

        // ====================================================================
        // SECCIÓN 9: UTILIDADES
        // ====================================================================

        /// <summary>
        /// Elimina todos los datos guardados.
        /// </summary>
        public void DeleteAllData()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveSystem] Archivo de guardado eliminado");
            }

            CreateNewSaveData();
        }

        /// <summary>
        /// Obtiene la ruta del archivo de guardado (útil para debug).
        /// </summary>
        public string GetSaveFilePath()
        {
            return SaveFilePath;
        }
    }

    // ========================================================================
    // CLASES DE DATOS SERIALIZABLES
    // ========================================================================
    // Estas clases definen la estructura de los datos que se guardan.
    // [System.Serializable] es NECESARIO para que JsonUtility funcione.

    /// <summary>
    /// Estructura principal de datos guardados.
    /// Contiene todo lo que se persiste entre sesiones.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        // Nombre del último jugador (para autocompletar)
        public string lastPlayerName;

        // Lista de entradas del ranking
        public List<RankingEntry> ranking;

        // Aquí podrías añadir más datos:
        // public float musicVolume;
        // public float sfxVolume;
        // public bool tutorialCompleted;
        // etc.
    }

    /// <summary>
    /// Una entrada individual del ranking.
    /// </summary>
    [System.Serializable]
    public class RankingEntry
    {
        // Nombre del jugador
        public string playerName;

        // Puntuación obtenida
        public int score;

        // Fecha en que se registró
        public string date;
    }
}
