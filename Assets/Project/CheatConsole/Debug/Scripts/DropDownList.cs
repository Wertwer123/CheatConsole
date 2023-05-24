using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Cheating.UIElements
{
    public sealed class DropDownList : MonoBehaviour
    {
        

        [Header("UIElements")]
        [Tooltip("Needs to lie inside of an ScrollRect")]
        [SerializeField] VerticalLayoutGroup LayoutGroup;
        [SerializeField] GameObject DropDownElementTemplate;

        List<DropDownListElement> DropDownListElements = new();
        Dictionary<string, GameObject> AllDropDownElements = new();
       

        public static DropDownList operator +(DropDownList DropDownList, DropDownListElement DropDownListElement)
        {
            DropDownList.TryAddNewDropDownListElement(DropDownListElement);
            return DropDownList;
        }

        public void Expand()
        {
            LayoutGroup.gameObject.SetActive(true);
        }
        public void Hide()
        {
            LayoutGroup.gameObject.SetActive(false);
        }
        public void Clear()
        {
            foreach (var DropDownElement in AllDropDownElements)
            {
                DropDownElement.Value.SetActive(false);
            }
        }
        public void Refresh()
        {
            foreach (var DropDownElement in DropDownListElements)
            {
                TrySpawnDropDownElement(DropDownElement.GetText());
            }
        }
        public void SetDropDownListContent(List<DropDownListElement> NewListContent)
        {
            DropDownListElements = NewListContent;
            Refresh();
        }
        public void TryAddNewDropDownListElement(DropDownListElement NewDropDownListElement)
        {
            if (AllDropDownElements.ContainsKey(NewDropDownListElement.GetText()))
            {
                AllDropDownElements[NewDropDownListElement.GetText()].SetActive(true);
                return;
            }

            DropDownListElements.Add(NewDropDownListElement);
            TrySpawnDropDownElement(NewDropDownListElement.GetText());

        }
        void TrySpawnDropDownElement(string Text)
        {
            if (AllDropDownElements.ContainsKey(Text)) return;

            var Instance = Instantiate(DropDownElementTemplate, transform.position, Quaternion.identity, LayoutGroup.transform);
            Instance.GetComponentInChildren<TMP_Text>().text = Text;
            AllDropDownElements.Add(Text, Instance);
        }
        
        #region NestedClass

        public class DropDownListElement
        {
            string Text;

            #region Properties

            public string GetText() => Text;

            #endregion

            public DropDownListElement(string Text)
            {
                this.Text = Text;
            }
        }

        #endregion
    }
}

