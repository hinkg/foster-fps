﻿#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Numerics;
using Foster.Framework;

namespace FosterTest;

class Program
{
	private static void Main(string[] args)
	{
		App.Run<Game>("Foster App", 720, 540, false, Renderers.OpenGL);
	}
}

class Game : Module
{
	public const float FixedUpdateTimestep = 0.01f; // 100hz
	public static float fixedUpdateAccumulator;
	private TimeSpan prevTime;

	private const float MouseSensitivity = 4.0f;
	private Vector2 prevMousePosition;

	// Game

	private List<Entity> entities;
	private Player player;

	private Level level;
	private Sky sky;

	// Rendering

	private const float CameraFieldOfView = 75.0f;
	private const float CameraNearPlane = 0.1f;
	private const float CameraFarPlane = 1000.0f;

	private const float ShadowMapScale = 1.0f / 32.0f;
	private const int ShadowMapResolution = 2048;
	public Target shadowTarget;

	private SpriteFont font;
	private Model cube;
	private Model sphere;

	private Material defaultMaterial;

	public bool debugShowShadowMap;
	public bool debugShowSkyTexture;

	public Vector2 sunRotation;

	public Vector3 sunDirection => Vector3.Normalize(new(
		-MathF.Sin(sunRotation.Y) * MathF.Abs(MathF.Sin(sunRotation.X)),
		MathF.Cos(sunRotation.Y) * MathF.Abs(MathF.Sin(sunRotation.X)),
		MathF.Cos(sunRotation.X)));


	//

	public override void Startup()
	{
		Time.FixedStepTarget = TimeSpan.FromSeconds(1.0f / 1000);
		App.VSync = true;

		font = new SpriteFont(Path.Join("Assets/Fonts/CozetteVector.ttf"), 12.0f);
		GUI.Init(font);

		defaultMaterial = MakeMaterial(ShaderInfo[Renderers.OpenGL]["Default"]);

		cube = new("Assets/Cube.glb");
		sphere = new("Assets/Sphere.glb");

		level = new Level("Assets/level1.map");

		entities = [];
		player = new Player(level.GetEntity("info_player_start"), new Vector2(MathF.PI / 2, 0.0f), Vector3.Zero);
		entities.Add(player);

		shadowTarget = new Target(ShadowMapResolution, ShadowMapResolution, [TextureFormat.Depth24Stencil8]);

		sunRotation = new Vector2(1.2f, 0.0f);

		sky = new Sky();
	}

	public override void Shutdown()
	{

	}

	private void UpdatePlayerInput()
	{
		Vector3 movement = Vector3.Zero;

		if (!GUI.AnyInteraction) {
			if (Input.Mouse.LeftDown)
			{
				player.rotation.Y += (prevMousePosition.X - Input.Mouse.X) / 1000.0f * MouseSensitivity;
				player.rotation.X += (prevMousePosition.Y - Input.Mouse.Y) / 1000.0f * MouseSensitivity;

				player.rotation.X = Math.Clamp(player.rotation.X, 0.1f, 3.0f);
			}

			if (Input.Keyboard.Down(Keys.W)) movement.Y += 1.0f;
			if (Input.Keyboard.Down(Keys.S)) movement.Y -= 1.0f;
			if (Input.Keyboard.Down(Keys.A)) movement.X -= 1.0f;
			if (Input.Keyboard.Down(Keys.D)) movement.X += 1.0f;

			if (Input.Keyboard.Pressed(Keys.Space)) player.DoJump();
			if (Input.Keyboard.Down(Keys.LeftControl)) movement.Z -= 1.0f;
		}

		if (movement != Vector3.Zero)
		{
			movement = Vector3.Normalize(movement);

			if (movement.X != 0.0f || movement.Y != 0.0f)
			{
				var strafe = MathF.Atan2(movement.Y, movement.X);
				movement.X = MathF.Cos(player.rotation.Y + strafe);
				movement.Y = MathF.Sin(player.rotation.Y + strafe);
			}
		}

		player.SetMovementDir(movement);
	}

	private void ReloadResources() {
		try
		{
			var newMaterial = MakeMaterial(ShaderInfo[Renderers.OpenGL]["Default"]);
			defaultMaterial.Clear();
			defaultMaterial = newMaterial;
		}
		catch { }

		try
		{
			var newLevel = new Level("Assets/level1.map");
			level = newLevel;
		}
		catch { }

		sky.ReloadShader();
	}

	public override void Update()
	{
		if (Input.Keyboard.Pressed(Keys.F2)) debugShowShadowMap = !debugShowShadowMap;
		if (Input.Keyboard.Pressed(Keys.F3)) debugShowSkyTexture = !debugShowSkyTexture;
		if (Input.Keyboard.Pressed(Keys.R)) ReloadResources();

		// GUI

		GUI.NewFrame();

		var r0 = GUI.Viewport.Inflate(-8.0f);

		GUI.TextLine(ref r0, $"fps: {1.0f / (Time.Now.TotalSeconds - prevTime.TotalSeconds):0.00}");
		GUI.TextLine(ref r0, $"mem: {GC.GetGCMemoryInfo().HeapSizeBytes / 1.0e6:0.00}MB");
		GUI.TextLine(ref r0, $"pos: ({player.position.X:0.00}, {player.position.Y:0.00}, {player.position.Z:0.00})");
		GUI.TextLine(ref r0, $"vel: ({player.velocity.X:0.00}, {player.velocity.Y:0.00}, {player.velocity.Z:0.00})");
		GUI.TextLine(ref r0, $"rot: ({player.rotation.X:0.00}, {player.rotation.Y:0.00})");
		GUI.TextLine(ref r0, $"sun: ({sunDirection.X:0.00} {sunDirection.Y:0.00}, {sunDirection.Z:0.00}) ({sunRotation.X:0.00}, {sunRotation.Y:0.00})");
		GUI.TextLine(ref r0, $"ent: {entities.Count}");

		if (GUI.Button(r0.CutTop(18.0f).GetLeft(128.0f), "Reload resources")) ReloadResources();

		if (debugShowShadowMap) GUI.Batch.Image(shadowTarget.Attachments[0], Vector2.Zero, Vector2.Zero, new Vector2(1.0f / 2.0f), 0.0f, Color.White);
		if (debugShowSkyTexture) GUI.Batch.Image(sky.GetTexture(), new Vector2(0, sky.GetTexture().Size.Y), Vector2.Zero, new Vector2(1.0f, -1.0f), 0.0f, Color.White);

		// Player input
		UpdatePlayerInput();

		// Sun rotation
		if (Input.Mouse.RightDown)
		{
			sunRotation.Y += (prevMousePosition.X - Input.Mouse.X) / 250.0f;
			sunRotation.X -= (prevMousePosition.Y - Input.Mouse.Y) / 250.0f;

			sunRotation.X = Math.Clamp(sunRotation.X, 0.1f, 3.0f);
		}

		// Spawn spheres
		if (Input.Keyboard.Pressed(Keys.F1))
		{
			entities.Add(new TestSphere(
				player.position + Vector3.UnitZ * player.EyeOffset + player.Facing,
				player.Facing * 0.5f
			));
		}

		// Fixed update

		fixedUpdateAccumulator += Time.Delta;

		if (fixedUpdateAccumulator > FixedUpdateTimestep)
		{
			foreach (var entity in entities)
			{
				entity.PreFixedUpdate();
			}

			while (fixedUpdateAccumulator > FixedUpdateTimestep)
			{
				foreach (var entity in entities)
				{
					entity.FixedUpdate(level);
				}

				fixedUpdateAccumulator -= FixedUpdateTimestep;
			}

		}

		//

		prevMousePosition = new Vector2(Input.Mouse.X, Input.Mouse.Y);
	}

	public Matrix4x4 GetCameraMatrix()
	{
		var eye_position = player.GetInterpolatedPosition() + new Vector3(0.0f, 0.0f, player.EyeOffset);
		return Matrix4x4.CreateTranslation(-eye_position) * GetCameraRotationMatrix();
	}

	public Matrix4x4 GetCameraRotationMatrix()
	{
		var mtx = Matrix4x4.Identity;
		mtx *= Matrix4x4.CreateRotationZ(-player.rotation.Y);
		mtx *= Matrix4x4.CreateRotationX(-player.rotation.X);
		return mtx;
	}

	public Matrix4x4 GetPerspectiveMatrix()
	{
		return Matrix4x4.CreatePerspectiveFieldOfView(
			CameraFieldOfView * (MathF.PI / 180.0f),
			(float)App.Width / App.Height,
			CameraNearPlane,
			CameraFarPlane
		);
	}

	public Matrix4x4 GetShadowMatrix()
	{
		var mtx = Matrix4x4.CreateTranslation(Vector3.Zero);
		mtx *= Matrix4x4.CreateScale(ShadowMapScale);
		mtx *= Matrix4x4.CreateLookAt(Vector3.Zero, sunDirection, Vector3.UnitZ);
		return mtx;
	}

	public override void Render()
	{
		prevTime = Time.Now;

		defaultMaterial.Set("u_textureShadow", shadowTarget.Attachments[0]);
		defaultMaterial.Set("u_shadowMatrix", GetShadowMatrix());
		defaultMaterial.Set("u_sunDirection", sunDirection);

		//
		// Shadow map pass
		//

		{
			shadowTarget.Clear(Color.Transparent, 1.0f, 0, ClearMask.Depth);

			defaultMaterial.Set("u_viewMatrix", GetShadowMatrix());

			foreach (var entity in entities)
			{
				if (entity == player) continue;

				var offset = entity.GetInterpolatedPosition();
				sphere.Draw(shadowTarget, defaultMaterial, Matrix4x4.CreateTranslation(offset), CullMode.Back);
			}

			level.Draw(shadowTarget, defaultMaterial, Matrix4x4.Identity, CullMode.Back);
		}

		//
		// Main pass
		//

		{
			Graphics.Clear(Color.Transparent, 1.0f, 0, ClearMask.Depth);

			sky.Draw(sunDirection, GetCameraRotationMatrix() * GetPerspectiveMatrix());

			defaultMaterial.Set("u_viewMatrix", GetCameraMatrix() * GetPerspectiveMatrix());

			level.Draw(null, defaultMaterial, Matrix4x4.Identity, CullMode.Back);

			foreach (var entity in entities)
			{
				if (entity == player) continue;

				var offset = entity.GetInterpolatedPosition();
				sphere.Draw(null, defaultMaterial, Matrix4x4.CreateTranslation(offset), CullMode.Back);
			}
		}

		GUI.Render();

		//
	}

	//
	//
	//

    public static Material MakeMaterial((string, string) path) => MakeMaterial(path.Item1, path.Item2);

    public static Material MakeMaterial(string vertexPath, string fragmentPath)
    {
        string vertexData = File.ReadAllText(vertexPath);
        string fragmentData = File.ReadAllText(fragmentPath);

        return new Material(new Shader(new ShaderCreateInfo(vertexData, fragmentData)));
    }

	public readonly static Dictionary<Renderers, Dictionary<string, (string, string)>> ShaderInfo = new()
	{
		[Renderers.OpenGL] = new()
		{
			["Default"] = ("Assets/Shaders/default.vert.glsl", "Assets/Shaders/default.frag.glsl"),
			["Sky"] = ("Assets/Shaders/sky.vert.glsl", "Assets/Shaders/sky.frag.glsl"),
			["Atmosphere"] = ("Assets/Shaders/atmosphere.vert.glsl", "Assets/Shaders/atmosphere.frag.glsl"),
		},

		// TODO: DirectX 11 shaders
	};
}