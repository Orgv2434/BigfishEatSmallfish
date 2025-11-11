using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioSource BGM;
    [SerializeField] private AudioSource BGM2;
    [SerializeField] private AudioSource FSM;
    public AudioClip exp;
    public AudioClip upgrade;
    public AudioClip click;
    public AudioClip drawover;
    public AudioClip die;
    public AudioClip plusExp;
    public AudioClip dush;
    public AudioClip hudun;
    public AudioClip eatskillfish;
    public AudioClip gainhealth;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保在场景切换时不被销毁
            BGM.Play();
            BGM2.Play();
        }
        else
        {
            Destroy(gameObject); // 如果已有实例，销毁重复的实例
        }
    }

    #region 具体音效播放
    // Update is called once per frame
    
    public void PlayBGM()
    {
        BGM.Play();
        BGM2.Play();
    }
    public void StopBGM()
    {
        BGM.Stop();
        BGM2.Stop();
    }

    public void PauseBGM()
    {
        BGM.Pause();
        BGM2.Pause();
    }
    public void Exp()
    {
       FSM.PlayOneShot(exp);
    }
    public void Upgrade()
    {
        FSM.PlayOneShot(upgrade);
    }
    public void Click()
    {
        FSM.PlayOneShot(click);
    }
    public void DrawOver()
    {
        FSM.PlayOneShot(drawover);
    }
    public void Die()
    {
        FSM.PlayOneShot(die);
    }
    public void EatSkillFish()
    {
        FSM.PlayOneShot(eatskillfish);
    }
    public void PlusExp()
    {
        FSM.PlayOneShot(plusExp);
    }
    public void Dush()
    {
        FSM.PlayOneShot(dush);
    }
    public void GainHealth()
    {
        FSM.PlayOneShot(gainhealth);
    }
    public void HuDun()
    {
        FSM.PlayOneShot(hudun);
    }
    
    
    #endregion

    #region 其他绑定方法

    /// <summary>
    /// 查找场景中所有Button并绑定点击音效
    /// </summary>
    public void FindAllButtonsAndBindClickSound()
    {
        // 查找场景中所有Button（包括禁用状态的对象）
        Button[] allButtons = FindObjectsOfType<Button>(includeInactive: true);

        if (allButtons.Length == 0)
        {
            Debug.Log($"【MusicManager】当前场景未找到任何Button");
            return;
        }

        foreach (Button button in allButtons)
        {
            // 先移除已有监听（避免重复绑定导致多次播放音效）
            button.onClick.RemoveListener(Click);
            // 绑定新的点击音效监听
            button.onClick.AddListener(Click);
        }
    }
    #endregion
}
