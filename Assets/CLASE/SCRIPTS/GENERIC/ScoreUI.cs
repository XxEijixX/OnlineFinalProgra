using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : NetworkBehaviour
{
    [SerializeField] private Text player1ScoreText;
    [SerializeField] private Text player2ScoreText;

    public static ScoreUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void UpdateScores(int score1, int score2)
    {
        if (player1ScoreText != null)
            player1ScoreText.text = $"Jugador 1: {score1}";
        else
            Debug.LogWarning("Player1ScoreText es null");

        if (player2ScoreText != null)
            player2ScoreText.text = $"Jugador 2: {score2}";
        else
            Debug.LogWarning("Player2ScoreText es null");

        Debug.Log($"UI Actualizado - P1: {score1}, P2: {score2}");
    }
}
