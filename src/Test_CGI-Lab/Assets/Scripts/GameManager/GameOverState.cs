using Characters;
using Sounds;
using Tracks;
using UI;
using UnityEngine;

namespace GameManager
{
	/// <summary>
	/// state pushed on top of the GameManager when the player dies.
	/// </summary>
	public class GameOverState : AState
	{
		public TrackManager trackManager;
		public Canvas canvas;

		public AudioClip gameOverTheme;

		public Leaderboard miniLeaderboard;
		public Leaderboard fullLeaderboard;

		public GameObject addButton;

		public override void Enter(AState from)
		{
			canvas.gameObject.SetActive(true);

			miniLeaderboard.playerEntry.inputName.text = PlayerData.instance.previousName;
		
			miniLeaderboard.playerEntry.score.text = trackManager.score.ToString();
			miniLeaderboard.Populate();

			CreditCoins();

			if (MusicPlayer.instance.GetStem(0) != gameOverTheme)
			{
				MusicPlayer.instance.SetStem(0, gameOverTheme);
				StartCoroutine(MusicPlayer.instance.RestartAllStems());
			}
		}

		public override void Exit(AState to)
		{
			canvas.gameObject.SetActive(false);
			FinishRun();
		}

		public override string GetName()
		{
			return "GameOver";
		}

		public override void Tick()
		{
        
		}

		public void OpenLeaderboard()
		{
			fullLeaderboard.forcePlayerDisplay = false;
			fullLeaderboard.displayPlayer = true;
			fullLeaderboard.playerEntry.playerName.text = miniLeaderboard.playerEntry.inputName.text;
			fullLeaderboard.playerEntry.score.text = trackManager.score.ToString();

			fullLeaderboard.Open();
		}

		public void GoToStore()
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene("shop", UnityEngine.SceneManagement.LoadSceneMode.Additive);
		}


		public void GoToLoadout()
		{
			trackManager.isRerun = false;
			manager.SwitchState("Loadout");
		}

		public void RunAgain()
		{
			trackManager.isRerun = false;
			manager.SwitchState("Game");
		}

		protected void CreditCoins()
		{
			PlayerData.instance.Save();
		}

		protected void FinishRun()
		{
			if(miniLeaderboard.playerEntry.inputName.text == "")
			{
				miniLeaderboard.playerEntry.inputName.text = "Soldier Buddy";
			}
			else
			{
				PlayerData.instance.previousName = miniLeaderboard.playerEntry.inputName.text;
			}

			PlayerData.instance.InsertScore(trackManager.score, miniLeaderboard.playerEntry.inputName.text );

			CharacterCollider.DeathEvent de = trackManager.characterController.characterCollider.deathData;

			PlayerData.instance.Save();

			trackManager.End();
		}

		//----------------
	}
}
