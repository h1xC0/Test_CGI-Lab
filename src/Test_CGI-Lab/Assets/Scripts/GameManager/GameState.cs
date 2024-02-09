﻿using System.Collections;
using System.Collections.Generic;
using Characters;
using Sounds;
using TMPro;
using Tracks;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace GameManager
{
    /// <summary>
    /// Pushed on top of the GameManager during gameplay. Takes care of initializing all the UI and start the TrackManager
    /// Also will take care of cleaning when leaving that state.
    /// </summary>
    public class GameState : AState
    {
        static int s_DeadHash = Animator.StringToHash("Dead");

        public Canvas canvas;
        public TrackManager trackManager;

        public AudioClip gameTheme;

        [Header("UI")]
        public TMP_Text coinText;
        public TMP_Text premiumText;
        public TMP_Text scoreText;
        public TMP_Text distanceText;
        public TMP_Text multiplierText;
        public TMP_Text countdownText;
        public RectTransform powerupZone;
        public RectTransform lifeRectTransform;

        public RectTransform pauseMenu;
        public RectTransform wholeUI;
        public Button pauseButton;

        public Image inventoryIcon;

        public GameObject gameOverPopup;
        public Button premiumForLifeButton;
        public TMP_Text premiumCurrencyOwned;

        [Header("Prefabs")]
        public GameObject PowerupIconPrefab;

        [Header("Tutorial")]
        public TMP_Text tutorialValidatedObstacles;
        public GameObject sideSlideTuto;
        public GameObject upSlideTuto;
        public GameObject downSlideTuto;
        public GameObject finishTuto;

        public Modifier currentModifier = new Modifier();

        protected bool m_Finished;
        protected float m_TimeSinceStart;
        protected List<PowerupIcon> m_PowerupIcons = new List<PowerupIcon>();
        protected Image[] m_LifeHearts;

        protected RectTransform m_CountdownRectTransform;
        protected bool m_WasMoving;

        protected bool m_GameoverSelectionDone = false;

        protected int k_MaxLives = 3;

        protected bool m_IsTutorial; //Tutorial is a special run that don't chance section until the tutorial step is "validated".
        protected int m_TutorialClearedObstacle = 0;
        protected bool m_CountObstacles = true;
        protected bool m_DisplayTutorial;
        protected int m_CurrentSegmentObstacleIndex = 0;
        protected TrackSegment m_NextValidSegment = null;
        protected int k_ObstacleToClear = 3;

        public override void Enter(AState from)
        {
            m_CountdownRectTransform = countdownText.GetComponent<RectTransform>();

            m_LifeHearts = new Image[k_MaxLives];
            for (int i = 0; i < k_MaxLives; ++i)
            {
                m_LifeHearts[i] = lifeRectTransform.GetChild(i).GetComponent<Image>();
            }

            if (MusicPlayer.instance.GetStem(0) != gameTheme)
            {
                MusicPlayer.instance.SetStem(0, gameTheme);
                CoroutineHandler.StartStaticCoroutine(MusicPlayer.instance.RestartAllStems());
            }

            m_GameoverSelectionDone = false;

            StartGame();
        }

        public override void Exit(AState to)
        {
            canvas.gameObject.SetActive(false);

            ClearPowerup();
        }

        public void StartGame()
        {
            canvas.gameObject.SetActive(true);
            pauseMenu.gameObject.SetActive(false);
            wholeUI.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(!trackManager.isTutorial);
            gameOverPopup.SetActive(false);

            sideSlideTuto.SetActive(false);
            upSlideTuto.SetActive(false);
            downSlideTuto.SetActive(false);
            finishTuto.SetActive(false);
            tutorialValidatedObstacles.gameObject.SetActive(false);

            if (!trackManager.isRerun)
            {
                m_TimeSinceStart = 0;
                trackManager.characterController.currentLife = trackManager.characterController.maxLife;
            }

            currentModifier.OnRunStart(this);

            m_IsTutorial = !PlayerData.instance.tutorialDone;
            trackManager.isTutorial = m_IsTutorial;

            if (m_IsTutorial)
            {
                tutorialValidatedObstacles.gameObject.SetActive(true);
                tutorialValidatedObstacles.text = $"0/{k_ObstacleToClear}";

                m_DisplayTutorial = true;
                trackManager.newSegmentCreated = segment =>
                {
                    if (trackManager.currentZone != 0 && !m_CountObstacles && m_NextValidSegment == null)
                    {
                        m_NextValidSegment = segment;
                    }
                };

                trackManager.currentSegementChanged = segment =>
                {
                    m_CurrentSegmentObstacleIndex = 0;

                    if (!m_CountObstacles && trackManager.currentSegment == m_NextValidSegment)
                    {
                        trackManager.characterController.currentTutorialLevel += 1;
                        m_CountObstacles = true;
                        m_NextValidSegment = null;
                        m_DisplayTutorial = true;

                        tutorialValidatedObstacles.text = $"{m_TutorialClearedObstacle}/{k_ObstacleToClear}";
                    }
                };
            }

            m_Finished = false;
            m_PowerupIcons.Clear();

            StartCoroutine(trackManager.Begin());
        }

        public override string GetName()
        {
            return "Game";
        }

        public override void Tick()
        {
            if (m_Finished)
            {
                return;
            }

            if (trackManager.isLoaded)
            {
                CharacterInputController chrCtrl = trackManager.characterController;

                m_TimeSinceStart += Time.deltaTime;

                if (chrCtrl.currentLife <= 0)
                {
                    pauseButton.gameObject.SetActive(false);
                    chrCtrl.CleanConsumable();
                    chrCtrl.character.animator.SetBool(s_DeadHash, true);
                    chrCtrl.characterCollider.koParticle.gameObject.SetActive(true);
                    StartCoroutine(WaitForGameOver());
                }

                List<Consumable.Consumable> toRemove = new List<Consumable.Consumable>();
                List<PowerupIcon> toRemoveIcon = new List<PowerupIcon>();

                for (int i = 0; i < chrCtrl.consumables.Count; ++i)
                {
                    PowerupIcon icon = null;
                    for (int j = 0; j < m_PowerupIcons.Count; ++j)
                    {
                        if (m_PowerupIcons[j].linkedConsumable == chrCtrl.consumables[i])
                        {
                            icon = m_PowerupIcons[j];
                            break;
                        }
                    }

                    chrCtrl.consumables[i].Tick(chrCtrl);
                    if (!chrCtrl.consumables[i].active)
                    {
                        toRemove.Add(chrCtrl.consumables[i]);
                        toRemoveIcon.Add(icon);
                    }
                    else if (icon == null)
                    {
                        GameObject o = Instantiate(PowerupIconPrefab);

                        icon = o.GetComponent<PowerupIcon>();

                        icon.linkedConsumable = chrCtrl.consumables[i];
                        icon.transform.SetParent(powerupZone, false);

                        m_PowerupIcons.Add(icon);
                    }
                }

                for (int i = 0; i < toRemove.Count; ++i)
                {
                    toRemove[i].Ended(trackManager.characterController);

                    Addressables.ReleaseInstance(toRemove[i].gameObject);
                    if (toRemoveIcon[i] != null)
                        Destroy(toRemoveIcon[i].gameObject);

                    chrCtrl.consumables.Remove(toRemove[i]);
                    m_PowerupIcons.Remove(toRemoveIcon[i]);
                }

                if (m_IsTutorial)
                    TutorialCheckObstacleClear();

                UpdateUI();

                currentModifier.OnRunTick(this);
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Pause();
        }

        void OnApplicationFocus(bool focusStatus)
        {
            if (!focusStatus) Pause();
        }

        public void Pause(bool displayMenu = true)
        {
            if (m_Finished || AudioListener.pause == true)
                return;

            AudioListener.pause = true;
            Time.timeScale = 0;

            pauseButton.gameObject.SetActive(false);
            pauseMenu.gameObject.SetActive (displayMenu);
            wholeUI.gameObject.SetActive(false);
            m_WasMoving = trackManager.isMoving;
            trackManager.StopMove();
        }

        public void Resume()
        {
            Time.timeScale = 1.0f;
            pauseButton.gameObject.SetActive(true);
            pauseMenu.gameObject.SetActive (false);
            wholeUI.gameObject.SetActive(true);
            if (m_WasMoving)
            {
                trackManager.StartMove(false);
            }

            AudioListener.pause = false;
        }

        public void QuitToLoadout()
        {
            Time.timeScale = 1.0f;
            AudioListener.pause = false;
            trackManager.End();
            trackManager.isRerun = false;
            PlayerData.instance.Save();
            manager.SwitchState ("Loadout");
        }

        protected void UpdateUI()
        {
            coinText.text = trackManager.characterController.coins.ToString();
            premiumText.text = trackManager.characterController.premium.ToString();

            for (int i = 0; i < 3; ++i)
            {

                if(trackManager.characterController.currentLife > i)
                {
                    m_LifeHearts[i].color = Color.white;
                }
                else
                {
                    m_LifeHearts[i].color = Color.black;
                }
            }

            scoreText.text = trackManager.score.ToString();
            multiplierText.text = "x " + trackManager.multiplier;

            distanceText.text = Mathf.FloorToInt(trackManager.worldDistance).ToString() + "m";

            if (trackManager.timeToStart >= 0)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = Mathf.Ceil(trackManager.timeToStart).ToString();
                m_CountdownRectTransform.localScale = Vector3.one * (1.0f - (trackManager.timeToStart - Mathf.Floor(trackManager.timeToStart)));
            }
            else
            {
                m_CountdownRectTransform.localScale = Vector3.zero;
            }

            if (trackManager.characterController.inventory != null)
            {
                inventoryIcon.transform.parent.gameObject.SetActive(true);
                inventoryIcon.sprite = trackManager.characterController.inventory.icon;
            }
            else
                inventoryIcon.transform.parent.gameObject.SetActive(false);
        }

        IEnumerator WaitForGameOver()
        {
            m_Finished = true;
            trackManager.StopMove();

            Shader.SetGlobalFloat("_BlinkingValue", 0.0f);

            yield return new WaitForSeconds(2.0f);
            if (currentModifier.OnRunEnd(this))
            {
                if (trackManager.isRerun)
                    manager.SwitchState("GameOver");
                else
                    OpenGameOverPopup();
            }
        }

        protected void ClearPowerup()
        {
            for (int i = 0; i < m_PowerupIcons.Count; ++i)
            {
                if (m_PowerupIcons[i] != null)
                    Destroy(m_PowerupIcons[i].gameObject);
            }

            trackManager.characterController.powerupSource.Stop();

            m_PowerupIcons.Clear();
        }

        public void OpenGameOverPopup()
        {
            premiumForLifeButton.interactable = PlayerData.instance.premium >= 3;

            premiumCurrencyOwned.text = PlayerData.instance.premium.ToString();

            ClearPowerup();

            gameOverPopup.SetActive(true);
        }

        public void GameOver()
        {
            manager.SwitchState("GameOver");
        }

        public void PremiumForLife()
        {
            if (m_GameoverSelectionDone)
                return;

            m_GameoverSelectionDone = true;

            PlayerData.instance.premium -= 3;
            trackManager.characterController.premium -= Mathf.Min(trackManager.characterController.premium, 3);

            SecondWind();
        }

        public void SecondWind()
        {
            trackManager.characterController.currentLife = 1;
            trackManager.isRerun = true;
            StartGame();
        }

        void TutorialCheckObstacleClear()
        {
            if (trackManager.segments.Count == 0)
                return;

            if (AudioListener.pause && !trackManager.characterController.tutorialWaitingForValidation)
            {
                m_DisplayTutorial = false;
                DisplayTutorial(false);
            }

            float ratio = trackManager.currentSegmentDistance / trackManager.currentSegment.worldLength;
            float nextObstaclePosition = m_CurrentSegmentObstacleIndex < trackManager.currentSegment.obstaclePositions.Length ? trackManager.currentSegment.obstaclePositions[m_CurrentSegmentObstacleIndex] : float.MaxValue;

            if (m_CountObstacles && ratio > nextObstaclePosition + 0.05f)
            {
                m_CurrentSegmentObstacleIndex += 1;

                if (!trackManager.characterController.characterCollider.tutorialHitObstacle)
                {
                    m_TutorialClearedObstacle += 1;
                    tutorialValidatedObstacles.text = $"{m_TutorialClearedObstacle}/{k_ObstacleToClear}";
                }

                trackManager.characterController.characterCollider.tutorialHitObstacle = false;

                if (m_TutorialClearedObstacle == k_ObstacleToClear)
                {
                    m_TutorialClearedObstacle = 0;
                    m_CountObstacles = false;
                    m_NextValidSegment = null;
                    trackManager.ChangeZone();

                    tutorialValidatedObstacles.text = "Passed!";

                    if (trackManager.currentZone == 0)
                    {
                        trackManager.characterController.currentTutorialLevel = 3;
                        DisplayTutorial(true);
                    }
                }
            }
            else if (m_DisplayTutorial && ratio > nextObstaclePosition - 0.1f)
                DisplayTutorial(true);
        }

        void DisplayTutorial(bool value)
        {
            if(value)
                Pause(false);
            else
            {
                Resume();
            }

            switch (trackManager.characterController.currentTutorialLevel)
            {
                case 0:
                    sideSlideTuto.SetActive(value);
                    trackManager.characterController.tutorialWaitingForValidation = value;
                    break;
                case 1:
                    upSlideTuto.SetActive(value);
                    trackManager.characterController.tutorialWaitingForValidation = value;
                    break;
                case 2:
                    downSlideTuto.SetActive(value);
                    trackManager.characterController.tutorialWaitingForValidation = value;
                    break;
                case 3:
                    finishTuto.SetActive(value);
                    trackManager.characterController.StopSliding();
                    trackManager.characterController.tutorialWaitingForValidation = value;
                    break;
                default:
                    break;
            }
        }


        public void FinishTutorial()
        {
            PlayerData.instance.tutorialDone = true;
            PlayerData.instance.Save();

            QuitToLoadout();
        }
    }
}