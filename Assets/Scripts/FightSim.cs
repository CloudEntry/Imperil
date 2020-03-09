using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class FightSim : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Fight());
    }

    IEnumerator Fight()
    {
        yield return new WaitForSeconds(2);
        // RNG for whether attacker wins 0 -> 1
        int num = Random.Range(0, 2);

        if (num == 0)
        {
            ManageGame.instance.battleWon = false;
        }
        else
        {
            ManageGame.instance.battleWon = true;
        }

        ManageGame.instance.battleHasEnded = true;
        SceneManager.LoadScene("SampleScene");
    }
}
