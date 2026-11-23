using UnityEngine;

namespace SaintKnight.Day09
{
    [DefaultExecutionOrder(500)]
    [DisallowMultipleComponent]
    public sealed class SKCorridorPreview : MonoBehaviour
    {
        public const int PixelsPerUnit = 48;
        public const int Width = 640, Height = 360;
        public const float ArchCenterPixel = 916f;

        private const int GeometryLayer = 30;
        private const int GeometryMask = 1 << GeometryLayer;

        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Shader stoneShader, compositeShader;
        [SerializeField] private bool showGeometry = true;

        private Camera geometryCamera;
        private MeshRenderer architecture, composite;
        private RenderTexture target;
        private Mesh archMesh, quadMesh;
        private Material stone, compositing;
        private int sourceMask;

        public bool ShowGeometry
        {
            get => showGeometry;
            set
            {
                showGeometry = value;
                if (isActiveAndEnabled) SetVisible();
            }
        }

        public static float StageX(float pixel) => (pixel - Width * .5f) / PixelsPerUnit;
        public static float StageY(float pixel) => (Height * .5f - pixel) / PixelsPerUnit;

        public void Configure(Camera camera, Shader stonePass, Shader compositePass)
        {
            gameplayCamera = camera;
            stoneShader = stonePass;
            compositeShader = compositePass;
        }

        private void OnEnable()
        {
            sourceMask = gameplayCamera.cullingMask;
            gameplayCamera.cullingMask &= ~GeometryMask;

            target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Corridor geometry 640x360",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            target.Create();
            var cameraObject = new GameObject("Corridor Geometry Camera");
            cameraObject.transform.SetParent(transform, false);
            geometryCamera = cameraObject.AddComponent<Camera>();
            geometryCamera.clearFlags = CameraClearFlags.SolidColor;
            geometryCamera.backgroundColor = Color.clear;
            geometryCamera.cullingMask = GeometryMask;
            geometryCamera.targetTexture = target;
            geometryCamera.orthographic = true;
            geometryCamera.orthographicSize = Height / (2f * PixelsPerUnit);
            geometryCamera.aspect = (float)Width / Height;
            geometryCamera.allowHDR = false;
            geometryCamera.allowMSAA = false;
            geometryCamera.useOcclusionCulling = false;
            geometryCamera.depth = gameplayCamera.depth - 1;

            stone = new Material(stoneShader);
            archMesh = SKCorridorStoneMesh.Build();
            architecture = CreateRenderer("Stone arch and piers", archMesh, stone, GeometryLayer);

            compositing = new Material(compositeShader);
            compositing.SetTexture("_CorridorTex", target);
            quadMesh = CreateQuad();
            composite = CreateRenderer("Corridor Pixel Composite", quadMesh, compositing, gameObject.layer);
            composite.sortingOrder = -9; // Between the panorama (-10) and all gameplay sprites.
            LateUpdate();
        }

        private void LateUpdate()
        {
            var position = gameplayCamera.transform.position;
            position.x = Snap(position.x);
            position.y = Snap(position.y);
            gameplayCamera.transform.position = position;
            geometryCamera.transform.position = position;
            composite.transform.position = new Vector3(position.x, position.y, 1f);
            SetVisible();
        }

        private void SetVisible()
        {
            geometryCamera.enabled = showGeometry;
            composite.enabled = showGeometry;
        }

        private static float Snap(float value) => Mathf.Round(value * PixelsPerUnit) / PixelsPerUnit;

        private MeshRenderer CreateRenderer(string name, Mesh mesh, Material material, int layer)
        {
            var drawing = new GameObject(name) { layer = layer };
            drawing.transform.SetParent(transform, false);
            drawing.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = drawing.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Mesh CreateQuad()
        {
            const float halfWidth = Width / (2f * PixelsPerUnit), halfHeight = Height / (2f * PixelsPerUnit);
            return new Mesh
            {
                name = "Corridor pixel composite",
                vertices = new[]
                {
                    new Vector3(-halfWidth, -halfHeight, 0), new Vector3(halfWidth, -halfHeight, 0),
                    new Vector3(halfWidth, halfHeight, 0), new Vector3(-halfWidth, halfHeight, 0)
                },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
        }

        private void OnDisable()
        {
            if (gameplayCamera != null) gameplayCamera.cullingMask = sourceMask;
            geometryCamera.enabled = false;
            geometryCamera.targetTexture = null;
            composite.enabled = false;
            architecture.enabled = false;
            Destroy(geometryCamera.gameObject);
            Destroy(architecture.gameObject);
            Destroy(composite.gameObject);

            target.Release();
            Destroy(target);
            Destroy(archMesh);
            Destroy(quadMesh);
            Destroy(stone);
            Destroy(compositing);
        }
    }
}
