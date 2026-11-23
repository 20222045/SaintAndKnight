#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SaintKnight.Day01;

namespace SaintKnight.Day01.Editor
{
    public static class SKDay01SceneBuilder
    {
        private const string Base = "Assets/SaintKnightDay01";
        private static readonly Color Ink = new Color(.055f, .045f, .075f, 1);
        private static readonly Color Panel = new Color(.105f, .070f, .105f, 1);
        private static readonly Color Gold = new Color(.76f, .59f, .35f, 1);
        private static Font font;

        [MenuItem("Tools/SaintKnight/Day 01/Create Playable Test Scene")]
        public static void CreateDemo()
        {
            if (!Directory.Exists(Base + "/Scenes")) Directory.CreateDirectory(Base + "/Scenes");
            var bg = LoadSprite("Art/gameplay_background.png", 48f);
            var saint = LoadSprite("Art/cecilia_PLACEHOLDER.png", 48f);
            var knight = LoadSprite("Art/adela_PLACEHOLDER.png", 48f);
            var saintPortrait = LoadSprite("Art/cecilia_approved_cropped.png", 100f);
            var knightPortrait = LoadSprite("Art/adela_approved_cropped.png", 100f);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bgGo = new GameObject("Pixel_Background_Day01");
            var srBg = bgGo.AddComponent<SpriteRenderer>();
            srBg.sprite = bg;
            srBg.sortingOrder = -10;
            bgGo.transform.position = new Vector3(0, 0, 0);

            var camGo = new GameObject("Camera_2D_Gameplay");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.75f;
            cam.backgroundColor = Ink;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.rect = new Rect(.25f, .125f, .75f, .75f);
            camGo.transform.position = new Vector3(0, 0, -10);
            cam.nearClipPlane = .01f;
            cam.farClipPlane = 100;

            // Basic static floor; intentionally no monster or combat on Day 01.
            var ground = new GameObject("Ground_Collider_2D");
            ground.transform.position = new Vector3(0f, -3.25f, 0f);
            var floor = ground.AddComponent<BoxCollider2D>();
            floor.size = new Vector2(13.1f, 1.0f);
            ground.AddComponent<SpriteRenderer>().color = new Color(.23f,.15f,.25f,1);

            var player = new GameObject("Player_2D_TemporarySprite");
            player.transform.position = new Vector3(-3f, -2.15f, 0);
            var playerRenderer = player.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = saint;
            playerRenderer.sortingOrder = 10;
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.2f;
            body.freezeRotation = true;
            var box = player.AddComponent<BoxCollider2D>();
            box.size = new Vector2(.47f,.94f);
            player.AddComponent<SKDay01Player>();

            Canvas canvas = MakeCanvas();
            var hudPanel = RectPanel("HUD_Left_Panel", canvas.transform, Vector2.zero, new Vector2(.25f, 1), Panel);
            RectPanel("HUD_Top_Line", hudPanel.transform, new Vector2(0, .991f), Vector2.one, Gold);
            MakeText("PROJECT", hudPanel.transform, "SAINT AND KNIGHT", 24,
                new Vector2(0,.91f),new Vector2(1,.98f),Gold);
            MakeText("HUD_TITLE", hudPanel.transform, "3D HUD ZONE (DAY 01)", 14,
                new Vector2(0,.865f),new Vector2(1,.91f),new Color(.76f,.72f,.70f));

            var portraitGo = new GameObject("Approved_Portrait_Placeholder", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(hudPanel.transform, false);
            Rect(portraitGo.GetComponent<RectTransform>(),new Vector2(.08f,.28f),new Vector2(.92f,.82f));
            var portrait = portraitGo.GetComponent<Image>();
            portrait.sprite = saintPortrait;
            portrait.preserveAspect = true;
            portrait.color = Color.white;
            var charName = MakeText("CHAR_NAME", hudPanel.transform,"CECILIA NOEL",21,
                new Vector2(0,.23f),new Vector2(1,.29f), Gold);
            var hp = MakeText("HP_TEXT", hudPanel.transform,"HP 100 / 100",18,
                new Vector2(0,.165f),new Vector2(1,.22f),Color.white);
            var barBg = RectPanel("HP_BACK",hudPanel.transform,new Vector2(.11f,.14f),new Vector2(.89f,.155f),new Color(.16f,.12f,.14f));
            var fillGo = RectPanel("HP_FILL",barBg.transform,Vector2.zero,Vector2.one,new Color(.76f,.19f,.24f));
            var hpFill = fillGo.GetComponent<Image>(); hpFill.type=Image.Type.Filled; hpFill.fillMethod=Image.FillMethod.Horizontal;
            var damage = MakeText("DAMAGE",hudPanel.transform,"DAMAGE STAGE 0 / 4",15,
                new Vector2(0,.075f),new Vector2(1,.13f),Gold);
            MakeText("NOTE",hudPanel.transform,"Real-time 3D model connects later",11,
                new Vector2(0,.015f),new Vector2(1,.06f),new Color(.8f,.73f,.73f));

            var statusBackdrop = RectPanel("TOP_STATUS_BACKDROP", canvas.transform,
                new Vector2(.25f, .875f), Vector2.one, Color.black);
            statusBackdrop.GetComponent<Image>().raycastTarget = false;
            var bottom = RectPanel("BOTTOM_CONTROLS",canvas.transform,new Vector2(.25f,0),new Vector2(1,.125f),Ink);
            MakeText("CONTROLS",bottom.transform,
                "MOVE A/D   |   JUMP SPACE   |   SWITCH TAB   |   HP - Q   |   HP + E",19,
                new Vector2(0,0),Vector2.one,Gold);

            var hud = hudPanel.AddComponent<SKDay01HUD>();
            SetObjectRefs(hud, "portrait", portrait, "hpFill", hpFill,
                "characterName",charName,"healthText",hp,"stageText",damage,
                "ceciliaPortrait",saintPortrait,"adelaPortrait",knightPortrait);
            var controller = player.GetComponent<SKDay01Player>();
            SetObjectRefs(controller,"ceciliaTemporarySprite",saint,"adelaTemporarySprite",knight,"hud",hud);

            string path = Base + "/Scenes/Day01_Prototype.unity";
            EditorSceneManager.SaveScene(scene,path);
            EditorBuildSettings.scenes = new [] { new EditorBuildSettingsScene(path,true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            EditorUtility.DisplayDialog("SaintKnight - Day 01",
                "Day01_Prototype scene created. Press Play.\n"+
                "This is 2D movement + UI scaffold only.\n"+
                "The approved 3D GLBs are preserved separately.","OK");
        }

        private static Sprite LoadSprite(string relativePath, float pixelsPerUnit)
        {
            var assetPath = Base+"/"+relativePath;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing texture: "+assetPath);
            if (importer.textureType != TextureImporterType.Sprite ||
                Mathf.Abs(importer.spritePixelsPerUnit-pixelsPerUnit)>.01f ||
                importer.filterMode != FilterMode.Point)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) throw new System.Exception("Sprite import failed: "+assetPath);
            return sprite;
        }

        private static Canvas MakeCanvas()
        {
            var go = new GameObject("Canvas_2D_UI_and_3D_HUD_Slot",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = go.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1280,720);
            scale.matchWidthOrHeight = .5f;
            return canvas;
        }
        private static GameObject RectPanel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image));
            go.transform.SetParent(parent,false);
            Rect(go.GetComponent<RectTransform>(),min,max);
            go.GetComponent<Image>().color=color;
            return go;
        }
        private static void Rect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        private static Text MakeText(string name, Transform parent, string value, int size,
            Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Text));
            go.transform.SetParent(parent,false);
            Rect(go.GetComponent<RectTransform>(),min,max);
            var label=go.GetComponent<Text>();
            label.font = Font();
            label.text=value;
            label.fontSize=size;
            label.color=color;
            label.alignment=TextAnchor.MiddleCenter;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;
            label.verticalOverflow=VerticalWrapMode.Truncate;
            return label;
        }
        private static Font Font()
        {
            if (font != null) return font;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
        private static void SetObjectRefs(Object component, params object[] nameValuePairs)
        {
            var so=new SerializedObject(component);
            for (int i=0;i<nameValuePairs.Length;i+=2)
            {
                var p=so.FindProperty((string)nameValuePairs[i]);
                if(p==null) throw new System.Exception("Missing property: "+nameValuePairs[i]);
                p.objectReferenceValue = (Object)nameValuePairs[i+1];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
