using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loop : MonoBehaviour
{
    private int loopCount = 0;
    private int incompleteDungeons = 0;
    
    void Awake()
    {
        Loop[] objs = GameObject.FindObjectsOfType<Loop>();

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
        }
        
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        if (loopCount < 1000)
        {
            Generator3D levelGenerator = FindObjectOfType<Generator3D>();
            loopCount++;
            levelGenerator.GenerateLevel();
            //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void IncrementIncompleteDungeons()
    {
        incompleteDungeons++;
        Debug.Log("Number of incomplete dungeons: " + incompleteDungeons);
    }
}
