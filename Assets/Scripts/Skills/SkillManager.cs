using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkillManager : MonoBehaviour
{
    public SkillData skill;
    private GameObject source;
    private AudioSource sourceContextualSource;
    private Button button;
    private bool isReady;

    public void Initialize(SkillData skill, GameObject source)
    {
        this.skill = skill;
        this.source = source;

        // Try to get the audio source from the source unit
        UnitManager um = source.GetComponent<UnitManager>();
        if (um != null)
        {
            sourceContextualSource = um.contextualSource;
        }
    }

    public void Trigger(GameObject target = null)
    {
        if (!isReady)
        {
            return;
        }

        StartCoroutine(WrappedTrigger(target));
    }

    public void SetButton(Button button)
    {
        this.button = button;
        SetReady(true);
    }

    private IEnumerator WrappedTrigger(GameObject target)
    {
        if (sourceContextualSource != null && skill.onStartSound != null)
        {
            sourceContextualSource.PlayOneShot(skill.onStartSound);
        }

        yield return new WaitForSeconds(skill.castTime);

        if (sourceContextualSource != null && skill.onEndSound != null)
        {
            sourceContextualSource.PlayOneShot(skill.onEndSound);
        }

        skill.Trigger(source, target);
        SetReady(false);
        yield return new WaitForSeconds(skill.cooldown);
        SetReady(true);
    }

    private void SetReady(bool ready)
    {
        isReady = ready;

        if (button != null)
        {
            button.interactable = ready;
        }
    }
}