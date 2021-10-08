using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIDriver : MonoBehaviour
{
    private void Update()
    {
        //Process ability UI
        abilityCastTimeCurrent += Time.deltaTime;
        if (abilityCastTimeCurrent < abilityCastTimeMax)
        {
            abilityProgressBar.fillAmount = abilityCastTimeCurrent / abilityCastTimeMax;
        }
        else
        {
            abilityUIContainer.SetActive(false);
        }
    }

    [Header("UI elements")]
    [SerializeField] private GameObject abilityUIContainer;
    [SerializeField] private Image abilityIcon;
    [SerializeField] private Image abilityProgressBar;
    [SerializeField] private Text abilityNameText;
    [SerializeField] [HideInInspector] private float abilityCastTimeCurrent;
    [SerializeField] [HideInInspector] private float abilityCastTimeMax;

    public void SetCurrentAbility(Sprite icon, string name, float maxTime, float curTime = 0)
    {
        abilityUIContainer.SetActive(true);
        abilityIcon.sprite = icon;
        abilityNameText.text = name;
        abilityCastTimeCurrent = curTime;
        abilityCastTimeMax = maxTime;
        abilityProgressBar.fillAmount = curTime / maxTime;
    }

    public enum AbilityEndReason
    {
        None,
        CastTimeEnded,
        Cancelled,
        Interrupted
    }

    public void ClearCurrentAbility(AbilityEndReason reason)
    {
        abilityCastTimeMax = 0;
        if(reason != AbilityEndReason.None) ShowMessage(reason.ToString());
    }

    [Header("Messages")]
    [SerializeField] private TextMessage messagePrefab;
    [SerializeField] private Transform messageSource;
    [SerializeField] private float messageLife;

    public void ShowMessage(string message)
    {
        TextMessage obj = Instantiate(messagePrefab.gameObject, messageSource.position, messageSource.rotation).GetComponent<TextMessage>();
        obj.uiText.text = message;
        obj.lifetime = messageLife;
    }
}