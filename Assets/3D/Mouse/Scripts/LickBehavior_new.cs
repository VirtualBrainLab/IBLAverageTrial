using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LickBehavior_new : MonoBehaviour
{
    public Animator lickAnimator;
    public Animator waterAnimator;

    private void Awake()
    {
        waterAnimator.speed = 5;
    }

    public void Lick()
    {
        lickAnimator.SetBool("Lick", true);
        Drop();
        StartCoroutine(EndLick(0.3f + Random.value * 0.6f));
    }

    public IEnumerator EndLick(float delay)
    {
        yield return new WaitForSeconds(delay);
        lickAnimator.SetBool("Lick", false);
    }

    public void Drop()
    {
        waterAnimator.SetTrigger("ValveOpen");
        StartCoroutine(EndDrop(0.15f + Random.value * 0.15f));
    }
    public IEnumerator EndDrop(float delay)
    {
        yield return new WaitForSeconds(delay);
        waterAnimator.SetTrigger("ValveClosed");
    }
}
