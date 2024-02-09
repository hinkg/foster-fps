using System.Numerics;
using Foster.Framework;

namespace FosterTest;

public class Sky
{
    private Model skySphere;
    private Material skyMaterial;

    private Image cloudImage;
    private Texture cloudTexture;

    private Mesh quad;
    private Material atmosphereMaterial;
    private Target atmosphereRenderTarget;

    private Vector3 prevSunDirection;

    public Sky()
    {
        skySphere = new Model("Assets/Cube.glb");
        skyMaterial = Game.MakeMaterial(Game.ShaderInfo[Renderers.OpenGL]["Sky"]);

        cloudImage = new Image("Assets/Textures/clouds.png");
        cloudTexture = new Texture(cloudImage);

        atmosphereMaterial = Game.MakeMaterial(Game.ShaderInfo[Renderers.OpenGL]["Atmosphere"]);
        atmosphereRenderTarget = new Target(512, 256, [TextureFormat.R8G8B8A8]);

        quad = new Mesh();

        quad.SetVertices([
            new MeshVertex(new Vector3(-1.0f, -1.0f, 0.0f), new Vector2(0.0f, 0.0f), Vector3.Zero, Color.White, VecByte4.Zero),
            new MeshVertex(new Vector3(+1.0f, -1.0f, 0.0f), new Vector2(1.0f, 0.0f), Vector3.Zero, Color.White, VecByte4.Zero),
            new MeshVertex(new Vector3(-1.0f, +1.0f, 0.0f), new Vector2(0.0f, 1.0f), Vector3.Zero, Color.White, VecByte4.Zero),
            new MeshVertex(new Vector3(+1.0f, +1.0f, 0.0f), new Vector2(1.0f, 1.0f), Vector3.Zero, Color.White, VecByte4.Zero),
        ]);

        quad.SetIndices([0, 1, 2, 3, 2, 1]);
    }

    public Texture GetTexture()
    {
        return atmosphereRenderTarget.Attachments[0];
    }

    public void ReloadShader()
    {
        try
        {
            var newMaterial = Game.MakeMaterial(Game.ShaderInfo[Renderers.OpenGL]["Atmosphere"]);
            atmosphereMaterial.Clear();
            atmosphereMaterial = newMaterial;

            newMaterial = Game.MakeMaterial(Game.ShaderInfo[Renderers.OpenGL]["Sky"]);
            skyMaterial.Clear();
            skyMaterial = newMaterial;
        }
        catch
        {
            return;
        }
    }

    public void Draw(Vector3 sunDirection, Matrix4x4 viewMatrix)
    {
        // Draw sky texture

        DrawCommand call;

        if (sunDirection != prevSunDirection)
        {
            atmosphereMaterial.Set("u_sunDirection", sunDirection);

            call = new DrawCommand(atmosphereRenderTarget, quad, atmosphereMaterial)
            {
                DepthCompare = DepthCompare.Always,
                DepthMask = false,
                CullMode = CullMode.None,
                MeshIndexStart = 0,
                MeshIndexCount = quad.IndexCount
            };

            call.Submit();

            prevSunDirection = sunDirection;
        }

        // Draw sky to main framebuffer

        skyMaterial.Set("u_viewMatrix", viewMatrix);
        skyMaterial.Set("u_modelMatrix", Matrix4x4.Identity);
        skyMaterial.Set("u_scroll", (float)Time.Now.TotalSeconds * 0.001f);

        skyMaterial.Set("u_cloudTexture", cloudTexture);
        skyMaterial.Set("u_sunDirection", sunDirection);

        skyMaterial.Set("u_skyTexture", atmosphereRenderTarget.Attachments[0]);

        call = new DrawCommand(null, skySphere.mesh, skyMaterial)
        {
            DepthCompare = DepthCompare.Always,
            DepthMask = false,
            CullMode = CullMode.Front,
            MeshIndexStart = 0,
            MeshIndexCount = skySphere.mesh.IndexCount,
        };

        call.Submit();
    }
}