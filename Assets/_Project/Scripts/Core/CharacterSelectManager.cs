using UnityEngine;
using BIT.Data;

// ============================================================================
// CHARACTERSELECTMANAGER.CS — Persiste la elección de personaje entre escenas
// ============================================================================
// Singleton con DontDestroyOnLoad. El jugador elige en la pantalla de
// selección y PlayerController lo lee al inicializarse en la escena de juego.
// ============================================================================

namespace BIT.Core
{
    public class CharacterSelectManager : MonoBehaviour
    {
        public static CharacterSelectManager Instance { get; private set; }

        // Personaje seleccionado (null = usar stats por defecto del prefab)
        public CharacterData SelectedCharacter { get; private set; }

        // Nombre del jugador (para el ranking)
        public string PlayerName { get; private set; } = "Jugador";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SelectCharacter(CharacterData data)
        {
            SelectedCharacter = data;
            Debug.Log($"[CharacterSelect] Personaje elegido: {data.characterName}");
        }

        public void SetPlayerName(string name)
        {
            PlayerName = string.IsNullOrWhiteSpace(name) ? "Jugador" : name.Trim();
        }
    }
}
