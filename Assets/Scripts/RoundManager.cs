using Demo.Scripts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct PlayerConfigs
{
    public List<float> lowLatencyArray;
    public List<float> highLatencyArray;

    public List<float> ADTKickInDelayArray;
    public List<float> ADTLetOffDelayArray;
}

public class RoundManager : NetworkBehaviour
{
    public float roundDuration;
    public int numberOfRounds;
    public int currentRound;

    public PlayerConfigs player1Configs;
    public PlayerConfigs player2Configs;

    [HideInInspector]
    public NetworkVariable<float> roundTimer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    bool initialRoundConfigsTransferred;
    NetworkObject player1GO = null;
    NetworkObject player2GO = null;

    string logfilename = string.Empty;

    List <int> indexArray = new List<int>();
    // Start is called before the first frame update
    void Start()
    {
        logfilename = /*DateTime.Now.ToString("dd-hh-yyyy-hh:mm:ss.mm") +"_" +*/ GenRandomID(10);
    }

    bool readCSVOnce = false;

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
            {
                player1GO = NetworkManager.Singleton.ConnectedClients[0].PlayerObject;
                player2GO = NetworkManager.Singleton.ConnectedClients[1].PlayerObject;

                if (player1GO.GetComponent<FPSController>().isPlayerReady.Value &&
                    !player1GO.GetComponent<FPSController>().expQuesEnabled.Value &&
                    !player1GO.GetComponent<FPSController>().qoeEnabled.Value &&
                    player2GO.GetComponent<FPSController>().isPlayerReady.Value &&
                    !player2GO.GetComponent<FPSController>().expQuesEnabled.Value &&
                    !player2GO.GetComponent<FPSController>().qoeEnabled.Value
                )
                {
                    roundTimer.Value -= Time.deltaTime;
                }

                if (!initialRoundConfigsTransferred)
                {
                    initialRoundConfigsTransferred = true;
                    SendRoundConfigs();
                }
                if (roundTimer.Value <= 0)
                {
                    roundTimer.Value = roundDuration;
                    currentRound++;
                    SendRoundConfigs();
                    RespawnClients();
                }
            }
            else
            {
                if (!readCSVOnce)
                {
                    ReadCSV();

                    roundTimer.Value = roundDuration;
                    numberOfRounds = player1Configs.highLatencyArray.Count;

                    for (int i = 0; i < numberOfRounds; i++)
                    {
                        indexArray.Add(i);
                    }

                    // Shuffle the list
                    Shuffle(indexArray);

                    //Add practice round
                    int temp = indexArray[numberOfRounds - 1];
                    indexArray[numberOfRounds - 1] = indexArray[0];
                    indexArray[0] = 0;
                    indexArray.Add(temp);
                    indexArray.Add(0);
                    numberOfRounds++;

                    // Debug.Log to print the shuffled list to the Console window in Unity
                    foreach (var index in indexArray)
                    {
                        Debug.Log(index);
                    }

                    currentRound = 1;
                    readCSVOnce = true;
                }
            }

        }
    }
    void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    private void RespawnClients()
    {
        roundTimer.Value = roundDuration;
        NetworkManager.Singleton.ConnectedClients[0].PlayerObject.GetComponent<FPSController>().RespawnOnlyPlayerServerRpc();
        NetworkManager.Singleton.ConnectedClients[1].PlayerObject.GetComponent<FPSController>().RespawnOnlyPlayerServerRpc();
    }
    void SendRoundConfigs()
    {
        NetworkManager.Singleton.ConnectedClients[0].PlayerObject.GetComponent<LatencyManager>().SetLatConfigsServerRpc(
            player1Configs.lowLatencyArray[indexArray[currentRound - 1]],
            player1Configs.highLatencyArray[indexArray[currentRound - 1]],
            player1Configs.ADTKickInDelayArray[indexArray[currentRound - 1]],
            player1Configs.ADTLetOffDelayArray[indexArray[currentRound - 1]], currentRound, numberOfRounds, logfilename, indexArray[currentRound - 1]
        );

        NetworkManager.Singleton.ConnectedClients[1].PlayerObject.GetComponent<LatencyManager>().SetLatConfigsServerRpc(
            player2Configs.lowLatencyArray[indexArray[currentRound - 1]],
            player2Configs.highLatencyArray[indexArray[currentRound - 1]],
            player2Configs.ADTKickInDelayArray[indexArray[currentRound - 1]],
            player2Configs.ADTLetOffDelayArray[indexArray[currentRound - 1]], currentRound, numberOfRounds, logfilename, indexArray[currentRound - 1]
        );
    }

    public void ReadCSV()
    {
        string line = null;
        StreamReader strReader = new StreamReader("Data\\Configs\\PlayerConfig.csv");
        bool EOF = false;
        player1Configs.lowLatencyArray.Clear();
        player1Configs.highLatencyArray.Clear();
        player1Configs.ADTKickInDelayArray.Clear();
        player1Configs.ADTLetOffDelayArray.Clear();

        player2Configs.lowLatencyArray.Clear();
        player2Configs.highLatencyArray.Clear();
        player2Configs.ADTKickInDelayArray.Clear();
        player2Configs.ADTLetOffDelayArray.Clear();

        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var dataValues = line.Split(',');

                /*for (int i = 0; i < dataValues.Length; i++)
                    Debug.Log(dataValues[i]);*/

                //Player 1
                player1Configs.lowLatencyArray.Add(float.Parse(dataValues[0]));
                player1Configs.highLatencyArray.Add(float.Parse(dataValues[1]));
                player1Configs.ADTKickInDelayArray.Add(float.Parse(dataValues[2]));
                player1Configs.ADTLetOffDelayArray.Add(float.Parse(dataValues[3]));

                //Player 2
                player2Configs.lowLatencyArray.Add(float.Parse(dataValues[4]));
                player2Configs.highLatencyArray.Add(float.Parse(dataValues[5]));
                player2Configs.ADTKickInDelayArray.Add(float.Parse(dataValues[6]));
                player2Configs.ADTLetOffDelayArray.Add(float.Parse(dataValues[7]));
            }
        }
    }

    String GenRandomID(int len)
    {
        String alphanum = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        String tmp_s = "";

        for (int i = 0; i < len; ++i)
        {
            tmp_s += alphanum[UnityEngine.Random.Range(0, 1000) % (alphanum.Length - 1)];
        }

        return tmp_s;
    }
}
