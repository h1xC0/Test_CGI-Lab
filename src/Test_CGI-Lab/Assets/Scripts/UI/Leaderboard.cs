using UnityEngine;

namespace UI
{
	public class Leaderboard : MonoBehaviour
	{
		public RectTransform entriesRoot;
		public int entriesCount;

		public HighscoreUI playerEntry;
		public bool forcePlayerDisplay;
		public bool displayPlayer = true;

		public void Open()
		{
			gameObject.SetActive(true);

			Populate();
		}

		public void Close()
		{
			gameObject.SetActive(false);
		}

		public void Populate()
		{
			playerEntry.transform.SetAsLastSibling();
			for(int i = 0; i < entriesCount; ++i)
			{
				entriesRoot.GetChild(i).gameObject.SetActive(true);
			}

			int localStart = 0;
			int place = -1;
			int localPlace = -1;

			if (displayPlayer)
			{
				place = PlayerData.instance.GetScorePlace(int.Parse(playerEntry.score.text));
				localPlace = place - localStart;
			}

			if (localPlace >= 0 && localPlace < entriesCount && displayPlayer)
			{
				playerEntry.gameObject.SetActive(true);
				playerEntry.transform.SetSiblingIndex(localPlace);
			}

			if (!forcePlayerDisplay || PlayerData.instance.highscores.Count < entriesCount)
				entriesRoot.GetChild(entriesRoot.transform.childCount - 1).gameObject.SetActive(false);

			int currentHighScore = localStart;

			for (int i = 0; i < entriesCount; ++i)
			{
				HighscoreUI hs = entriesRoot.GetChild(i).GetComponent<HighscoreUI>();

				if (hs == playerEntry || hs == null)
				{
					continue;
				}

				if (PlayerData.instance.highscores.Count > currentHighScore)
				{
					hs.gameObject.SetActive(true);
					hs.playerName.text = PlayerData.instance.highscores[currentHighScore].name;
					hs.number.text = (localStart + i + 1).ToString();
					hs.score.text = PlayerData.instance.highscores[currentHighScore].score.ToString();

					currentHighScore++;
				}
				else
					hs.gameObject.SetActive(false);
			}

			if (forcePlayerDisplay) 
				playerEntry.gameObject.SetActive(true);

			playerEntry.number.text = (place + 1).ToString();
		}
	}
}
