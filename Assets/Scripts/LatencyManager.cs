using Demo.Scripts.Runtime;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;



public struct Pair
{
    public float value;
    public System.DateTime time;
}

public class LatencyManager : NetworkBehaviour
{

    public float playerLatency;

    public bool ADTEnabled;

    public float lowLatency;
    public float highLatency;

    public float ADTKickInStepMagnitude;
    public float ADTLetOffStepMagnitude;

    public float lowLatencyTemp;
    public float highLatencyTemp;

    public float ADTKickInStepMagnitudeTemp;
    public float ADTLetOffStepMagnitudeTemp;

    public float ADTKickInTimer;
    public float ADTLetOffTimer;


    public float currentLatencyLow;
    public float currentLatencyHigh;

    public Queue<Pair> movementXQueue;
    public Queue<Pair> movementYQueue;

    public Queue<Pair> lookXQueue;
    public Queue<Pair> lookYQueue;

    public Queue<Pair> firePressedQueue;
    public Queue<Pair> fireReleasedQueue;

    public Queue<Pair> ADSPressedQueue;
    public Queue<Pair> ADSReleasedQueue;

    public Queue<Pair> leanLeftQueue;
    public Queue<Pair> leanRightQueue;

    public int currentRoundNumber = 0;
    public int totalRoundNumber = 0;

    public int currentRoundNumberTemp = 0;
    public int totalRoundNumberTemp = 0;

    public int shuffleIndex = -99;
    public int shuffleIndexTemp = -99;

    public NetworkVariable<float> currentRoundTimer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> currentSessionTimer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Start()
    {
        movementXQueue = new Queue<Pair>();
        movementYQueue = new Queue<Pair>();

        lookXQueue = new Queue<Pair>();
        lookYQueue = new Queue<Pair>();

        firePressedQueue = new Queue<Pair>();
        fireReleasedQueue = new Queue<Pair>();

        ADSPressedQueue = new Queue<Pair>();
        ADSReleasedQueue = new Queue<Pair>();

        leanLeftQueue = new Queue<Pair>();
        leanRightQueue = new Queue<Pair>();

        ClearAllQueue();
    }

    public void ClearAllQueue()
    {
        if (movementXQueue != null)
            movementXQueue.Clear();
        if (movementYQueue != null)
            movementYQueue.Clear();

        if (lookXQueue != null)
            lookXQueue.Clear();
        if (lookYQueue != null)
            lookYQueue.Clear();

        if (firePressedQueue != null)
            firePressedQueue.Clear();
        if (fireReleasedQueue != null)
            fireReleasedQueue.Clear();

        if (ADSPressedQueue != null)
            ADSPressedQueue.Clear();
        if (ADSReleasedQueue != null)
            ADSReleasedQueue.Clear();

        if (leanLeftQueue != null)
            leanLeftQueue.Clear();
        if (leanRightQueue != null)
            leanRightQueue.Clear();

        playerLatency = lowLatency;
        currentLatencyHigh = lowLatency;
        currentLatencyLow = lowLatency;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetLatConfigsServerRpc(float lowLatency,
     float highLatency,
     float ADTKickInDelay,
     float ADTLetOffDelay, int currentRoundNumber, int totalRoundNumber, string logfilename , int shuffleIndex)
    {
        SetLatConfigsClientRpc(lowLatency, highLatency, ADTKickInDelay, ADTLetOffDelay, currentRoundNumber, totalRoundNumber, logfilename, shuffleIndex);
    }

    [ClientRpc]
    public void SetLatConfigsClientRpc(float lowLatency,
     float highLatency,
     float ADTKickInDelay,
     float ADTLetOffDelay, int currentRoundNumber, int totalRoundNumber, string logfilename, int shuffleIndex, ClientRpcParams clientRpcParams = default)
    {
        SetLatConfigs(lowLatency, highLatency, ADTKickInDelay, ADTLetOffDelay, currentRoundNumber, totalRoundNumber, logfilename, shuffleIndex);
    }

    void SetLatConfigs(float lowLatency,
     float highLatency,
     float ADTKickInDelay,
     float ADTLetOffDelay,
     int currentRoundNumber, int totalRoundNumber, string logfilename, int shuffleIndex)
    {
        Debug.Log("round no " + currentRoundNumber + "client " + IsOwner);
        if (currentRoundNumber > 1)
        {
            this.gameObject.GetComponent<FPSController>().EnableQOEServerRpc();

            this.lowLatencyTemp = lowLatency;
            this.highLatencyTemp = highLatency;
            this.ADTKickInStepMagnitudeTemp = ADTKickInDelay;
            this.ADTLetOffStepMagnitudeTemp = ADTLetOffDelay;
            this.currentRoundNumberTemp = currentRoundNumber;
            this.totalRoundNumberTemp = totalRoundNumber;
            this.shuffleIndexTemp = shuffleIndex;
        }
        else
        {
            this.lowLatency = lowLatency;
            this.highLatency = highLatency;
            this.ADTKickInStepMagnitude = ADTKickInDelay;
            this.ADTLetOffStepMagnitude = ADTLetOffDelay;
            this.currentRoundNumber = currentRoundNumber;
            this.totalRoundNumber = totalRoundNumber;
            this.shuffleIndex = shuffleIndex;
            GetComponent<FPSController>().fileNameSuffix = logfilename;
            ClearAllQueue();
        }
    }

    public void SetLatConfigsFromTemp()
    {
        this.lowLatency = lowLatencyTemp;
        this.highLatency = highLatencyTemp;
        this.ADTKickInStepMagnitude = ADTKickInStepMagnitudeTemp;
        this.ADTLetOffStepMagnitude = ADTLetOffStepMagnitudeTemp;
        this.currentRoundNumber = currentRoundNumberTemp;
        this.totalRoundNumber = totalRoundNumberTemp;
        this.shuffleIndex = shuffleIndexTemp;
    }


    [ClientRpc]
    public void ResetCurrentRoundTimerClientRpc()
    {
        currentRoundTimer.Value = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log("lat :: " + playerLatency);

        if (IsOwner)
        {
            currentSessionTimer.Value += Time.deltaTime;
            currentRoundTimer.Value += Time.deltaTime;
        }

        StepADT();
    }


    void StepADT()
    {
        if (ADTEnabled)
        {
            if (playerLatency < highLatency)
            {
                playerLatency += ADTKickInStepMagnitude * Time.deltaTime;

                if (playerLatency > highLatency)
                    playerLatency = highLatency;
            }
            else
            {
                playerLatency = highLatency;
            }
        }
        else
        {
            if (playerLatency > lowLatency)
            {
                playerLatency -= ADTLetOffStepMagnitude * Time.deltaTime;

                if (playerLatency < lowLatency)
                    playerLatency = lowLatency;
            }
            else
            {
                playerLatency = lowLatency;
            }
        }
    }

    void TimedADT()
    {
        if (ADTEnabled)
        {
            ADTLetOffTimer = 0;
            if (ADTKickInTimer < ADTKickInStepMagnitude)
                ADTKickInTimer += Time.deltaTime;
            else
                ADTKickInTimer = ADTKickInStepMagnitude;

            if (ADTKickInStepMagnitude > 0)
            {
                playerLatency = currentLatencyLow + (highLatency - currentLatencyLow) * (ADTKickInTimer / ADTKickInStepMagnitude);
            }
            else
            {
                playerLatency = highLatency;
            }

            currentLatencyHigh = playerLatency;
        }
        else
        {
            ADTKickInTimer = 0;
            if (ADTLetOffTimer < ADTLetOffStepMagnitude)
                ADTLetOffTimer += Time.deltaTime;
            else
                ADTLetOffTimer = ADTLetOffStepMagnitude;

            if (ADTLetOffStepMagnitude > 0)
            {
                playerLatency = lowLatency + (currentLatencyHigh - lowLatency) * (1 - (ADTLetOffTimer / ADTLetOffStepMagnitude));
            }
            else
            {
                playerLatency = lowLatency;
            }
            currentLatencyLow = playerLatency;
        }
    }

}



