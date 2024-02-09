using Characters;
using Consumable;
using GameManager;
using Themes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Shop
{
    public class ShopUI : MonoBehaviour
    {
        public ConsumableDatabase consumableDatabase;

        public ShopItemList itemList;
        public ShopThemeList themeList;

        [Header("UI")]
        public Text coinCounter;
        public Text premiumCounter;
        public Button cheatButton;

        protected ShopList m_OpenList;

        protected const int k_CheatCoins = 1000000;
        protected const int k_CheatPremium = 1000;

        void Start ()
        {
            PlayerData.Create();

            consumableDatabase.Load();
            CoroutineHandler.StartStaticCoroutine(CharacterDatabase.LoadDatabase());
            CoroutineHandler.StartStaticCoroutine(ThemeDatabase.LoadDatabase());

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        //Disable cheating on non dev build outside of the editor
        cheatButton.interactable = false;
#else
            cheatButton.interactable = true;
#endif

            m_OpenList = itemList;
            itemList.Open();
        }
	
        void Update ()
        {
            coinCounter.text = PlayerData.instance.coins.ToString();
            premiumCounter.text = PlayerData.instance.premium.ToString();
        }

        public void OpenItemList()
        {
            m_OpenList.Close();
            itemList.Open();
            m_OpenList = itemList;
        }

        public void OpenThemeList()
        {
            m_OpenList.Close();
            themeList.Open();
            m_OpenList = themeList;
        }

        public void LoadScene(string scene)
        {
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }

        public void CloseScene()
        {
            SceneManager.UnloadSceneAsync("shop");
            LoadoutState loadoutState = GameManager.GameManager.instance.topState as LoadoutState;
            if(loadoutState != null)
            {
                loadoutState.Refresh();
            }
        }

        public void CheatCoin()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return ; //you can't cheat in production build
#endif

            PlayerData.instance.coins += k_CheatCoins;
            PlayerData.instance.premium += k_CheatPremium;
            PlayerData.instance.Save();
        }
    }
}
