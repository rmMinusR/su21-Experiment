using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CastingUIDriver : ScopedEventListener
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

    public void ClearCurrentAbility(Events.AbilityEndEvent.Reason reason, bool showMessage)
    {
        abilityCastTimeMax = 0;
        if(showMessage) ShowMessage(reason.ToString());
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

    protected override void DoEventRegistration()
    {
        EventBus.AddListener(this, typeof(Events.AbilityStartEvent), Events.Priority.Normal);
        EventBus.AddListener(this, typeof(Events.AbilityEndEvent  ), Events.Priority.Normal);
    }

    public override void OnRecieveEvent(Event e)
    {
        //if(e is Events.AbilityStartEvent eStart) SetCurrentAbility(eStart.ability); //TODO reimplement?
        if(e is Events.AbilityEndEvent eEnd) ClearCurrentAbility(eEnd.reason, eEnd.showMessage);
    }
}