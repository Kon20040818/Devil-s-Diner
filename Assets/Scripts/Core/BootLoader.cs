// ============================================================
// BootLoader.cs
// BootScene 専用の起動コンポーネント。
// タイトル画面を表示し、ゲーム開始時に BaseScene へ遷移する。
// ============================================================
using UnityEngine;

/// <summary>
/// BootScene に配置される起動コンポーネント。
/// GameManager.Awake() でシングルトンが確立された後、
/// タイトル画面を表示し、操作に応じて BaseScene へ遷移する。
/// </summary>
public sealed class BootLoader : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[BootLoader] GameManager が初期化されていません。");
            return;
        }

        // シーンBGM自動切替を結線
        AudioEventConnector.WireSceneBGM();

        ShowTitleScreen();
    }

    private void ShowTitleScreen()
    {
        // TitleScreenUI を生成
        var titleGO = new GameObject("TitleScreenUI");
        var titleUI = titleGO.AddComponent<TitleScreenUI>();

        // セーブデータの有無を判定
        bool hasSave = GameManager.Instance.SaveData != null
            && GameManager.Instance.SaveData.HasSaveData();

        titleUI.OnStartGame += () =>
        {
            Destroy(titleGO);
            GameManager.Instance.TransitionToScene("BaseScene");
        };

        titleUI.OnContinueGame += () =>
        {
            Destroy(titleGO);
            if (GameManager.Instance.SaveData != null)
            {
                GameManager.Instance.SaveData.Load();
            }
            GameManager.Instance.TransitionToScene("BaseScene");
        };

        titleUI.Show(hasSave);
    }
}
