using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// DialogueData asset'lerini tek panelden oluşturma/düzenleme penceresi.
// Sol: asset listesi + yeni oluşturma. Sağ: seçili diyaloğun satırları,
// koşulları ve cevap etkileri.
public class DialogueEditorWindow : EditorWindow
{
    Vector2 listScroll;
    Vector2 detailScroll;
    DialogueData selected;
    SerializedObject serialized;
    string newAssetName = "YeniDiyalog";
    string search = "";

    [MenuItem("Tools/UnoG/Diyalog Editörü", priority = 30)]
    static void Open()
    {
        var window = GetWindow<DialogueEditorWindow>("Diyalog Editörü");
        window.minSize = new Vector2(760f, 420f);
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawAssetList();
        DrawDetails();
        EditorGUILayout.EndHorizontal();
    }

    void DrawAssetList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250f));

        GUILayout.Label("Diyaloglar", EditorStyles.boldLabel);
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);

        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        foreach (var data in FindAllDialogues())
        {
            if (!string.IsNullOrEmpty(search) &&
                data.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            bool isSelected = data == selected;
            var style = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft };
            string prefix = data.isNegative ? "[-] " : "[+] ";
            GUI.backgroundColor = isSelected ? new Color(0.6f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button(prefix + data.name, style))
            {
                selected = data;
                serialized = new SerializedObject(selected);
                EditorGUIUtility.PingObject(selected);
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(6f);
        GUILayout.Label("Yeni Diyalog", EditorStyles.boldLabel);
        newAssetName = EditorGUILayout.TextField(newAssetName);
        if (GUILayout.Button("Oluştur"))
            CreateNewAsset();

        EditorGUILayout.EndVertical();
    }

    void CreateNewAsset()
    {
        if (string.IsNullOrWhiteSpace(newAssetName)) return;

        if (!AssetDatabase.IsValidFolder(SampleDialogueGenerator.DialogueFolder))
            AssetDatabase.CreateFolder("Assets", "Dialogues");

        string path = AssetDatabase.GenerateUniqueAssetPath(
            SampleDialogueGenerator.DialogueFolder + "/" + newAssetName.Trim() + ".asset");
        var data = ScriptableObject.CreateInstance<DialogueData>();
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        selected = data;
        serialized = new SerializedObject(selected);
        EditorGUIUtility.PingObject(data);
    }

    void DrawDetails()
    {
        EditorGUILayout.BeginVertical();

        if (selected == null)
        {
            EditorGUILayout.HelpBox(
                "Soldan bir diyalog seç veya yeni oluştur.\n\n" +
                "[+] olumlu diyalog (koşullu, 2 cevaplı)\n" +
                "[-] olumsuz havuz diyaloğu (koşulsuz, cevapsız)", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        if (serialized == null || serialized.targetObject != selected)
            serialized = new SerializedObject(selected);

        serialized.Update();
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

        GUILayout.Label(selected.name, EditorStyles.largeLabel);
        GUILayout.Space(4f);

        EditorGUILayout.PropertyField(serialized.FindProperty("isNegative"),
            new GUIContent("Olumsuz Havuz Diyaloğu"));

        var isNegativeNow = serialized.FindProperty("isNegative").boolValue;
        if (!isNegativeNow)
        {
            EditorGUILayout.PropertyField(serialized.FindProperty("speaker"),
                new GUIContent("Karakter (sol portre)"));
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Olumsuzlarda karakter seçilmez: tıklanan karakterin portresi kullanılır.",
                MessageType.None);
        }

        EditorGUILayout.PropertyField(serialized.FindProperty("lines"),
            new GUIContent("Satırlar (sol=karakter / sağ=ana karakter)"), true);

        var isNegative = serialized.FindProperty("isNegative").boolValue;
        if (!isNegative)
        {
            GUILayout.Space(6f);
            EditorGUILayout.PropertyField(serialized.FindProperty("requirements"),
                new GUIContent("Açılma Koşulları (min/max)"), true);
            GUILayout.Space(6f);
            EditorGUILayout.PropertyField(serialized.FindProperty("answerA"),
                new GUIContent("Cevap A"), true);
            EditorGUILayout.PropertyField(serialized.FindProperty("answerB"),
                new GUIContent("Cevap B"), true);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Olumsuz diyaloglar koşulsuzdur, cevap içermez ve stat'lara etki etmez.",
                MessageType.None);
        }

        EditorGUILayout.EndScrollView();

        if (serialized.ApplyModifiedProperties())
            EditorUtility.SetDirty(selected);

        GUILayout.Space(4f);
        if (GUILayout.Button("Kaydet", GUILayout.Height(28f)))
            AssetDatabase.SaveAssets();

        EditorGUILayout.EndVertical();
    }

    static List<DialogueData> FindAllDialogues()
    {
        var result = new List<DialogueData>();
        foreach (var guid in AssetDatabase.FindAssets("t:DialogueData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<DialogueData>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (data != null) result.Add(data);
        }
        result.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return result;
    }
}
