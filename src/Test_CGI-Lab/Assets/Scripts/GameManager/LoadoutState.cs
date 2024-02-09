using System.Collections;
using System.Collections.Generic;
using Characters;
using Consumable;
using Sounds;
using Themes;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace GameManager
{
	public class LoadoutState : AState
	{
		public Canvas inventoryCanvas;

		[Header("Char UI")]
		public TMP_Text charNameDisplay;
		public RectTransform charSelect;
		public Transform charPosition;

		[Header("Theme UI")]
		public TMP_Text themeNameDisplay;
		public RectTransform themeSelect;
		public Image themeIcon;

		[Header("PowerUp UI")]
		public RectTransform powerupSelect;
		public Image powerupIcon;
		public TMP_Text powerupCount;
		public Sprite noItemIcon;

		[Header("Other Data")]
		public Leaderboard leaderboard;
		public Button runButton;

		public GameObject tutorialBlocker;
		public GameObject tutorialPrompt;

		public MeshFilter skyMeshFilter;
		public MeshFilter UIGroundFilter;

		public AudioClip menuTheme;


		[Header("Prefabs")]
		public ConsumableIcon consumableIcon;

		Consumable.Consumable.ConsumableType m_PowerupToUse = Consumable.Consumable.ConsumableType.NONE;

		protected GameObject m_Character;
		protected int m_UsedAccessory = -1;
		protected int m_UsedPowerupIndex;
		protected bool m_IsLoadingCharacter;

		protected Modifier m_CurrentModifier = new Modifier();

		protected const float k_CharacterRotationSpeed = 45f;
		protected const string k_ShopSceneName = "shop";
		protected const float k_OwnedAccessoriesCharacterOffset = -0.1f;
		protected int k_UILayer;
		protected readonly Quaternion k_FlippedYAxisRotation = Quaternion.Euler (0f, 180f, 0f);

		public override void Enter(AState from)
		{
			tutorialBlocker.SetActive(!PlayerData.instance.tutorialDone);
			tutorialPrompt.SetActive(false);

			inventoryCanvas.gameObject.SetActive(true);

			charNameDisplay.text = "";
			themeNameDisplay.text = "";

			k_UILayer = LayerMask.NameToLayer("UI");

			skyMeshFilter.gameObject.SetActive(true);
			UIGroundFilter.gameObject.SetActive(true);

			Shader.SetGlobalFloat("_BlinkingValue", 0.0f);

			if (MusicPlayer.instance.GetStem(0) != menuTheme)
			{
				MusicPlayer.instance.SetStem(0, menuTheme);
				StartCoroutine(MusicPlayer.instance.RestartAllStems());
			}

			runButton.interactable = false;
			runButton.GetComponentInChildren<TMP_Text>().text = "Loading...";

			if(m_PowerupToUse != Consumable.Consumable.ConsumableType.NONE)
			{
				if (!PlayerData.instance.consumables.ContainsKey(m_PowerupToUse) || PlayerData.instance.consumables[m_PowerupToUse] == 0)
					m_PowerupToUse = Consumable.Consumable.ConsumableType.NONE;
			}

			Refresh();
		}

		public override void Exit(AState to)
		{
			inventoryCanvas.gameObject.SetActive(false);

			if (m_Character != null) Addressables.ReleaseInstance(m_Character);

			GameState gs = to as GameState;

			skyMeshFilter.gameObject.SetActive(false);
			UIGroundFilter.gameObject.SetActive(false);

			if (gs != null)
			{
				gs.currentModifier = m_CurrentModifier;
			
				m_CurrentModifier = new Modifier();

				if (m_PowerupToUse != Consumable.Consumable.ConsumableType.NONE)
				{
					PlayerData.instance.Consume(m_PowerupToUse);
					Consumable.Consumable inv = Instantiate(ConsumableDatabase.GetConsumbale(m_PowerupToUse));
					inv.gameObject.SetActive(false);
					gs.trackManager.characterController.inventory = inv;
				}
			}
		}

		public void Refresh()
		{
			PopulatePowerup();

			StartCoroutine(PopulateCharacters());
			StartCoroutine(PopulateTheme());
		}

		public override string GetName()
		{
			return "Loadout";
		}

		public override void Tick()
		{
			if (!runButton.interactable)
			{
				bool interactable = ThemeDatabase.loaded && CharacterDatabase.loaded;
				if(interactable)
				{
					runButton.interactable = true;
					runButton.GetComponentInChildren<TMP_Text>().text = "Run!";

					//we can always enabled, as the parent will be disabled if tutorial is already done
					tutorialPrompt.SetActive(true);
				}
			}

			if(m_Character != null)
			{
				m_Character.transform.Rotate(0, k_CharacterRotationSpeed * Time.deltaTime, 0, Space.Self);
			}

			charSelect.gameObject.SetActive(PlayerData.instance.characters.Count > 1);
			themeSelect.gameObject.SetActive(PlayerData.instance.themes.Count > 1);
		}

		public void GoToStore()
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(k_ShopSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
		}

		public void ChangeCharacter(int dir)
		{
			PlayerData.instance.usedCharacter += dir;
			if (PlayerData.instance.usedCharacter >= PlayerData.instance.characters.Count)
				PlayerData.instance.usedCharacter = 0;
			else if(PlayerData.instance.usedCharacter < 0)
				PlayerData.instance.usedCharacter = PlayerData.instance.characters.Count-1;

			StartCoroutine(PopulateCharacters());
		}

		public void ChangeTheme(int dir)
		{
			PlayerData.instance.usedTheme += dir;
			if (PlayerData.instance.usedTheme >= PlayerData.instance.themes.Count)
				PlayerData.instance.usedTheme = 0;
			else if (PlayerData.instance.usedTheme < 0)
				PlayerData.instance.usedTheme = PlayerData.instance.themes.Count - 1;

			StartCoroutine(PopulateTheme());
		}

		public IEnumerator PopulateTheme()
		{
			ThemeData t = null;

			while (t == null)
			{
				t = ThemeDatabase.GetThemeData(PlayerData.instance.themes[PlayerData.instance.usedTheme]);
				yield return null;
			}

			themeNameDisplay.text = t.themeName;
			themeIcon.sprite = t.themeIcon;

			skyMeshFilter.sharedMesh = t.skyMesh;
			UIGroundFilter.sharedMesh = t.UIGroundMesh;
		}

		public IEnumerator PopulateCharacters()
		{
			m_UsedAccessory = -1;

			if (!m_IsLoadingCharacter)
			{
				m_IsLoadingCharacter = true;
				GameObject newChar = null;
				while (newChar == null)
				{
					Character c = CharacterDatabase.GetCharacter(PlayerData.instance.characters[PlayerData.instance.usedCharacter]);

					if (c != null)
					{

						Vector3 pos = charPosition.transform.position;
						pos.x = 0.0f;
						charPosition.transform.position = pos;

						AsyncOperationHandle op = Addressables.InstantiateAsync(c.characterName);
						yield return op;
						if (op.Result == null || !(op.Result is GameObject))
						{
							Debug.LogWarning(string.Format("Unable to load character {0}.", c.characterName));
							yield break;
						}
						newChar = op.Result as GameObject;
						Helpers.SetRendererLayerRecursive(newChar, k_UILayer);
						newChar.transform.SetParent(charPosition, false);
						newChar.transform.rotation = k_FlippedYAxisRotation;

						if (m_Character != null)
							Addressables.ReleaseInstance(m_Character);

						m_Character = newChar;
						charNameDisplay.text = c.characterName;

						m_Character.transform.localPosition = Vector3.right * 1000;

						yield return new WaitForEndOfFrame();
						m_Character.transform.localPosition = Vector3.zero;
					}
					else
						yield return new WaitForSeconds(1.0f);
				}
				m_IsLoadingCharacter = false;
			}
		}

		void PopulatePowerup()
		{
			powerupIcon.gameObject.SetActive(true);

			if (PlayerData.instance.consumables.Count > 0)
			{
				Consumable.Consumable c = ConsumableDatabase.GetConsumbale(m_PowerupToUse);

				powerupSelect.gameObject.SetActive(true);
				if (c != null)
				{
					powerupIcon.sprite = c.icon;
					powerupCount.text = PlayerData.instance.consumables[m_PowerupToUse].ToString();
				}
				else
				{
					powerupIcon.sprite = noItemIcon;
					powerupCount.text = "";
				}
			}
			else
			{
				powerupSelect.gameObject.SetActive(false);
			}
		}

		public void ChangeConsumable(int dir)
		{
			bool found = false;
			do
			{
				m_UsedPowerupIndex += dir;
				if(m_UsedPowerupIndex >= (int)Consumable.Consumable.ConsumableType.MAX_COUNT)
				{
					m_UsedPowerupIndex = 0; 
				}
				else if(m_UsedPowerupIndex < 0)
				{
					m_UsedPowerupIndex = (int)Consumable.Consumable.ConsumableType.MAX_COUNT - 1;
				}

				int count = 0;
				if(PlayerData.instance.consumables.TryGetValue((Consumable.Consumable.ConsumableType)m_UsedPowerupIndex, out count) && count > 0)
				{
					found = true;
				}

			} while (m_UsedPowerupIndex != 0 && !found);

			m_PowerupToUse = (Consumable.Consumable.ConsumableType)m_UsedPowerupIndex;
			PopulatePowerup();
		}

		public void UnequipPowerup()
		{
			m_PowerupToUse = Consumable.Consumable.ConsumableType.NONE;
		}
	

		public void SetModifier(Modifier modifier)
		{
			m_CurrentModifier = modifier;
		}

		public void StartGame()
		{
			if (PlayerData.instance.tutorialDone)
			{
				if (PlayerData.instance.ftueLevel == 1)
				{
					PlayerData.instance.ftueLevel = 2;
					PlayerData.instance.Save();
				}
			}

			manager.SwitchState("Game");
		}

		public void Openleaderboard()
		{
			leaderboard.displayPlayer = false;
			leaderboard.forcePlayerDisplay = false;
			leaderboard.Open();
		}
	}
}
