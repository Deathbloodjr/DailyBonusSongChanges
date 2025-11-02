using HarmonyLib;
using LightWeightJsonParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DailyBonusSongChanges.Patches
{
    internal class DailyBonusSongChangesPatch
    {
        public static int NumDailyMusicCount => Plugin.Instance.ConfigNumDailyBonusSongs.Value;
        public static int MinLevel => Plugin.Instance.ConfigMinLevelBonusSongs.Value;
        public static int MaxLevel => Plugin.Instance.ConfigMaxLevelBonusSongs.Value;

		[HarmonyPatch(typeof(SongSelectManager), "UpdateDailyMusicsInfo")]
		[HarmonyPatch(MethodType.Normal)]
		[HarmonyPrefix]
		private static bool SongSelectManager_UpdateDailyMusicsInfo_Prefix(SongSelectManager __instance)
		{
			DailyMusicsInfo dailyMusicsInfo;
			TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MyDataManager.PlayData.GetDailyMusicsInfo(0, out dailyMusicsInfo);
			DateTime today = DateTime.Today;
			DateTime dateTime = DateTime.Today.AddDays(-1.0);
			DateTime dateTime2 = DateTime.Today.AddDays(1.0);
#if TAIKO_MONO
			DateTime t = (DateTime.Now.Hour >= 5) ? new DateTime(today.Year, today.Month, today.Day, 5, 0, 0) : new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 5, 0, 0);
			DateTime t2 = (DateTime.Now.Hour >= 5) ? new DateTime(dateTime2.Year, dateTime2.Month, dateTime2.Day, 5, 0, 0) : new DateTime(today.Year, today.Month, today.Day, 5, 0, 0);
#elif TAIKO_IL2CPP
			Il2CppSystem.DateTime t = (DateTime.Now.Hour >= 5) ? new Il2CppSystem.DateTime(today.Year, today.Month, today.Day, 5, 0, 0) : new Il2CppSystem.DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 5, 0, 0);
			Il2CppSystem.DateTime t2 = (DateTime.Now.Hour >= 5) ? new Il2CppSystem.DateTime(dateTime2.Year, dateTime2.Month, dateTime2.Day, 5, 0, 0) : new Il2CppSystem.DateTime(today.Year, today.Month, today.Day, 5, 0, 0);
#endif
			if (dailyMusicsInfo.updateTime < t || dailyMusicsInfo.updateTime > t2)
			{
				List<int> list = new List<int>(NumDailyMusicCount);
				List<int> list2 = new List<int>(NumDailyMusicCount);
#if TAIKO_MONO
				int num = (from song in __instance.UnsortedSongList
						   select song.SongGenre).Distinct<int>().Count<int>();
#elif TAIKO_IL2CPP
				List<SongSelectManager.Song> unsortedSongList = new List<SongSelectManager.Song>();
                for (int i = 0; i < __instance.UnsortedSongList.Count; i++)
                {
					unsortedSongList.Add(__instance.UnsortedSongList[i]);
				}
				int num = (from song in unsortedSongList
						   select song.SongGenre).Distinct<int>().Count<int>();
#endif

				for (int i = 0; i < NumDailyMusicCount; i++)
				{
					int num2;
					SongSelectManager.Song song3;
					do
					{
						num2 = UnityEngine.Random.Range(0, __instance.UnsortedSongList.Count);
						song3 = __instance.UnsortedSongList[num2];
					}
					while ((dailyMusicsInfo.uniqueIds.Contains(song3.UniqueId) && i < __instance.UnsortedSongList.Count - NumDailyMusicCount)
					|| (list.Contains(num2) && i < __instance.UnsortedSongList.Count) || (list2.Contains(song3.SongGenre) && i < num)
					|| (song3.Stars[3] < MinLevel || song3.Stars[3] > MaxLevel) && (song3.Stars[4] < MinLevel || song3.Stars[4] > MaxLevel));
					list.Add(num2);
					list2.Add(song3.SongGenre);
				}


				for (int j = 0; j < NumDailyMusicCount; j++)
				{
					dailyMusicsInfo.uniqueIds[j] = __instance.UnsortedSongList[list[j]].UniqueId;
					dailyMusicsInfo.isPlayed[j] = false;
				}
#if TAIKO_MONO
				dailyMusicsInfo.updateTime = DateTime.Now;
				TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MyDataManager.PlayData.SetDailyMusicsInfo(0, dailyMusicsInfo, true);
#elif TAIKO_IL2CPP
				dailyMusicsInfo.updateTime = Il2CppSystem.DateTime.Now;
				TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MyDataManager.PlayData.SetDailyMusicsInfo(0, ref dailyMusicsInfo, true);
#endif
			}

			List<SongSelectManager.Song> SelectedSongs = new List<SongSelectManager.Song>();
			for (int k = 0; k < NumDailyMusicCount; k++)
			{
				foreach (SongSelectManager.Song song2 in __instance.UnsortedSongList)
				{
					if (song2.UniqueId == dailyMusicsInfo.uniqueIds[k] && !dailyMusicsInfo.isPlayed[k])
					{
						song2.DailyBonus = true;
                        SelectedSongs.Add(song2);
                        break;
					}
				}
			}
			WriteDailyPlaylistJson(SelectedSongs);

            return false;
		}


		private static void WriteDailyPlaylistJson(List<SongSelectManager.Song> songs)
		{
			var orderedSongs = songs.OrderBy((x) => Math.Max(x.Stars[3], x.Stars[4])).ToList();

			LWJsonObject json = new LWJsonObject()
				.Add("playlistName", "DailyBonusSongs")
				.Add("order", 0)
				.Add("songs", new LWJsonArray());

			var songArray = json["songs"].AsArray();
			for (int i = 0; i < orderedSongs.Count; i++)
			{
				LWJsonObject songJson = new LWJsonObject()
					.Add("songId", orderedSongs[i].Id)
					.Add("genreNo", orderedSongs[i].SongGenre)
					.Add("isDlc", false);
				songArray.Add(songJson);
			}

			File.WriteAllText(Path.Combine(Plugin.Instance.ConfigPlaylistJson.Value, "DailyBonusSongs.json"), json.ToString());
		}


		[HarmonyPatch(typeof(DailyMusicsInfo), "Reset")]
		[HarmonyPatch(MethodType.Normal)]
		[HarmonyPrefix]
		private static bool DailyMusicsInfo_Reset_Prefix(ref DailyMusicsInfo __instance)
		{
#if TAIKO_MONO
			__instance.updateTime = default(DateTime);
#elif TAIKO_IL2CPP
			__instance.updateTime = default(Il2CppSystem.DateTime);
#endif
			__instance.uniqueIds = new int[NumDailyMusicCount];
			__instance.isPlayed = new bool[NumDailyMusicCount];
			return false;
		}

		[HarmonyPatch(typeof(DailyMusicsInfo), "IsValid")]
		[HarmonyPatch(MethodType.Normal)]
		[HarmonyPrefix]
		private static bool DailyMusicsInfo_IsValid_Prefix(DailyMusicsInfo __instance, ref bool __result)
		{
			bool NewIsValid()
			{
				return __instance.uniqueIds != null && __instance.isPlayed != null && __instance.uniqueIds.Length == NumDailyMusicCount && __instance.isPlayed.Length == NumDailyMusicCount;
			}

			__result = NewIsValid();

			return false;
		}



		[HarmonyPatch(typeof(ResultPlayer), "InitializeEnsoParts")]
		[HarmonyPatch(MethodType.Normal)]
		[HarmonyPrefix]
		public static bool ResultPlayer_InitializeEnsoParts_Postfix(ResultPlayer __instance, EnsoPlayingParameter param, EnsoPostInfo post, EnsoData.Settings settings)
		{
			__instance.ensoParam = param;
			__instance.playerNum = settings.playerNum;
			__instance.playerNo = (int)post.playerNo;
			__instance.isRankMatch = (settings.rankMatchType > EnsoData.RankMatchType.None);
			if (post.pos != Vector3.zero)
			{
				RectTransform rectTransform = __instance.transform as RectTransform;
				if (rectTransform != null)
				{
					rectTransform.localPosition += post.pos;
				}
				else
				{
					__instance.transform.localPosition += post.pos;
				}
			}
			__instance.graphicManager = GameObject.Find("EnsoGraphicManager").GetComponent<EnsoGraphicManager>();
			__instance.isAutoPlay = (settings.ensoPlayerSettings[__instance.playerNo].special == DataConst.SpecialTypes.Auto);
			__instance.iconEnso.InitializeEnsoParts(param, post, ref settings);
			__instance.iconShinuchi.InitializeEnsoParts(param, post, ref settings);
			__instance.localSettings = settings;
			__instance.touchDisable = true;
			__instance.playDataManager = TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MyDataManager.PlayData;
			DailyMusicsInfo dailyMusicsInfo;
			__instance.playDataManager.GetDailyMusicsInfo(0, out dailyMusicsInfo);
#if TAIKO_MONO
			__instance.isDailyBonus = false;
#endif
			__instance.isPlayCoinSe = false;
			__instance.isPlayScoreSe = false;
#if TAIKO_MONO
			for (int i = 0; i < NumDailyMusicCount; i++)
			{
				if (dailyMusicsInfo.uniqueIds[i] == settings.musicUniqueId && !dailyMusicsInfo.isPlayed[i])
				{
					__instance.isDailyBonus = true;
				}
			}
#endif
			__instance.resultSyncFlag[0] = (__instance.resultSyncFlag[1] = false);
			__instance.resultWaitFlag = false;
			__instance.resultLastFlag = false;
			__instance.resultIsSync = false;
			__instance.isEndTamashiiGaugeAnim = false;
			if (settings.playerNum == 2)
			{
				__instance.resultWaitFlag = true;
				if (__instance.playerNo == 0)
				{
					__instance.resultBase.transform.localPosition = __instance.pos_P1;
				}
				else
				{
					__instance.resultBase.transform.localPosition = __instance.pos_P2;
				}
			}
			return false;
		}
		[HarmonyPatch(typeof(ResultPlayer), "SetDairyMusicPlayed")]
		[HarmonyPatch(MethodType.Normal)]
		[HarmonyPrefix]
		public static bool ResultPlayer_SetDairyMusicPlayed_Prefix(ResultPlayer __instance)
		{
			__instance.playDataManager = TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MyDataManager.PlayData;
			DailyMusicsInfo dailyMusicsInfo;
			__instance.playDataManager.GetDailyMusicsInfo(0, out dailyMusicsInfo);
			for (int i = 0; i < NumDailyMusicCount; i++)
			{
				if (dailyMusicsInfo.uniqueIds[i] == __instance.localSettings.musicUniqueId && !dailyMusicsInfo.isPlayed[i])
				{
					dailyMusicsInfo.isPlayed[i] = true;
#if TAIKO_MONO
					__instance.playDataManager.SetDailyMusicsInfo(0, dailyMusicsInfo, false);
#elif TAIKO_IL2CPP
					__instance.playDataManager.SetDailyMusicsInfo(0, ref dailyMusicsInfo, false);
#endif
				}
			}
			return false;
		}

	}
}
