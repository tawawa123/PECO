using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public class UIManager : MonoBehaviour
    {
        [System.Serializable]
        public class UIEntry
        {
            public UIType type;
            public UIGroup group;
            public GameObject panel;
        }

        public static UIManager Instance { get; private set; }

        [SerializeField] private List<UIEntry> uiList;

        private Dictionary<UIType, UIEntry> uiMap;

        private void Awake()
        {
            Instance = this;

            uiMap = new Dictionary<UIType, UIEntry>();
            foreach (var ui in uiList)
            {
                uiMap[ui.type] = ui;
            }
        }

        // UIへの参照
        public Transform Get(UIType type)
        {
            return uiMap[type].panel.transform;
        }

        // 個別制御
        public void Show(UIType type)
        {
            uiMap[type].panel.SetActive(true);
        }

        public void Hide(UIType type)
        {
            uiMap[type].panel.SetActive(false);
        }

        // グループ制御
        public void SetGroupActive(UIGroup group, bool active)
        {
            foreach (var ui in uiMap.Values)
            {
                if (ui.group == group)
                {
                    ui.panel.SetActive(active);
                }
            }
        }
    }

}