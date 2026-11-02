using IMMonster.SceneArt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ArchiveWorkstationArt))]
public sealed class ArchiveWorkstationArtInspector : Editor
{
    bool showReferences;
    public override void OnInspectorGUI()
    {
        var art=(ArchiveWorkstationArt)target;
        EditorGUILayout.LabelField("档案工作区 · 摆放预演",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("可预览灾前、受袭后和封档状态。切换摆放不会推进游戏剧情。",MessageType.Info);
        if(GUILayout.Button("查看灾前摆放"))Change(art,()=>art.SetDisturbed(false));
        if(GUILayout.Button("查看受袭后摆放"))Change(art,()=>art.SetDisturbed(true));
        if(GUILayout.Button(art.IsArchiveSealed?"蓝绳改为未封档":"蓝绳改为已封档"))Change(art,()=>art.SetArchiveSealed(!art.IsArchiveSealed));
        EditorGUILayout.LabelField("当前",(art.IsDisturbed?"受袭后":"灾前")+" / "+(art.IsArchiveSealed?"已封档":"未封档"));
        showReferences=EditorGUILayout.Foldout(showReferences,"接入引用");
        if(showReferences)
        {
            serializedObject.Update();
            foreach(string name in new[]{"intactProps","disturbedProps","looseSeals","tiedSeals"})EditorGUILayout.PropertyField(serializedObject.FindProperty(name),true);
            serializedObject.ApplyModifiedProperties();
        }
    }
    static void Change(ArchiveWorkstationArt art,System.Action change)
    {
        Undo.RegisterFullObjectHierarchyUndo(art.gameObject,"Preview archive arrangement");change();
        SealChamberStudy.SetArchiveReviewLighting(art.IsDisturbed);
        EditorUtility.SetDirty(art);
        if(!Application.isPlaying)EditorSceneManager.MarkSceneDirty(art.gameObject.scene);
        SceneView.RepaintAll();
    }
}
