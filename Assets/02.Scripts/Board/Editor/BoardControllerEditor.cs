using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(BoardController))]
public class BoardControllerEditor : Editor
{
    private const string GeneratedRootName = "BoardRoot";

    private SerializedProperty tilesPerSide;
    private SerializedProperty tileSpacing;
    private SerializedProperty cornerSpacingMultiplier;
    private SerializedProperty cornerTilePrefab;
    private SerializedProperty leftTopTilePrefab;
    private SerializedProperty leftBottomTilePrefab;
    private SerializedProperty rightTopTilePrefab;
    private SerializedProperty rightBottomTilePrefab;
    private SerializedProperty boardTiles;

    private void OnEnable()
    {
        tilesPerSide = serializedObject.FindProperty("tilesPerSide");
        tileSpacing = serializedObject.FindProperty("tileSpacing");
        cornerSpacingMultiplier = serializedObject.FindProperty("cornerSpacingMultiplier");
        cornerTilePrefab = serializedObject.FindProperty("cornerTilePrefab");
        leftTopTilePrefab = serializedObject.FindProperty("leftTopTilePrefab");
        leftBottomTilePrefab = serializedObject.FindProperty("leftBottomTilePrefab");
        rightTopTilePrefab = serializedObject.FindProperty("rightTopTilePrefab");
        rightBottomTilePrefab = serializedObject.FindProperty("rightBottomTilePrefab");
        boardTiles = serializedObject.FindProperty("boardTiles");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            DrawPropertiesExcluding(
                serializedObject,
                "boardTiles",
                "tilesPerSide",
                "tileSpacing",
                "cornerSpacingMultiplier",
                "cornerTilePrefab",
                "leftTopTilePrefab",
                "rightTopTilePrefab",
                "rightBottomTilePrefab",
                "leftBottomTilePrefab");
        }
        else
        {
            DrawPropertiesExcluding(serializedObject, "boardTiles");
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(boardTiles, true);
        }

        serializedObject.ApplyModifiedProperties();

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        bool hasAllPrefabs =
            cornerTilePrefab.objectReferenceValue != null &&
            leftTopTilePrefab.objectReferenceValue != null &&
            rightTopTilePrefab.objectReferenceValue != null &&
            rightBottomTilePrefab.objectReferenceValue != null &&
            leftBottomTilePrefab.objectReferenceValue != null;

        if (!hasAllPrefabs)
        {
            EditorGUILayout.HelpBox("코너와 4종 타일 프리팹을 모두 지정해 주세요.", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(!hasAllPrefabs))
        {
            if (GUILayout.Button("보드 타일 배치"))
            {
                GenerateBoard();
            }
        }
    }

    private void GenerateBoard()
    {
        BoardController controller = (BoardController)target;
        Transform existingRoot = controller.transform.Find(GeneratedRootName);

        Undo.SetCurrentGroupName("Generate Board Tiles");
        int undoGroup = Undo.GetCurrentGroup();

        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot.gameObject);
        }

        RemoveLegacyRootTiles(controller);

        GameObject generatedRoot = new(GeneratedRootName);
        Undo.RegisterCreatedObjectUndo(generatedRoot, "Create Generated Tiles Root");
        generatedRoot.transform.SetParent(controller.transform, false);

        int count = tilesPerSide.intValue;
        Vector2 spacing = tileSpacing.vector2Value;
        float cornerSpacing = Mathf.Max(1f, cornerSpacingMultiplier.floatValue);
        float cornerDistance = count - 1 + cornerSpacing * 2f;
        float sideEndDistance = count - 1 + cornerSpacing;
        List<BoardTile> createdTiles = new(count * 4 + 4);
        GameObject cornerPrefab = (GameObject)cornerTilePrefab.objectReferenceValue;

        GameObject corners = new("Corners");
        Undo.RegisterCreatedObjectUndo(corners, "Create Corner Tiles");
        corners.transform.SetParent(generatedRoot.transform, false);

        CreateCorner(corners.transform, "CornerTile_Bottom", cornerPrefab,
            new Vector3(0f, -cornerDistance * spacing.y), createdTiles);
        CreateSide(generatedRoot.transform, "LeftBottom", (GameObject)leftBottomTilePrefab.objectReferenceValue,
            count, index => new Vector3(-(cornerSpacing + index) * spacing.x,
                -(sideEndDistance - index) * spacing.y), createdTiles);
        CreateCorner(corners.transform, "CornerTile_Left", cornerPrefab,
            new Vector3(-cornerDistance * spacing.x, 0f), createdTiles);
        CreateSide(generatedRoot.transform, "LeftTop", (GameObject)leftTopTilePrefab.objectReferenceValue,
            count, index => new Vector3(-(sideEndDistance - index) * spacing.x,
                (cornerSpacing + index) * spacing.y), createdTiles);
        CreateCorner(corners.transform, "CornerTile_Top", cornerPrefab,
            new Vector3(0f, cornerDistance * spacing.y), createdTiles);
        CreateSide(generatedRoot.transform, "RightTop", (GameObject)rightTopTilePrefab.objectReferenceValue,
            count, index => new Vector3((cornerSpacing + index) * spacing.x,
                (sideEndDistance - index) * spacing.y), createdTiles);
        CreateCorner(corners.transform, "CornerTile_Right", cornerPrefab,
            new Vector3(cornerDistance * spacing.x, 0f), createdTiles);
        CreateSide(generatedRoot.transform, "RightBottom", (GameObject)rightBottomTilePrefab.objectReferenceValue,
            count, index => new Vector3((sideEndDistance - index) * spacing.x,
                -(cornerSpacing + index) * spacing.y), createdTiles);

        SetSortingOrders(createdTiles);

        serializedObject.Update();
        boardTiles.ClearArray();

        for (int index = 0; index < createdTiles.Count; index++)
        {
            boardTiles.InsertArrayElementAtIndex(index);
            boardTiles.GetArrayElementAtIndex(index).objectReferenceValue = createdTiles[index];
        }

        serializedObject.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Selection.activeGameObject = generatedRoot;
    }

    private void RemoveLegacyRootTiles(BoardController controller)
    {
        foreach (GameObject root in controller.gameObject.scene.GetRootGameObjects())
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(root);
            bool hasGeneratedName =
                root.name.StartsWith("CornerTile_") ||
                root.name.StartsWith("LeftTopTile_") ||
                root.name.StartsWith("RightTopTile_") ||
                root.name.StartsWith("RightBottomTile_") ||
                root.name.StartsWith("LeftBottomTile_");
            bool usesTilePrefab =
                source == cornerTilePrefab.objectReferenceValue ||
                source == leftTopTilePrefab.objectReferenceValue ||
                source == rightTopTilePrefab.objectReferenceValue ||
                source == rightBottomTilePrefab.objectReferenceValue ||
                source == leftBottomTilePrefab.objectReferenceValue;

            if (hasGeneratedName && usesTilePrefab)
            {
                Undo.DestroyObjectImmediate(root);
            }
        }
    }

    private static void CreateSide(
        Transform root,
        string sideName,
        GameObject prefab,
        int count,
        System.Func<int, Vector3> getPosition,
        List<BoardTile> createdTiles)
    {
        GameObject side = new(sideName);
        Undo.RegisterCreatedObjectUndo(side, $"Create {sideName} Tiles");
        side.transform.SetParent(root, false);

        for (int index = 0; index < count; index++)
        {
            GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, side.transform);
            Undo.RegisterCreatedObjectUndo(tile, $"Create {sideName} Tile");
            tile.name = $"{sideName}Tile_{index + 1:00}";
            tile.transform.localPosition = getPosition(index);
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localScale = Vector3.one;
            createdTiles.Add(tile.GetComponent<BoardTile>());
        }
    }

    private static void CreateCorner(
        Transform root,
        string tileName,
        GameObject prefab,
        Vector3 position,
        List<BoardTile> createdTiles)
    {
        GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        Undo.RegisterCreatedObjectUndo(tile, $"Create {tileName}");
        tile.name = tileName;
        tile.transform.localPosition = position;
        tile.transform.localRotation = Quaternion.identity;
        tile.transform.localScale = Vector3.one;
        createdTiles.Add(tile.GetComponent<BoardTile>());
    }

    private static void SetSortingOrders(List<BoardTile> boardTiles)
    {
        List<BoardTile> tilesByDepth = new(boardTiles);
        tilesByDepth.Sort((left, right) =>
        {
            Vector3 leftPosition = left.transform.localPosition;
            Vector3 rightPosition = right.transform.localPosition;
            int yComparison = rightPosition.y.CompareTo(leftPosition.y);

            return yComparison != 0
                ? yComparison
                : leftPosition.x.CompareTo(rightPosition.x);
        });

        for (int index = 0; index < tilesByDepth.Count; index++)
        {
            GameObject tile = tilesByDepth[index].gameObject;
            SortingGroup sortingGroup = tile.GetComponent<SortingGroup>();

            if (sortingGroup == null)
            {
                sortingGroup = Undo.AddComponent<SortingGroup>(tile);
            }

            Undo.RecordObject(sortingGroup, "Set Tile Sorting Order");
            sortingGroup.sortingOrder = index;
        }
    }
}
