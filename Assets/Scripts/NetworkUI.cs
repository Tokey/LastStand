using Demo.Scripts.Runtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;
using System;
using System.IO;

public class NetworkUI : NetworkBehaviour
{
    public Button hostButton;
    public Button clientButton;
    public Button serverButton;

    public Image highAlertTop;
    public Image highAlertBottom;
    public Image highAlertLeft;
    public Image highAlertRight;


    public TMP_InputField serverIP;
    public TMP_InputField serverPort;

    public GameObject player;
    public GameObject enemy;
    public GameObject[] localPlayerObjects;
    public TMPro.TMP_Text ammoText;
    public TMPro.TMP_Text healthText;
    public TMPro.TMP_Text scoreText;
    public TMPro.TMP_Text durationText;

    public TMPro.TMP_Text killsText;
    public TMPro.TMP_Text deathsText;

    public TMPro.TMP_Text roundText;
    public TMPro.TMP_Text pingText;

    public GameObject readyTextGO;
    public TMPro.TMP_Text readyText;

    public GameObject qoeSliderGO;
    public Slider qoeSlider;
    public TMPro.TMP_Text sliderText;
    public GameObject qoeSubmitGO;

    public GameObject expQuestionGO;

    public RoundManager roundManager;

    public Animator healthIconAnim;
    public Image healthIconImage;

    public Image hitMarkerImage;

    bool isServerCSV = true;
    String serverIPCSV = "";
    UInt16 portCSV = 0;
    bool lagview = false;
    //int serverTickRateCSV = 60;


    // Start is called before the first frame update
    private void Awake()
    {
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = serverIP.text;
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = UInt16.Parse(serverPort.text);

            NetworkManager.Singleton.StartHost();
            DisableNetworkUI();
        }
        );

        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.GetComponent<UnityTransport>().ConnectionData.Address = serverIP.text;
            NetworkManager.GetComponent<UnityTransport>().ConnectionData.Port = UInt16.Parse(serverPort.text);

            NetworkManager.Singleton.StartClient();
            DisableNetworkUI();
        }
        );

        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.GetComponent<UnityTransport>().ConnectionData.Address = serverIP.text;
            NetworkManager.GetComponent<UnityTransport>().ConnectionData.Port = UInt16.Parse(serverPort.text);

            NetworkManager.Singleton.StartServer();
            DisableNetworkUI();
        }
        );
    }

    private void Start()
    {
        ReadCSV();
        serverIP.text = serverIPCSV;
        serverPort.text = portCSV.ToString();
        if (!isServerCSV)
        {
            hostButton.GameObject().SetActive(false);
            serverButton.GameObject().SetActive(false);
            serverIP.GameObject().SetActive(false);
            serverPort.GameObject().SetActive(false);
        }
        else
        {
            clientButton.GameObject().SetActive(false);
            serverButton.GameObject().SetActive(false);
            serverIP.GameObject().SetActive(false);
            serverPort.GameObject().SetActive(false);
        }

    }

    void DisableNetworkUI()
    {
        hostButton.GameObject().SetActive(false);
        clientButton.GameObject().SetActive(false);
        serverButton.GameObject().SetActive(false);
        serverIP.GameObject().SetActive(false);
        serverPort.GameObject().SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            ammoText.text = "" + player.GetComponent<FPSController>().GetGun().currentAmmoCount;
            healthText.text = "      " + player.GetComponent<PlayerStats>().currentHealth;
            scoreText.text = "Score: " + player.GetComponent<FPSController>().score;
            durationText.text = "Duration: " + roundManager.roundTimer.Value.ToString("#.#");

            killsText.text = "Kills: " + player.GetComponent<FPSController>().roundKills;
            deathsText.text = "Deaths: " + player.GetComponent<FPSController>().roundDeaths;

            if (player.GetComponent<LatencyManager>().currentRoundNumber <= player.GetComponent<LatencyManager>().totalRoundNumber)
                roundText.text = "Round\n " + player.GetComponent<LatencyManager>().currentRoundNumber + "/" + player.GetComponent<LatencyManager>().totalRoundNumber;
            else
                roundText.text = "Thank You!";

            if (!player.GetComponent<FPSController>().isInvincible)
            {
                healthIconAnim.enabled = false;
                healthIconImage.color = Color.white;
            }
            else
            {
                healthIconAnim.enabled = true;
            }

            if (pingText.isActiveAndEnabled)
            {
                if (lagview)
                    pingText.text = player.GetComponent<LatencyManager>().playerLatency.ToString();
                else 
                    pingText.text = "";
            }

            if (!player.GetComponent<FPSController>().qoeEnabled.Value && !player.GetComponent<FPSController>().expQuesEnabled.Value)
                readyTextGO.gameObject.SetActive(!player.GetComponent<FPSController>().isPlayerReady.Value || !player.GetComponent<FPSController>().otherReady);

            if (readyTextGO.gameObject.activeSelf)
            {
                if (!player.GetComponent<FPSController>().isPlayerReady.Value)
                {
                    readyText.text = "Press Tab when you are ready";
                }
                else
                {
                    readyText.text = "Wait for your opponent to be ready";
                }
            }


            qoeSliderGO.gameObject.SetActive(player.GetComponent<FPSController>().qoeEnabled.Value);
            expQuestionGO.gameObject.SetActive(player.GetComponent<FPSController>().expQuesEnabled.Value);

            if (enemy != null)
            {
                var relativePoint = player.transform.InverseTransformPoint(enemy.transform.position);

                float distance = Vector3.Distance(player.transform.position, enemy.transform.position);

                if (player.GetComponent<FPSController>().takehitTimer > 0)
                    SetHighAlertAlpha(distance / (3 + player.GetComponent<FPSController>().takehitTimer * 20), player.GetComponent<FPSController>().takehitTimer);
                else
                    SetHighAlertAlpha(distance/2, 0);


                if (relativePoint.x < 0.0 && relativePoint.z > 0.0) // Front left
                {
                    if (Math.Abs(relativePoint.x) > Math.Abs(relativePoint.z))
                    {
                        highAlertLeft.gameObject.SetActive(true);
                        highAlertTop.gameObject.SetActive(false);
                    }
                    else
                    {
                        highAlertLeft.gameObject.SetActive(false);
                        highAlertTop.gameObject.SetActive(true);
                    }

                    highAlertBottom.gameObject.SetActive(false);
                    highAlertRight.gameObject.SetActive(false);
                }
                else if (relativePoint.x > 0.0 && relativePoint.z > 0.0) // front right
                {
                    if (Math.Abs(relativePoint.x) > Math.Abs(relativePoint.z))
                    {
                        highAlertRight.gameObject.SetActive(true);
                        highAlertTop.gameObject.SetActive(false);
                    }
                    else
                    {
                        highAlertRight.gameObject.SetActive(false);
                        highAlertTop.gameObject.SetActive(true);
                    }

                    highAlertLeft.gameObject.SetActive(false);
                    highAlertBottom.gameObject.SetActive(false);
                }

                else if (relativePoint.x < 0.0 && relativePoint.z < 0.0) // Back left
                {

                    if (Math.Abs(relativePoint.x) > Math.Abs(relativePoint.z))
                    {
                        highAlertLeft.gameObject.SetActive(true);
                        highAlertBottom.gameObject.SetActive(false);
                    }
                    else
                    {
                        highAlertLeft.gameObject.SetActive(false);
                        highAlertBottom.gameObject.SetActive(true);
                    }

                    highAlertTop.gameObject.SetActive(false);
                    highAlertRight.gameObject.SetActive(false);
                }
                else if (relativePoint.x > 0.0 && relativePoint.z < 0.0) // Back right
                {
                    if (Math.Abs(relativePoint.x) > Math.Abs(relativePoint.z))
                    {
                        highAlertRight.gameObject.SetActive(true);
                        highAlertBottom.gameObject.SetActive(false);
                    }
                    else
                    {
                        highAlertRight.gameObject.SetActive(false);
                        highAlertBottom.gameObject.SetActive(true);
                    }

                    highAlertLeft.gameObject.SetActive(false);
                    highAlertTop.gameObject.SetActive(false);

                }
            }
            else
            {
                localPlayerObjects = GameObject.FindGameObjectsWithTag("Player");

                foreach (var playerObj in localPlayerObjects)
                {
                    if (!playerObj.Equals(player))
                        enemy = playerObj;
                }

                highAlertTop.gameObject.SetActive(false);
                highAlertBottom.gameObject.SetActive(false);
                highAlertLeft.gameObject.SetActive(false);
                highAlertRight.gameObject.SetActive(false);
            }

        }
        else if (NetworkManager.Singleton.IsConnectedClient)
        {
            player = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        }



        if (qoeSliderGO.activeSelf)
        {
            sliderText.text = qoeSlider.value.ToString("#.#");
            if (qoeSlider.value != 3.000f)
                qoeSubmitGO.SetActive(true);
            else
                qoeSubmitGO.SetActive(false);
        }

        if (player != null)
        {
            if (player.GetComponent<FPSController>().killCooldown > 0)
            {
                hitMarkerImage.color = new Color(1, 0, 0, 1);
            }
            else if (player.GetComponent<FPSController>().headshotCooldown > 0)
            {
                hitMarkerImage.color = new Color(1, 1, 0, 1);
            }
            else if (player.GetComponent<FPSController>().regularHitCooldown > 0)
            {
                hitMarkerImage.color = new Color(1, 1, 1, 1);
            }
            else
            {
                hitMarkerImage.color = new Color(0, 0, 0, 0);
            }
        }
    }

    void SetHighAlertAlpha(float distance, float takehitTimer)
    {

        if (distance == 0)
            return;

        float green = 1 / (distance / 10);

        if (takehitTimer > 0)
        {
            green = 0;
        }

        float red = 1 / (distance / 10);

        float blue = 0;
        float alpha = 1 / (distance / 10);

        highAlertTop.rectTransform.localScale = new Vector3(1 / ((distance / 5) + 1), 1, 0.9f);
        highAlertBottom.rectTransform.localScale = new Vector3(1 / (((distance / 5) + 0.9f)), 1, 1);
        highAlertLeft.rectTransform.localScale = new Vector3(1 / (((distance / 5) + 0.9f)), 1, 1);
        highAlertRight.rectTransform.localScale = new Vector3(1 / (((distance / 5) + 0.9f)), 1, 1);

        highAlertTop.color = new Color(red, green, blue, alpha);
        highAlertBottom.color = new Color(red, green, blue, alpha);
        highAlertLeft.color = new Color(red, green, blue, alpha);
        highAlertRight.color = new Color(red, green, blue, alpha);
    }

    public void QOESubmitPressed()
    {
        player.GetComponent<FPSController>().qoeValue = qoeSlider.value;
        qoeSlider.value = 3.000f;
        player.GetComponent<FPSController>().qoeEnabled.Value = false;
        player.GetComponent<FPSController>().expQuesEnabled.Value = true;
    }

    public void ExpYesPressed()
    {
        player.GetComponent<FPSController>().expQuesValue = true;
        player.GetComponent<FPSController>().isPlayerReady.Value = false;
        player.GetComponent<FPSController>().expQuesEnabled.Value = false;
        player.GetComponent<FPSController>().LogPerRound();

        if (player.GetComponent<LatencyManager>().currentRoundNumber >= player.GetComponent<LatencyManager>().totalRoundNumber)
            player.GetComponent<FPSController>().toQuit = true;

        player.GetComponent<LatencyManager>().SetLatConfigsFromTemp();
        player.GetComponent<FPSController>().ResetRoundVarsServerRpc();
    }
    public void ExpNoPressed()
    {
        player.GetComponent<FPSController>().expQuesValue = false;
        player.GetComponent<FPSController>().isPlayerReady.Value = false;
        player.GetComponent<FPSController>().expQuesEnabled.Value = false;
        player.GetComponent<FPSController>().LogPerRound();

        if (player.GetComponent<LatencyManager>().currentRoundNumber >= player.GetComponent<LatencyManager>().totalRoundNumber)
            player.GetComponent<FPSController>().toQuit = true;

        player.GetComponent<LatencyManager>().SetLatConfigsFromTemp();
        player.GetComponent<FPSController>().ResetRoundVarsServerRpc();
    }

    public void ReadCSV()
    {
        string line = null;
        StreamReader strReader = new StreamReader("Data\\Configs\\GameConfig.csv");
        bool EOF = false;

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
                isServerCSV = bool.Parse(dataValues[0].ToString());
                serverIPCSV = dataValues[1].ToString();
                portCSV = UInt16.Parse(dataValues[2].ToString());
                lagview = bool.Parse(dataValues[3].ToString());

            }
        }
    }
}
