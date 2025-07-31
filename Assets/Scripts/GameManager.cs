using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Player player;
    public int[] roundsPointsRequirement;
    public int pointsThisRound;
    public int roundNumber;
    public bool roundInProgress;
    public float Roundduration = 20f;



    public void StartRound()
    {
        roundNumber++;
        pointsThisRound = 0;
        roundInProgress = true;
    }



}