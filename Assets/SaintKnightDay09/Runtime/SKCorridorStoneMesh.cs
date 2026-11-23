using System.Collections.Generic;
using UnityEngine;

namespace SaintKnight.Day09
{
    internal static class SKCorridorStoneMesh
    {
        private const float Center = SKCorridorPreview.ArchCenterPixel;
        private const float Spring = 114f, Floor = 310f;
        private const int ArchStones = 15;
        private const float InnerRadius = 50f, OuterRadius = 66f;

        private static readonly Color32 Mortar = new(29, 29, 36, 255);
        private static readonly Color32[] StoneColors =
        {
            new(58, 58, 69, 255), new(63, 63, 73, 255), new(53, 54, 64, 255),
            new(58, 57, 64, 255), new(49, 51, 60, 255)
        };

        public static Mesh Build()
        {
            var mesh = new StoneMesh();
            AddPier(mesh, Center - 58f, 0);
            AddPier(mesh, Center + 58f, 3);
            for (int index = 0; index < ArchStones; index++)
            {
                float first = Mathf.PI * index / ArchStones;
                float last = Mathf.PI * (index + 1) / ArchStones;
                mesh.Add(Wedge(first, last, InnerRadius, OuterRadius), 4, 8, 0, Mortar);

                bool crown = index == ArchStones / 2;
                var profile = Wedge(first + .012f, last - .012f, InnerRadius + 1,
                    crown ? OuterRadius + 2f : OuterRadius - 1f);
                mesh.Add(profile, crown ? -4 : 0, 9, crown ? 1.6f : 1.1f,
                    StoneColors[(index * 3 + 1) % StoneColors.Length]);
            }

            return mesh.ToMesh();
        }

        private static void AddPier(StoneMesh mesh, float center, int offset)
        {
            mesh.Add(Rectangle(center, 17, Spring, Floor), 4, 8, 0, Mortar);
            int row = 0;
            for (float top = Spring + 8; top < Floor - 14; top += 16)
            {
                float bottom = Mathf.Min(top + 15, Floor - 14);
                float drift = row % 3 - 1;
                var tint = StoneColors[(row * 2 + offset) % StoneColors.Length];
                mesh.Add(Rectangle(center + drift, 18, top, bottom), 0, 8, 1.1f, tint);
                row++;
            }

            mesh.Add(Rectangle(center, 23, Spring - 3, Spring + 1), -3, 11, .7f, StoneColors[1]);
            mesh.Add(Rectangle(center, 20, Spring + 1, Spring + 7), -2, 10, .7f, StoneColors[0]);
            mesh.Add(Rectangle(center, 21, Floor - 14, Floor - 9), -2, 10, .8f, StoneColors[0]);
            mesh.Add(Rectangle(center, 24, Floor - 9, Floor), -3, 11, .8f, StoneColors[2]);
        }

        private static Vector2[] Rectangle(float x, float width, float top, float bottom)
        {
            float half = width * .5f;
            return new[]
            {
                new Vector2(x - half, -bottom), new Vector2(x + half, -bottom),
                new Vector2(x + half, -top), new Vector2(x - half, -top)
            };
        }

        private static Vector2[] Wedge(float first, float last, float inner, float outer)
        {
            var origin = new Vector2(Center, -Spring);
            var a = new Vector2(Mathf.Cos(first), Mathf.Sin(first));
            var b = new Vector2(Mathf.Cos(last), Mathf.Sin(last));
            return new[] { origin + a * inner, origin + a * outer, origin + b * outer, origin + b * inner };
        }

        private sealed class StoneMesh
        {
            private readonly List<Vector3> vertices = new();
            private readonly List<Color> colors = new();
            private readonly List<int> triangles = new();

            public void Add(Vector2[] outline, float front, float depth, float bevel, Color color)
            {
                var inset = new Vector2[4];
                for (int i = 0; i < 4; i++)
                {
                    Vector2 before = (outline[i] - outline[(i + 3) % 4]).normalized;
                    Vector2 after = (outline[(i + 1) % 4] - outline[i]).normalized;
                    var beforeNormal = new Vector2(-before.y, before.x);
                    var afterNormal = new Vector2(-after.y, after.x);
                    inset[i] = outline[i] + (beforeNormal + afterNormal)
                        * (bevel / (1f + Vector2.Dot(beforeNormal, afterNormal)));
                }

                if (QualitySettings.activeColorSpace == ColorSpace.Linear) color = color.linear;
                Face(Point(inset[0], front), Point(inset[3], front),
                    Point(inset[2], front), Point(inset[1], front), color);
                Face(Point(outline[0], front + depth), Point(outline[1], front + depth),
                    Point(outline[2], front + depth), Point(outline[3], front + depth), color);
                for (int i = 0; i < 4; i++)
                {
                    int next = (i + 1) % 4;
                    Vector3 shoulder = Point(outline[i], front + bevel);
                    Vector3 nextShoulder = Point(outline[next], front + bevel);
                    if (bevel > 0)
                        Face(Point(inset[i], front), Point(inset[next], front), nextShoulder, shoulder, color);
                    Face(shoulder, nextShoulder, Point(outline[next], front + depth), Point(outline[i], front + depth), color);
                }
            }

            private static Vector3 Point(Vector2 outline, float depth)
            {
                return new Vector3(SKCorridorPreview.StageX(outline.x), SKCorridorPreview.StageY(-outline.y),
                    1f + depth / SKCorridorPreview.PixelsPerUnit);
            }

            private void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
            {
                int first = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                vertices.Add(d);
                for (int i = 0; i < 4; i++) colors.Add(color);
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 3);
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh { name = "Beveled corridor masonry" };
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                return mesh;
            }
        }
    }
}
