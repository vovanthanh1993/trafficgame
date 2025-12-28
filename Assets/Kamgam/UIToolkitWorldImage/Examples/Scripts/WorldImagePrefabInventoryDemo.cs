using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Kamgam.UIToolkitWorldImage;

namespace Kamgam.UIToolkitWorldImage.Examples
{
    public partial class WorldImagePrefabInventoryDemo : MonoBehaviour
    {
        UIDocument m_uiDocument;
        public UIDocument Document
        {
            get
            {
                if (m_uiDocument == null)
                {
                    m_uiDocument = this.GetComponent<UIDocument>();
                }
                return m_uiDocument;
            }
        }

        WorldImage m_worldImage;
        Button m_btnSword;
        Button m_btnStaff;

        public void Start()
        {
            m_worldImage = Document.rootVisualElement.Q<WorldImage>("WorldImageSword");

            m_btnSword = Document.rootVisualElement.Q<Button>("ButtonSword"); 
            m_btnSword.RegisterCallback<ClickEvent>(onSwordButtonClicked);

            m_btnStaff = Document.rootVisualElement.Q<Button>("ButtonStaff");
            m_btnStaff.RegisterCallback<ClickEvent>(onStaffButtonClicked);

            m_btnSword.SetEnabled(false);
            m_btnStaff.SetEnabled(true);
        }

        private void onSwordButtonClicked(ClickEvent evt)
        {
            m_btnSword.SetEnabled(false);
            m_btnStaff.SetEnabled(true);

            m_worldImage.GetPrefabInstantiator().EnableOrCreate(0, destroyOnDisable: false, disableOthers: true);
        }

        private void onStaffButtonClicked(ClickEvent evt)
        {
            m_btnSword.SetEnabled(true);
            m_btnStaff.SetEnabled(false);

            m_worldImage.GetPrefabInstantiator().EnableOrCreate(1, destroyOnDisable: false, disableOthers: true);
        }
    }
}