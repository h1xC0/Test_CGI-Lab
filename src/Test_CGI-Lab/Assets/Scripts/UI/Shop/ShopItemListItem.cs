using UnityEngine;
using UnityEngine.UI;

namespace UI.Shop
{
	public class ShopItemListItem : MonoBehaviour
	{
		public Image icon;
		public Text nameText;
		public Text pricetext;
		public Text premiumText;
		public Button buyButton;

		public Text countText;

		public Sprite buyButtonSprite;
		public Sprite disabledButtonSprite;
	}
}
