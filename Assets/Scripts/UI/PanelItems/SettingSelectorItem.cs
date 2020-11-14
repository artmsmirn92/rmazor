﻿using Constants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PanelItems
{
    public class SettingSelectorItem : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private Animator animator;

        private bool m_IsInitialized;
        
        public void Init(bool _Selected, string _Text, System.Action<string> _Select, ToggleGroup _ToggleGroup)
        {
            title.text = _Text;
            name = $"{_Text} Setting";
            toggle.group = _ToggleGroup;
            toggle.isOn = _Selected;
            if (_Selected)
                animator.SetTrigger(AnimKeys.Selected);
                
 
            toggle.onValueChanged.AddListener(_IsOn =>
            {
                if (_IsOn && m_IsInitialized)
                {
                    SoundManager.Instance.PlayMenuButtonClick();
                    _Select?.Invoke(_Text);
                }
            });
            
            m_IsInitialized = true;
        }
    }
}