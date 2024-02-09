using Consumable;
using UnityEngine;

namespace UI.Shop
{
	public class ShopItemList : ShopList
	{
		static public Consumable.Consumable.ConsumableType[] s_ConsumablesTypes = System.Enum.GetValues(typeof(Consumable.Consumable.ConsumableType)) as Consumable.Consumable.ConsumableType[];

		public override void Populate()
		{
			m_RefreshCallback = null;
			foreach (Transform t in listRoot)
			{
				Destroy(t.gameObject);
			}

			for(int i = 0; i < s_ConsumablesTypes.Length; ++i)
			{
				Consumable.Consumable c = ConsumableDatabase.GetConsumbale(s_ConsumablesTypes[i]);
				if(c != null)
				{
					prefabItem.InstantiateAsync().Completed += (op) =>
					{
						if (op.Result == null || !(op.Result is GameObject))
						{
							Debug.LogWarning(string.Format("Unable to load item shop list {0}.", prefabItem.RuntimeKey));
							return;
						}
						GameObject newEntry = op.Result;
						newEntry.transform.SetParent(listRoot, false);

						ShopItemListItem itm = newEntry.GetComponent<ShopItemListItem>();

						itm.buyButton.image.sprite = itm.buyButtonSprite;

						itm.nameText.text = c.GetConsumableName();
						itm.pricetext.text = c.GetPrice().ToString();

						if (c.GetPremiumCost() > 0)
						{
							itm.premiumText.transform.parent.gameObject.SetActive(true);
							itm.premiumText.text = c.GetPremiumCost().ToString();
						}
						else
						{
							itm.premiumText.transform.parent.gameObject.SetActive(false);
						}

						itm.icon.sprite = c.icon;

						itm.countText.gameObject.SetActive(true);

						itm.buyButton.onClick.AddListener(delegate() { Buy(c); });
						m_RefreshCallback += delegate() { RefreshButton(itm, c); };
						RefreshButton(itm, c);
					};
				}
			}
		}

		protected void RefreshButton(ShopItemListItem itemList, Consumable.Consumable c)
		{
			int count = 0;
			PlayerData.instance.consumables.TryGetValue(c.GetConsumableType(), out count);
			itemList.countText.text = count.ToString();

			if (c.GetPrice() > PlayerData.instance.coins)
			{
				itemList.buyButton.interactable = false;
				itemList.pricetext.color = Color.red;
			}
			else
			{
				itemList.pricetext.color = Color.black;
			}

			if (c.GetPremiumCost() > PlayerData.instance.premium)
			{
				itemList.buyButton.interactable = false;
				itemList.premiumText.color = Color.red;
			}
			else
			{
				itemList.premiumText.color = Color.black;
			}
		}

		public void Buy(Consumable.Consumable c)
		{
			PlayerData.instance.coins -= c.GetPrice();
			PlayerData.instance.premium -= c.GetPremiumCost();
			PlayerData.instance.Add(c.GetConsumableType());
			PlayerData.instance.Save();

			Refresh();
		}
	}
}
