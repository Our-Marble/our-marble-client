using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬의 UI 캔버스에 뷰 컴포넌트를 붙이고 UIManager에 연결한다.
/// 캔버스 구조를 바꾼 뒤 다시 실행해도 된다(이미 있는 컴포넌트는 참조만 다시 채운다).
/// </summary>
public static class GameUISetup
{
    [MenuItem("Tools/Our Marble/Game UI 연결")]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        int missing = 0;

        // 캔버스가 "UI" 같은 부모 아래에 묶여 있어도 찾는다 (꺼져 있어도 찾는다)
        GameObject FindRoot(string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName) return root;
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == rootName) return child.gameObject;
            }
            Debug.LogWarning($"[GameUISetup] '{rootName}' 캔버스가 씬에 없습니다.");
            missing++;
            return null;
        }

        T Attach<T>(GameObject target) where T : UIView
        {
            if (target == null) return null;
            var view = target.GetComponent<T>();
            if (view == null) view = Undo.AddComponent<T>(target); // Reset에서 Bind 실행
            Undo.RecordObject(view, "Bind UI");
            view.Bind();

            // 열기·닫기 연출: 창과 어두운 배경에 CanvasGroup (알파 조절용)
            EnsureCanvasGroup(target.transform, view.PanelPath);
            EnsureCanvasGroup(target.transform, view.DimPath);
            view.BindTransitions();

            // 버튼 눌림 연출
            foreach (var button in target.GetComponentsInChildren<Button>(true))
                if (button.GetComponent<ButtonPressEffect>() == null)
                    Undo.AddComponent<ButtonPressEffect>(button.gameObject);

            EditorUtility.SetDirty(view);
            return view;
        }

        // 플레이어 정보 카드 4장
        var playerInfos = new PlayerInfoView[4];
        var playerCanvas = FindRoot("Canvas_PlayerInfo");
        for (int i = 0; i < 4 && playerCanvas != null; i++)
        {
            var card = playerCanvas.transform.Find($"PlayerInfo_{i + 1}");
            if (card == null) { Debug.LogWarning($"[GameUISetup] PlayerInfo_{i + 1} 없음"); missing++; continue; }
            playerInfos[i] = Attach<PlayerInfoView>(card.gameObject);
        }

        // 주사위 버튼은 누르고 있는 동안을 알아야 한다
        var diceCanvas = FindRoot("Canvas_DiceRoll");
        var rollButton = diceCanvas != null ? diceCanvas.transform.Find("DiceRollPanel/RollButton") : null;
        if (rollButton != null && rollButton.GetComponent<HoldButton>() == null)
            Undo.AddComponent<HoldButton>(rollButton.gameObject);

        var views = new (string field, UIView view)[]
        {
            ("topBar", Attach<TopBarView>(FindRoot("Canvas_TopBar"))),
            ("currentTurn", Attach<CurrentTurnView>(FindRoot("Canvas_CurrentTurn"))),
            ("diceRoll", Attach<DiceRollView>(diceCanvas)),
            ("diceResult", Attach<DiceResultView>(FindRoot("Canvas_DiceResult"))),
            ("roomSetup", Attach<RoomSetupView>(FindRoot("Canvas_RoomSetup"))),
            ("purchase", Attach<PurchasePopupView>(FindRoot("Canvas_PurchasePopup"))),
            ("takeover", Attach<TakeoverPopupView>(FindRoot("Canvas_TakeoverPopup"))),
            ("sell", Attach<SellPopupView>(FindRoot("Canvas_SellPopup"))),
            ("islandEscape", Attach<IslandEscapeView>(FindRoot("Canvas_IslandEscape"))),
            ("cardDraw", Attach<CardDrawView>(FindRoot("Canvas_CardDraw"))),
            ("destinationSelect", Attach<DestinationSelectView>(FindRoot("Canvas_DestinationSelect"))),
            ("tileInfo", Attach<TileInfoView>(FindRoot("Canvas_TileInfo"))),
            ("gameResult", Attach<GameResultView>(FindRoot("Canvas_GameResult"))),
            ("exitConfirm", Attach<ExitConfirmView>(FindRoot("Canvas_ExitConfirm"))),
            ("settings", Attach<SettingsView>(FindRoot("Canvas_Settings"))),
        };

        // UIManager (없으면 만든다)
        var manager = UnityEngine.Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            var go = new GameObject("UIManager");
            Undo.RegisterCreatedObjectUndo(go, "Create UIManager");
            manager = go.AddComponent<UIManager>();
        }

        var so = new SerializedObject(manager);
        var infos = so.FindProperty("playerInfos");
        infos.arraySize = playerInfos.Length;
        for (int i = 0; i < playerInfos.Length; i++)
            if (playerInfos[i] != null) infos.GetArrayElementAtIndex(i).objectReferenceValue = playerInfos[i];
        foreach (var (field, view) in views)
        {
            // 못 찾은 뷰는 기존 연결을 지우지 않는다
            if (view != null) so.FindProperty(field).objectReferenceValue = view;
        }

        // 황금열쇠 카드 덱: 비어 있으면 프로젝트의 CardDeckData를 찾아 연결한다 (카드 이름·설명·아이콘 조회용)
        var deckProperty = so.FindProperty("cardDeck");
        if (deckProperty != null && deckProperty.objectReferenceValue == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:CardDeckData");
            if (guids.Length > 0)
                deckProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardDeckData>(AssetDatabase.GUIDToAssetPath(guids[0]));
            else
                Debug.LogWarning("[GameUISetup] CardDeckData 에셋이 없어 카드 덱을 연결하지 못했습니다. UIManager의 Card Deck에 직접 넣어주세요.");
        }
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = manager.gameObject;

        if (missing == 0) Debug.Log("[GameUISetup] UI 연결 완료. 씬을 저장하세요.");
        else Debug.LogWarning($"[GameUISetup] 연결 완료, 찾지 못한 항목 {missing}개. 위 경고를 확인하세요.");
    }

    private static void EnsureCanvasGroup(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var target = root.Find(path);
        if (target == null)
        {
            Debug.LogWarning($"[GameUISetup] 연출 대상 '{root.name}/{path}' 없음");
            return;
        }
        if (target.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(target.gameObject);
    }
}
