#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Numerics;
using System.Runtime.InteropServices;
using Foster.Framework;
using Sledge.Formats.Map.Formats;
using Sledge.Formats.Map.Objects;

namespace FosterTest;

public class Level
{
    public MapFile data;
    private const float WorldScale = 1.0f / 32.0f;

    public Foster.Framework.Mesh mesh;
    public List<MeshVertex> vertices;
    public List<int> indices;

    public List<Image> images;
    public List<Texture> textures;
    public Dictionary<string, byte> TextureIDs;
    public const int MaxTextureCount = 8;

    private Dictionary<string, Vector3> entities;
    public Vector3 GetEntity(string name) => entities[name] * WorldScale;

    private void ProcessObject(MapObject obj)
    {
        void CalculateRotatedUV(in Face face, out Vector3 rotatedUAxis, out Vector3 rotatedVAxis)
        {
            // Determine the dominant axis of the normal vector
            static Vector3 GetRotationAxis(Vector3 normal)
            {
                var abs = Vector3.Abs(normal);
                if (abs.X > abs.Y && abs.X > abs.Z)
                    return Vector3.UnitX;
                else if (abs.Y > abs.Z)
                    return Vector3.UnitY;
                else
                    return Vector3.UnitZ;
            }

            // Apply scaling to the axes
            var scaledUAxis = face.UAxis / face.XScale;
            var scaledVAxis = face.VAxis / face.YScale;

            // Determine the rotation axis based on the face normal
            var rotationAxis = GetRotationAxis(face.Plane.Normal);
            var rotationMatrix = Matrix4x4.CreateFromAxisAngle(rotationAxis, face.Rotation * Calc.DegToRad);
            rotatedUAxis = Vector3.Transform(scaledUAxis, rotationMatrix);
            rotatedVAxis = Vector3.Transform(scaledVAxis, rotationMatrix);
        }

        Vector2 CalculateUV(in Face face, in Vector3 vertex, in Vector2 textureSize, in Vector3 rotatedUAxis, in Vector3 rotatedVAxis)
        {
            Vector2 uv;
            uv.X = vertex.X * rotatedUAxis.X + vertex.Y * rotatedUAxis.Y + vertex.Z * rotatedUAxis.Z;
            uv.Y = vertex.X * rotatedVAxis.X + vertex.Y * rotatedVAxis.Y + vertex.Z * rotatedVAxis.Z;
            uv.X += face.XShift;
            uv.Y += face.YShift;
            uv.X /= textureSize.X;
            uv.Y /= textureSize.Y;
            return uv;
        }

        byte GetTexture(string name)
        {
            byte textureID = 0;

            if (TextureIDs.TryGetValue(name, out textureID))
            {
                return textureID;
            }

            if (textures.Count >= MaxTextureCount)
            {
                return 0;
            }

            bool foundImage = false;

            var pngPath = System.IO.Path.Join(["Assets/Textures/", name]) + ".png";
            var jpgPath = System.IO.Path.Join(["Assets/Textures/", name]) + ".jpg";

            if (System.IO.Path.Exists(pngPath))
            {
                images.Add(new Image(pngPath));
                foundImage = true;
            }
            else if (System.IO.Path.Exists(jpgPath))
            {
                images.Add(new Image(jpgPath));
                foundImage = true;
            }

            if (foundImage) {
                textureID = (byte)textures.Count;
                TextureIDs.Add(name, textureID);

                textures.Add(new Texture(images.Last()));
                return textureID;
            }

            return 0;
        }

        if (obj is Sledge.Formats.Map.Objects.Entity entity)
        {
            entities[entity.ClassName] = entity.GetVectorProperty("origin", Vector3.Zero);
        }

        if (obj is Sledge.Formats.Map.Objects.Solid solid)
        {
            solid.ComputeVertices();

            foreach (var face in solid.Faces)
            {
                CalculateRotatedUV(face, out var rotatedUAxis, out var rotatedVAxis);

                byte textureID = GetTexture(face.TextureName);

                for (int i = 0; i < face.Vertices.Count - 2; i++)
                {
                    indices.Add(vertices.Count + 0);
                    indices.Add(vertices.Count + i + 1);
                    indices.Add(vertices.Count + i + 2);
                }

                for (int v = 0; v < face.Vertices.Count; v++)
                {
                    var vert = new Vector3(
                        face.Vertices[v].X * WorldScale,
                        face.Vertices[v].Y * WorldScale,
                        face.Vertices[v].Z * WorldScale
                    );

                    var uv = CalculateUV(face, face.Vertices[v], textures[0].Size, rotatedUAxis, rotatedVAxis);

                    vertices.Add(new MeshVertex(
                        vert,
                        uv,
                        face.Plane.Normal,
                        Color.White,
                        new VecByte4(textureID, 0, 0, 0)
                    ));
                }
            }
        }

        foreach (var child in obj.Children)
        {
            ProcessObject(child);
        }
    }

    public Level(string path)
    {
        vertices = [];
        indices = [];
        mesh = new();

        images = [];
        textures = [];

        entities = [];

        TextureIDs = [];

        var format = new QuakeMapFormat();
        data = format.ReadFromFile(path);

        foreach (var child in data.Worldspawn.Children)
        {
            ProcessObject(child);
        }

        mesh.SetVertices<MeshVertex>(CollectionsMarshal.AsSpan(vertices));
        mesh.SetIndices<int>(CollectionsMarshal.AsSpan(indices));
    }

    ~Level()
    {
        mesh.Dispose();

        foreach (var image in images)
        {
            image.Dispose();
        }

        foreach (var texture in textures)
        {
            texture.Dispose();
        }
    }

    public void Draw(Target? target, Material material, Matrix4x4 matrix, CullMode cullMode = CullMode.Back)
    {
        material.Set("u_modelMatrix", matrix);

        for (int i = 0; i < textures.Count; i++)
        {
            material.Set("u_texture" + i, textures[i]);
        }

        var call = new DrawCommand(target, mesh, material)
        {
            DepthCompare = DepthCompare.Less,
            DepthMask = true,
            CullMode = cullMode,
            MeshIndexStart = 0,
            MeshIndexCount = mesh.IndexCount
        };

        call.Submit();
    }
}