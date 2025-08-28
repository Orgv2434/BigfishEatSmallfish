using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestingNetcodeUI : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;
    // 游戏进程管理器
    public static TestingNetcodeUI instance {  get; private set; }

    private void Awake()

    {
        startHostButton.onClick.AddListener(() =>
        {
            Debug.Log("Host");
            NetworkManager.Singleton.StartHost();
      
            Hide();
        });
        startClientButton.onClick.AddListener(() =>
        {
            Debug.Log("Client");
            NetworkManager.Singleton.StartClient();

            Hide();
        });
    }



    private void Hide()
    {
        gameObject.SetActive(false);

    }
  

}
