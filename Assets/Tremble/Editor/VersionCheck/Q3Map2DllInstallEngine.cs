using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Networking;

namespace TinyGoose.Tremble.Editor
{
	public enum Q3Map2DllInstallState
	{
		NotInstalled,
		InstalledButWrongVersion,
		Installed,
	}

	public enum Q3Map2DllDownloadEngineState
	{
		Idle,
		Downloading,
		Installing,
		Succeeded,
		Failed
	}

	public static class Q3Map2DllInstallEngine
	{
		private static Q3Map2DllDownloadEngineState s_State = Q3Map2DllDownloadEngineState.Idle;
		private static UnityWebRequest s_DownloadRequest;
		private static UnityWebRequestAsyncOperation s_DownloadOperation;
		private static int s_ProgressTask;


		public static Q3Map2DllInstallState InstallState
		{
			get
			{
#if UNITY_WEBGL || UNITY_TVOS
				// Under tvOS or WebGL, you can't install Q3Map2 - but let's assume it's installed so as
				// not to spam the install window.
				Debug.Log("Unsupported platform! Cannot determine install state.");
				return Q3Map2DllInstallState.Installed;
#endif

				// We flagged this for deletion last time - do it now while we can!
				if (Q3Map2Dll.EDITOR_IsPluginsFolderFlaggedForDeletion)
				{
					Q3Map2Dll.EDITOR_DeletePluginsFolder();
					return Q3Map2DllInstallState.NotInstalled;
				}

				// No data - assume installed for now (we'll catch it next time!)
				if (!VersionCheck.HasData || VersionCheck.CurrentInstalledVersion?.Q3Map2Hash == null)
					return Q3Map2DllInstallState.Installed;

				// No plugins folder? Yeah, not installed!
				if (!Q3Map2Dll.EDITOR_PluginsFolderExists())
					return Q3Map2DllInstallState.NotInstalled;

				// Okay, we have a folder - check if the Git hash matches
				if (TrembleConsts.GAME_NAME.EqualsInvariant(TrembleConsts.ASSET_STORE_GAME_NAME))
				{
					Debug.Log("Nonmatching Plugins githash. Ignoring because sample.");
					return Q3Map2DllInstallState.Installed;
				}

				string currentGitHash = Q3Map2Dll.GetTinyGooseVersionInfo().GitHash;
				string expectedCurrentGitHash = VersionCheck.CurrentInstalledVersion.Q3Map2Hash;

				return currentGitHash.EqualsInvariant(expectedCurrentGitHash)
					? Q3Map2DllInstallState.Installed 						// Has matching Q3Map2 - nothing to do
					: Q3Map2DllInstallState.InstalledButWrongVersion;	// Version mismatch - needs update
			}
		}

		public static bool IsNewerQ3Map2Available => InstallState != Q3Map2DllInstallState.Installed;
		public static bool CanInstallInBackground => InstallState == Q3Map2DllInstallState.NotInstalled && !TrembleConsts.INTERNAL_IsTrembleAssetStoreProject;

		public static async Task<int> GetDownloadSizeBytes()
		{
			// First, get latest version info
			if (!VersionCheck.HasData)
			{
				// Do background check, but in foreground ;)
				TaskCompletionSource<bool> tcs = new();
				VersionCheck.FetchLatestVersionsInBackground(() => tcs.SetResult(true));
				await tcs.Task;
			}

			// Now check size
			int downloadSize = 0;
			string downloadUrl = VersionCheck.NewestAvailableVersion.Q3Map2Url;

			UnityWebRequest request = UnityWebRequest.Head(downloadUrl);
			UnityWebRequestAsyncOperation operation = request.SendWebRequest();

			operation.completed += _ =>
			{
				int.TryParse(request.GetResponseHeader("Content-Length"), out downloadSize);
				request.Dispose();
			};

			while (!operation.isDone)
			{
				await Task.Yield();
			}

			try
			{
				// Sometimes during a Domain Reload, the request gets nulled in a way we can't detect.
				// get_result() internally throws a NullReferenceException in this case, so we'll just
				// catch that here instead (:
				return request.result == UnityWebRequest.Result.Success ? downloadSize : 0;
			}
			catch (NullReferenceException)
			{
				return 0;
			}
		}

		public static Q3Map2DllDownloadEngineState State => s_State;
		public static float Progress => s_DownloadOperation?.progress ?? 0f;

		public static void Reset()
		{
			s_State = Q3Map2DllDownloadEngineState.Idle;
			s_DownloadOperation = null;
			s_DownloadRequest = null;
		}

		public static void StartDownloadAndInstallLatestQ3Map2()
		{
			try
			{
				s_DownloadRequest = UnityWebRequest.Get(VersionCheck.NewestAvailableVersion.Q3Map2Url);
				s_DownloadOperation = s_DownloadRequest.SendWebRequest();
				s_State = Q3Map2DllDownloadEngineState.Downloading;

				s_DownloadOperation.completed += OnDownloadCompleted;

				s_ProgressTask = ProgressUtil.Start("Installing Q3Map2");
			}
			catch (Exception)
			{
				EditorUtility.DisplayDialog(
					title: "Tremble Unavailable",
					message: "You do not have Q3Map2 installed, and Tremble failed to install it. " +
								"Check your internet connection and try again.",
					ok: "Okay");

				ProgressUtil.Fail(s_ProgressTask);
			}
		}

		private static void OnDownloadCompleted(AsyncOperation _)
		{
			if (s_DownloadRequest.result != UnityWebRequest.Result.Success)
			{
				s_State = Q3Map2DllDownloadEngineState.Failed;
				ProgressUtil.Fail(s_ProgressTask);
				return;
			}

			s_State = Q3Map2DllDownloadEngineState.Installing;

			string tempPluginsTpak = Path.Combine(TrembleConsts.EDITOR_GetTrembleInstallFolder(), "Plugins.tpak");
			string pluginsPath = Path.Combine(TrembleConsts.EDITOR_GetTrembleInstallFolder(), "Plugins");

			Q3Map2Dll.EDITOR_DeletePluginsFolder();

			// Unpack and delete tpak
			File.WriteAllBytes(tempPluginsTpak, s_DownloadRequest.downloadHandler.data);
			{
				Task.Run(async () =>
				{
					TpakArchive tpak = new(tempPluginsTpak);
					await tpak.UnpackAsync(pluginsPath);
				}).Wait();
			}
			File.Delete(tempPluginsTpak);

			s_DownloadRequest = null;
			s_DownloadOperation = null;
			ProgressUtil.Succeed(s_ProgressTask);

			EditorApplication.delayCall += () =>
			{
				CompilationPipeline.compilationFinished += _ => s_State = Q3Map2DllDownloadEngineState.Succeeded;

				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
				CompilationPipeline.RequestScriptCompilation();
			};
		}
	}
}