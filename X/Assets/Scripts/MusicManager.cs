using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [SerializeField] private AudioSource ocean1;
    [SerializeField] private AudioSource ocean2;
    [SerializeField] private AudioSource game;
    public AudioClip eat;
    public AudioClip exp;
    public AudioClip upgrade;
    public AudioClip click;
    public AudioClip drawover;
    public AudioClip die;

    void Awake()
    {
        // 确保全局唯一
        if (instance= null)
        {
           instance = this;
            DontDestroyOnLoad(gameObject);
            ocean1.Play();
            ocean2.Play();
        }

        else if(instance = this)
        {

        }
        

    }
    

    // Update is called once per frame
    public void StopBGM()
    {
        ocean1.Stop();
        ocean2.Stop();
    }
    public void Eat()
    {
        game.clip= eat;
        game.Play();
    }
    public void Exp()
    {
        game.clip = exp;
        game.Play();
    }
    public void Upgrade()
    {
        game.clip =upgrade;
        game.Play();
    }
    public void Click()
    {
        game.clip = click;
        game.Play();
    }
    public void DrawOver()
    {
        game.clip = drawover;
        game.Play();
    }
    public void Die()
    {
        game.clip = die;
        game.Play();
    }
}
