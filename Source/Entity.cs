#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Numerics;

namespace FosterTest;

public abstract class Entity
{
    public Vector3 position;
    public Vector3 positionPrev;

    public Vector2 rotation;
    public Vector3 velocity;

    private Vector3 movementDir;

    public bool isGrounded;
    public bool isTouching;

    public bool shouldJump;
    public int jumpGraceTicks;

    public Vector3 Facing => Vector3.Normalize(new(
        -MathF.Sin(rotation.Y) * MathF.Abs(MathF.Sin(rotation.X)),
         MathF.Cos(rotation.Y) * MathF.Abs(MathF.Sin(rotation.X)),
        -MathF.Cos(rotation.X)));

    public abstract Collision.ISolid Collider { get; }
    public abstract float EyeOffset { get; }

    public void DoJump()
    {
        shouldJump = true;
        jumpGraceTicks = 15;
    }

    public void SetMovementDir(Vector3 dir)
    {
        movementDir = dir != Vector3.Zero ? Vector3.Normalize(dir) : dir;
    }

    public void PreFixedUpdate()
    {
        positionPrev = position;
    }

    public void FixedUpdate(Level col)
    {
        // Gravity
        velocity.Z -= 0.002f;

        // Crappy player movement
        float movSpeed = 0.01f;
        var mov = movementDir * movSpeed;

        velocity += isGrounded ? mov : (mov * 0.4f * ((velocity.Length() > 0.07f && (velocity + mov).Length() > velocity.Length()) ? 0.1f : 1.0f));

        // Drag / friction

        if (isGrounded)
        {
            velocity.X *= 0.88f;
            velocity.Y *= 0.88f;
        }
        else
        {
            velocity.X *= 0.994f;
            velocity.Y *= 0.994f;
        }

        velocity.Z *= 0.994f;

        position += velocity;
        var collider = this.Collider;

        isGrounded = false;

        // Test collision against (all) triangles in the level

        for (int i = 0; i < col.indices.Count / 3; i++)
        {
            var tri = new Collision.Triangle(
                col.vertices[col.indices[i * 3 + 0]].Pos,
                col.vertices[col.indices[i * 3 + 1]].Pos,
                col.vertices[col.indices[i * 3 + 2]].Pos
            );

            if (collider.IntersectTriangle(tri, out var normal, out var depth))
            {
                var change = normal * Vector3.Dot(Vector3.Normalize(velocity), normal);

                velocity -= change * velocity.Length();
                position += normal * depth;

                collider = Collider;

                if (normal.Z > 0) isGrounded = true;
            }

            if (velocity.Length() < 0.0001) break;
        }

        // Jumping

        if (jumpGraceTicks > 0) jumpGraceTicks -= 1;

        if (isGrounded && jumpGraceTicks > 0)
        {
            velocity.Z += Math.Max(0.1f - velocity.Z * 0.2f, 0.0f);
            jumpGraceTicks = 0;
        }

        shouldJump = false;
    }

    public Vector3 GetInterpolatedPosition()
    {
        float alpha = Game.fixedUpdateAccumulator / Game.FixedUpdateTimestep;

        return (position * alpha) + (positionPrev * (1.0f - alpha));
    }
}

public class Player : Entity
{
    public Player(Vector3 inPosition, Vector2 inRotation, Vector3 inVelocity)
    {
        this.position = inPosition;
        this.positionPrev = inPosition;
        this.rotation = inRotation;
        this.velocity = inVelocity;
    }

    public override Collision.ISolid Collider => new Collision.Capsule(this.position, 0.5f, 1.8f);
    public override float EyeOffset => 1.6f;
}

public class TestSphere : Entity
{
    private const float Radius = 1.0f;

    public TestSphere(Vector3 inPosition, Vector3 inVelocity)
    {
        this.position = inPosition;
        this.positionPrev = inPosition;
        this.rotation = Vector2.Zero;
        this.velocity = inVelocity;
    }

    public override Collision.ISolid Collider => new Collision.Sphere(this.position, Radius);
    public override float EyeOffset => 0.0f;
}