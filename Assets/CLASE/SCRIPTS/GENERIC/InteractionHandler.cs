using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionHandler : NetworkBehaviour
{
    [Networked] private float TimeRemaining { get; set; }
    [Networked] private bool IsTimerActive { get; set; }
    [Networked] private bool GameEnded { get; set; }

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private GameManager gameManager;

    private static InteractionHandler instance;

    public override void Spawned()
    {
        // Solo el primer objeto spawneado (del host) maneja el timer
        if (Object.HasStateAuthority && instance == null)
        {
            instance = this;
            GameEnded = false;

            // Buscar el texto del timer en la escena
            if (timerText == null)
            {
                GameObject timerObject = GameObject.Find("TimerText");
                if (timerObject != null)
                {
                    timerText = timerObject.GetComponent<TextMeshProUGUI>();
                }
            }

            // Buscar el texto del ganador
            if (winnerText == null)
            {
                GameObject winnerObject = GameObject.Find("WinnerText");
                if (winnerObject != null)
                {
                    winnerText = winnerObject.GetComponent<TextMeshProUGUI>();
                    winnerText.text = ""; // Inicialmente vacío
                }
            }

            // Buscar el GameManager
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Solo la instancia principal (del host) maneja el timer
        if (this != instance || !Object.HasStateAuthority)
            return;

        // Si el timer está activo, reducir el tiempo
        if (IsTimerActive && !GameEnded)
        {
            TimeRemaining -= Runner.DeltaTime;

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsTimerActive = false;
                GameEnded = true;

                // Anunciar ganador
                AnnounceWinner();
            }
        }
    }

    public override void Render()
    {
        // Solo el host puede iniciar el timer (removemos la condición de InputAuthority)
        if (this == instance && Object.HasStateAuthority)
        {
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                if (!IsTimerActive && !GameEnded)
                {
                    TimeRemaining = 20f;
                    IsTimerActive = true;
                }
            }
        }

        // Todos actualizan el texto si son la instancia principal
        if (this == instance)
        {
            UpdateTimerDisplay();
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void AnnounceWinner()
    {
        if (gameManager == null || !Object.HasStateAuthority)
            return;

        int scoreP1 = gameManager.GetScorePlayerOne();
        int scoreP2 = gameManager.GetScorePlayerTwo();

        string winnerMessage = "";

        if (scoreP1 > scoreP2)
        {
            winnerMessage = "¡Jugador 1 Gana!";
        }
        else if (scoreP2 > scoreP1)
        {
            winnerMessage = "¡Jugador 2 Gana!";
        }
        else
        {
            winnerMessage = "¡Empate!";
        }

        // Usar RPC para anunciar a todos los clientes
        RPC_AnnounceWinner(winnerMessage);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceWinner(string message)
    {
        if (winnerText != null)
        {
            winnerText.text = message;
            Debug.Log($"🏆 {message}");
        }
    }

    private void OnDestroy()
    {
        if (this == instance)
        {
            instance = null;
        }
    }
}