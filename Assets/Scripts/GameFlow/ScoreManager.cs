using UnityEngine;

/// <summary>
/// Manages scoring in Dou Di Zhu.
/// Tracks bomb/rocket count during play, detects spring (chun tian),
/// and calculates final scores using: baseMultiplier * 2^(bombs + rockets + spring).
/// baseMultiplier comes from the grab landlord phase (1, 2, or 4).
/// Accumulates total scores across multiple games.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Number of bombs played this game (by any player)
    private int bombCount;

    // Number of rockets played this game
    private int rocketCount;

    // Base multiplier from grab landlord phase (1, 2, or 4)
    private int baseMultiplier;

    // Track how many times each player has played cards (for spring detection)
    private int[] playCountPerPlayer = new int[3];

    /// <summary>
    /// Cumulative total scores for each player across games.
    /// Positive = net winnings, negative = net losses.
    /// </summary>
    public int[] TotalScores { get; private set; } = new int[3];

    /// <summary>
    /// The score change for each player from the last completed game.
    /// Set after CalculateScores() is called.
    /// </summary>
    public int[] LastRoundScores { get; private set; } = new int[3];

    /// <summary>
    /// Resets per-game tracking for a new game. Call at the start of playing phase.
    /// Accepts the multiplier from the grab landlord phase (1, 2, or 4).
    /// </summary>
    public void ResetForNewGame(int multiplier)
    {
        baseMultiplier = multiplier;
        bombCount = 0;
        rocketCount = 0;
        playCountPerPlayer = new int[3];
        LastRoundScores = new int[3];

        Debug.Log($"ScoreManager reset. Base multiplier: x{baseMultiplier}");
    }

    /// <summary>
    /// Records a combo that was played. Increments bomb/rocket counters
    /// and tracks per-player play count for spring detection.
    /// Call this from TurnManager whenever a valid play is made.
    /// </summary>
    public void RecordPlay(int playerIndex, CardCombo combo)
    {
        playCountPerPlayer[playerIndex]++;

        if (combo.Type == ComboType.Bomb)
        {
            bombCount++;
            Debug.Log($"Bomb played! Total bombs: {bombCount}");
        }
        else if (combo.Type == ComboType.Rocket)
        {
            rocketCount++;
            Debug.Log($"Rocket played! Total rockets: {rocketCount}");
        }
    }

    /// <summary>
    /// Calculates the final multiplier: 2^(bombs + rockets + spring).
    /// </summary>
    private int CalculateMultiplier(bool isSpring)
    {
        int exponent = bombCount + rocketCount + (isSpring ? 1 : 0);
        // Use bit shift for power of 2
        return 1 << exponent;
    }

    /// <summary>
    /// Detects spring (chun tian):
    /// - Landlord spring: landlord plays all cards and neither farmer played at all.
    /// - Farmer spring: one farmer finishes and the landlord never played after the first turn.
    ///   (Simplified: landlord only played once = farmer spring.)
    /// </summary>
    private bool DetectSpring(int winnerIndex)
    {
        Player winner = GameManager.Instance.Players[winnerIndex];

        if (winner.IsLandlord)
        {
            // Landlord spring: neither farmer played any cards
            for (int i = 0; i < 3; i++)
            {
                if (i != winnerIndex && playCountPerPlayer[i] > 0)
                    return false;
            }
            Debug.Log("Spring detected! Landlord played all cards without farmers playing.");
            return true;
        }
        else
        {
            // Farmer spring: landlord only played once (the mandatory first lead)
            int landlordIndex = -1;
            for (int i = 0; i < 3; i++)
            {
                if (GameManager.Instance.Players[i].IsLandlord)
                {
                    landlordIndex = i;
                    break;
                }
            }

            if (landlordIndex >= 0 && playCountPerPlayer[landlordIndex] <= 1)
            {
                Debug.Log("Spring detected! Farmer won before landlord could play more than once.");
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Calculates and applies scores for all players after a game ends.
    /// Landlord wins: each farmer pays (baseMultiplier * multiplier) to landlord.
    /// Farmer wins: landlord pays (baseMultiplier * multiplier) to each farmer.
    /// </summary>
    public void CalculateScores(int winnerIndex)
    {
        Player winner = GameManager.Instance.Players[winnerIndex];
        bool isSpring = DetectSpring(winnerIndex);
        int multiplier = CalculateMultiplier(isSpring);
        int pointsPerFarmer = baseMultiplier * multiplier;

        // Find landlord index
        int landlordIndex = -1;
        for (int i = 0; i < 3; i++)
        {
            if (GameManager.Instance.Players[i].IsLandlord)
            {
                landlordIndex = i;
                break;
            }
        }

        if (winner.IsLandlord)
        {
            // Landlord wins: gains from both farmers
            LastRoundScores[landlordIndex] = pointsPerFarmer * 2;
            for (int i = 0; i < 3; i++)
            {
                if (i != landlordIndex)
                    LastRoundScores[i] = -pointsPerFarmer;
            }
        }
        else
        {
            // Farmers win: landlord pays both farmers
            LastRoundScores[landlordIndex] = -pointsPerFarmer * 2;
            for (int i = 0; i < 3; i++)
            {
                if (i != landlordIndex)
                    LastRoundScores[i] = pointsPerFarmer;
            }
        }

        // Accumulate into totals
        for (int i = 0; i < 3; i++)
        {
            TotalScores[i] += LastRoundScores[i];
        }

        Debug.Log($"Score result: baseMultiplier=x{baseMultiplier}, playMultiplier={multiplier} (bombs={bombCount}, rockets={rocketCount}, spring={isSpring})");
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"  {GameManager.Instance.Players[i].Name}: {FormatScoreChange(LastRoundScores[i])} (total: {TotalScores[i]})");
        }
    }

    /// <summary>
    /// Formats a score change with +/- prefix for display.
    /// </summary>
    public static string FormatScoreChange(int score)
    {
        return score >= 0 ? $"+{score}" : $"{score}";
    }
}
