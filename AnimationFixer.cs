using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Snorlax
{
    public class AnimationFixer : EditorWindow
    {
        public class FBXObject
        {
            public ModelImporter importer;
            public List<AnimationClip> clips;
            public bool NeedFix = false;
            public FBXObject(ModelImporter importer)
            {
                this.importer = importer;
                this.clips = GetClips(importer);
            }

            public List<AnimationClip> GetClips(Object FBX)
            {
                List<AnimationClip> tempList = new List<AnimationClip>();

                var Items = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(FBX));

                foreach (var item in Items)
                {
                    if (item is AnimationClip clip) tempList.Add(clip);
                }

                return tempList;
            }
        }


        [MenuItem("Snorlax's Tools/Animation Fixer")]
        public static void ShowWindow() => GetWindow<AnimationFixer>("Animation Fixer");
        public bool FilterWorking;
        public List<Object> ObjectList = new List<Object>();
        public List<FBXObject> FBXs = new List<FBXObject>();

        private const float height = 20f;
        private Vector2 scrollView = Vector2.zero;


        private void OnGUI()
        {
            DropArea();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Toggle Filter")) FilterWorking = !FilterWorking;

            if (GUILayout.Button("Fix")) FixAnimations();

            if (GUILayout.Button("Clear")) { ObjectList.Clear(); FBXs.Clear(); }

            EditorGUILayout.EndHorizontal();

            scrollView = EditorGUILayout.BeginScrollView(scrollView);    
            ListView();
            EditorGUILayout.EndScrollView();
        }

        private void DropArea()
        {
            Rect drop_area = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(drop_area, "");

            GUI.Box(drop_area, "Drag & Drop GameObjects and or Objects here", EditorStyles.centeredGreyMiniLabel);

            var e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(e.mousePosition)) return;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object droppedObj in DragAndDrop.objectReferences)
                        {
                            var import = (ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(droppedObj));

                           // if (ObjectList.Contains(droppedObj)) continue;

                            ObjectList.Add(droppedObj);

                            FBXs.Add(new FBXObject(import));
                        }
                    }
                    break;
            }
        }

        private void ListView()
        {
            Color defaultColor = GUI.backgroundColor;

            for (int i = 0; i < FBXs.Count; i++)
            {
                var animations = FBXs[i].importer.clipAnimations != null ? FBXs[i].importer.clipAnimations : FBXs[i].importer.defaultClipAnimations;

                for (int x = 0; x < animations.Length; x++)
                {
                    string hashName = $"{FBXs[i].importer.name} - {animations[x].name}";

                    //  if (!StringContains(hashName, SearchString) && !String.IsNullOrEmpty(SearchString))
                    //     continue;


                    var animationClip = FBXs[i].clips.Find(e => e.name == animations[x].name);
                    bool MisMatched = Mathf.FloorToInt(animations[x].lastFrame) != Mathf.FloorToInt(animationClip.length * animationClip.frameRate);//!foundAnimations.Contains(currentHashkeys.Keys[i].Clip);

                    if (!MisMatched && FilterWorking) continue; 

                    if (MisMatched)
                    {
                        FBXs[i].NeedFix = MisMatched;

                        GUI.backgroundColor = Color.red;
                    }
                    else
                    {
                        GUI.backgroundColor = Color.green;
                    }


                    Rect rect = EditorGUILayout.BeginHorizontal("Box", GUILayout.Height(height));
                    {
                        GUI.backgroundColor = defaultColor;

                        EditorGUILayout.LabelField(hashName);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void FixAnimations()
        {
            for(int i = 0; i < FBXs.Count; i++)
            {
                if (!FBXs[i].NeedFix) continue;

                SerializedObject so = new SerializedObject(FBXs[i].importer);
                SerializedProperty SerializedClips = so.FindProperty("m_ClipAnimations");

                var animations = FBXs[i].importer.clipAnimations != null ? FBXs[i].importer.clipAnimations : FBXs[i].importer.defaultClipAnimations;


                for (int x = 0; x < animations.Length; x++)
                {
                    var animationClip = FBXs[i].clips.Find(e => e.name == animations[x].name);

                    SerializedClips.GetArrayElementAtIndex(x).FindPropertyRelative("lastFrame").floatValue = animationClip.frameRate * animationClip.length;
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(FBXs[i].importer);
                FBXs[i].importer.SaveAndReimport();
            }
        }
    }
}
