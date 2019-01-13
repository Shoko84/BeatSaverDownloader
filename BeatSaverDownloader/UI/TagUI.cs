using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.FlowCoordinators;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = BeatSaverDownloader.Misc.Logger;

namespace BeatSaverDownloader.UI
{
    class TagUI : MonoBehaviour
    {
        /// <summary>
        /// Is UI currently initializing ?
        /// </summary>
        public bool IsInitializing = false;
        /// <summary>
        /// Is UI initialized ?
        /// </summary>
        public bool Initialized = false;
        
        private static TagUI _instance = null;
        /// <summary>
        /// Instance of the singleton <see cref="TagUI"/>
        /// </summary>
        public static TagUI Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("TagUI").AddComponent<TagUI>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// Instance of the <see cref="CustomMenu"/> created for the <see cref="TagUI"/>
        /// </summary>
        private CustomMenu _CustomMenu;
        /// <summary>
        /// Instance of the <see cref="CustomViewController"/> created for the <see cref="TagUI"/>
        /// </summary>
        private CustomViewController _CustomViewController;
        /// <summary>
        /// An array of the tag names that should be displayed
        /// </summary>
        private string[] _TagNames;
        /// <summary>
        /// The current page (1 is the first page)
        /// </summary>
        private int _CurrentPage = 1;

        /// <summary>
        /// The top label "Tags" displayed
        /// </summary>
        private TextMeshProUGUI _TopTagLabel;
        /// <summary>
        /// Every button tags created for the <see cref="TagUI"/>
        /// </summary>
        private Dictionary<Button, bool> _ButtonTags;
        /// <summary>
        /// The "Up arrow" <see cref="Button"/> for changing to the previous tag page
        /// </summary>
        private Button _PageUpButton;
        /// <summary>
        /// The "Down arrow" <see cref="Button"/> for changing to the next tag page
        /// </summary>
        private Button _PageDownButton;
        /// <summary>
        /// The "Close" <see cref="Button"/> for closing the <see cref="TagUI"/>
        /// </summary>
        private Button _CloseButton;

        /// <summary>
        /// The <see cref="ResultsViewController"/> from the result panel
        /// </summary>
        private ResultsViewController _ResultViewController;
        /// <summary>
        /// The "OK" button contained into the <see cref="ResultsViewController"/>
        /// </summary>
        private Button _OKResultButton;

        /**************\
        |* WEB EVENTS *|
        \**************/

        /// <summary>
        /// TODO: Warns the server that the state of a tag has been changed
        /// </summary>
        /// <param name="tagState">The state of the tag</param>
        /// <returns></returns>
        private IEnumerator _ChangingTagState(bool tagState)
        {
            yield return null;
        }

        /****************\
        |* CLICK EVENTS *|
        \****************/

        /// <summary>
        /// Event triggered when the up arrow is clicked
        /// </summary>
        private void _PageUpClicked()
        {
            if (_CurrentPage == 1) return;
            _ReloadPage(-1);
        }

        /// <summary>
        /// Event triggered when the down arrow is clicked
        /// </summary>
        private void _PageDownClicked()
        {
            _ReloadPage(1);
        }

        /// <summary>
        /// Event triggered when the "OK" button from the result screen is clicked
        /// </summary>
        private void _OKResultButtonClicked()
        {
            _CustomMenu.Dismiss();
            _OKResultButton.onClick.RemoveListener(_OKResultButtonClicked);
        }

        /// <summary>
        /// Event triggered when a tag is clicked
        /// </summary>
        /// <param name="tag">The <see cref="Button"/> instance clicked</param>
        private void _TagClicked(Button tag)
        {
            HMUI.NoTransitionsButton ntb = tag.GetComponent<HMUI.NoTransitionsButton>();

            if (_ButtonTags[tag])
                ntb.selectionStateDidChangeEvent -= Ntb_selectionStateDidChangeEvent;
            else
                ntb.selectionStateDidChangeEvent += Ntb_selectionStateDidChangeEvent;
            _ButtonTags[tag] = !_ButtonTags[tag];
            StartCoroutine(_ChangingTagState(_ButtonTags[tag]));
        }

        /// <summary>
        /// Enable the background for all the toggled tags (bit ghetto here but "works")
        /// </summary>
        /// <returns>yields for the Coroutine system</returns>
        private IEnumerator _EnableBGForToggledTags()
        {
            yield return new WaitForEndOfFrame();
            foreach (var pair in _ButtonTags)
            {
                if (pair.Value)
                {
                    pair.Key.transform.Find("Wrapper/BG").gameObject.SetActive(true);
                    pair.Key.transform.Find("Wrapper/Content/Text").GetComponent<TextMeshProUGUI>().color = new Color(0, 0, 0);
                }
            }
        }

        /// <summary>
        /// Event triggered when the <see cref="HMUI.NoTransitionsButton.SelectionState"/> is changed
        /// </summary>
        /// <param name="selectionState">The new <see cref="HMUI.NoTransitionsButton.SelectionState"/> selection state</param>
        private void Ntb_selectionStateDidChangeEvent(HMUI.NoTransitionsButton.SelectionState selectionState)
        {
            if (selectionState == HMUI.NoTransitionsButton.SelectionState.Normal)
                StartCoroutine(_EnableBGForToggledTags());
        }

        /// <summary>
        /// Generate the main UI components for the tag system
        /// </summary>
        /// <param name="viewController">The <see cref="CustomViewController"/> where you want to add tags</param>
        /// <param name="tags">An array of tag names</param>
        /// <param name="size">The anchor size of a Tag</param>
        /// <param name="posStart">The anchor position where you want to start the Tag generation</param>
        /// <param name="gap">The gap between each tag</param>
        /// <param name="textSize">The text size for the text inside a Tag</param>
        private void _GenerateButtonTags(CustomViewController viewController, string[] tags,
                                         Vector2 size, Vector2 posStart, Vector2 gap, float textSize)
        {
            if (!_PageUpButton)
            {
                _PageUpButton = viewController.CreateUIButton("PageUpButton",
                                                              new Vector2(2f, posStart.y + gap.y),
                                                              size,
                                                              () => { _PageUpClicked(); },
                                                              "");
            }
            _PageUpButton.interactable = _CurrentPage > 1;

            int nbTags = 0;
            for (; nbTags < tags.Length; ++nbTags)
            {
                Vector2 buttonPos = new Vector2((nbTags % 2 == 0 && nbTags + 1 == tags.Length) ? (0) : (posStart.x + ((nbTags % 2 == 0) ? (-gap.x) : (gap.x))),
                                                posStart.y - gap.y * (nbTags % 10 / 2));
                Button button = viewController.CreateUIButton("CreditsButton",
                                                              buttonPos,
                                                              size,
                                                              () => { },
                                                              tags[nbTags]);

                button.onClick.AddListener(delegate { _TagClicked(button); });
                button.SetButtonTextSize(textSize);
                button.ToggleWordWrapping(false);
                if (nbTags >= 10)
                    button.gameObject.SetActive(false);
                _ButtonTags.Add(button, false);
            }

            if (!_PageDownButton)
            {
                _PageDownButton = viewController.CreateUIButton("PageDownButton",
                                                                new Vector2(2f, posStart.y - gap.y * 5),
                                                                size,
                                                                () => { _PageDownClicked(); },
                                                                "");
            }
            _PageDownButton.interactable = nbTags >= 10;
        }

        /// <summary>
        /// Show and hide tags according to the current page
        /// </summary>
        /// <param name="pageInc">How much you want to add to the current page (can be negative)</param>
        private void _ReloadPage(int pageInc)
        {
            var keys = _ButtonTags.Keys.ToList();

            for (int i = (_CurrentPage - 1) * 10; i < keys.Count && i < _CurrentPage * 10; ++i)
                keys[i].gameObject.SetActive(false);
            _CurrentPage += pageInc;
            for (int i = (_CurrentPage - 1) * 10; i < keys.Count && i < _CurrentPage * 10; ++i)
                keys[i].gameObject.SetActive(true);

            _PageUpButton.interactable = _CurrentPage > 1;
            _PageDownButton.interactable = (_CurrentPage * 10) < _ButtonTags.Count;
        }

        /// <summary>
        /// Event triggered when the ResultViewController has been activated
        /// </summary>
        /// <param name="firstActivation">Is it the first it has been called ?</param>
        /// <param name="activationType">The kind of activation</param>
        private void _ResultViewController_didActivateEvent(bool firstActivation, VRUI.VRUIViewController.ActivationType activationType)
        {
            if (Initialized || IsInitializing) return;
            IDifficultyBeatmap diffBeatmap = _ResultViewController.GetPrivateField<IDifficultyBeatmap>("_difficultyBeatmap");
            if (diffBeatmap.level.levelID.Length >= 32)
            {
                IsInitializing = true;
                StartCoroutine(_SetupUI());
            }
            else
                Initialized = true;
        }

        /// <summary>
        /// Set up the UI components for the Tag System
        /// </summary>
        /// <returns>yields for the Coroutine system</returns>
        private IEnumerator _SetupUI()
        {
            if (!Initialized)
            {
                _ButtonTags = new Dictionary<Button, bool>();

                _CustomMenu = BeatSaberUI.CreateCustomMenu<CustomMenu>("Tags");
                _CustomViewController = BeatSaberUI.CreateViewController<CustomViewController>();
                _TagNames = new string[] { "Flow", "Streams", "Inventive Patterns",
                                       "Meme", "Jump Streams", "Beautiful Lighting", "Vision Blocks",
                                       "Bad Walls", "Overstated Difficulty", "Ugly", "Useless", "Entertaining", "Impossible" };
                _CustomMenu.SetLeftViewController(_CustomViewController, false, (firstActivation, type) =>
                {
                    if (firstActivation && type == VRUI.VRUIViewController.ActivationType.AddedToHierarchy)
                    {
                        _TopTagLabel = _CustomViewController.CreateText("Tags", new Vector2(2f, 32.5f));
                        _TopTagLabel.fontSize = 5f;
                        _TopTagLabel.alignment = TextAlignmentOptions.Center;
                        _GenerateButtonTags(_CustomViewController, _TagNames, new Vector2(12.5f, 6.25f), new Vector2(0, 12f), new Vector2(10f, 8.5f), 2f);
                        _CloseButton = _CustomViewController.CreateUIButton("CreditsButton",
                                                                            new Vector2(43f, 31.5f),
                                                                            new Vector2(20f, 8f),
                                                                            () => { _CustomMenu.Dismiss(); },
                                                                            "Close");
                        _CloseButton.SetButtonTextSize(3f);
                        _CloseButton.ToggleWordWrapping(false);

                        _OKResultButton = _ResultViewController.GetComponentsInChildren<Button>().First(x => x.name == "Ok");
                        _OKResultButton.onClick.AddListener(_OKResultButtonClicked);
                    }
                });

                yield return new WaitUntil(() => { return _CustomMenu.Present(false); });
                _CustomMenu.Present();

                IsInitializing = false;
                Initialized = true;
            }
        }

        /// <summary>
        /// Load and initialize the TagUI
        /// </summary>
        public void OnLoad()
        {
            Initialized = false;

            _ResultViewController = Resources.FindObjectsOfTypeAll<ResultsViewController>().First(x => x.name == "StandardLevelResultsViewController");
            _ResultViewController.didActivateEvent += _ResultViewController_didActivateEvent;
        }
    }
}
