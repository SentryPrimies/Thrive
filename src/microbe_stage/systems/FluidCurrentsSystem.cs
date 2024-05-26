namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using Newtonsoft.Json;
//using FastNoiseLite = FastNoiseLite;
using World = DefaultEcs.World;

/// <summary>
///   Gives a push from currents in a fluid to physics entities (that have <see cref="ManualPhysicsControl"/>).
///   Only acts on entities marked with <see cref="CurrentAffected"/>.
/// </summary>
[With(typeof(CurrentAffected))]
[With(typeof(Physics))]
[With(typeof(ManualPhysicsControl))]
[With(typeof(WorldPosition))]
[ReadsComponent(typeof(CurrentAffected))]
[ReadsComponent(typeof(Physics))]
[ReadsComponent(typeof(WorldPosition))]
[RuntimeCost(8)]
[JsonObject(MemberSerialization.OptIn)]
public sealed class FluidCurrentsSystem : AEntitySetSystem<float>
{
    private const float DISTURBANCE_TIMESCALE = 1.000f;
    private const float CURRENTS_TIMESCALE = 1.0f / 500.0f;
    private const float MIN_CURRENT_INTENSITY = 0.4f;
    private const float DISTURBANCE_TO_CURRENTS_RATIO = 0.15f;
    private const float NOISE_SCALE = 320.0f;

    // TODO: test the inbuilt fast noise in Godot to see if it is faster / a good enough replacement
    // TODO: re-implement 'disturbances'
    
    //private readonly Noise noiseCurrents;
    //private readonly Image noiseImage;
    private int vectorFieldWidth;
    private int vectorFieldHeight;
    private Vector2[] vectorField;
    // private readonly Vector2 scale = new Vector2(0.05f, 0.05f);

    [JsonProperty]
    private float currentsTimePassed;

    private async void LoadNoiseImage() {
        var noiseTexture = GD.Load<NoiseTexture2D>("res://src/microbe_stage/systems/CurrentNoiseTexture.tres");
        var noiseImage = noiseTexture.GetImage();
        if (noiseImage == null)
        {
            await new Signal(noiseTexture, "changed");
            noiseImage = noiseTexture.GetImage();
        }

        vectorFieldWidth = noiseImage.GetWidth();
        vectorFieldHeight = noiseImage.GetHeight();

        vectorField = new Vector2[vectorFieldWidth * vectorFieldHeight];

        // Convert to vector2 tangent-graident field
        for (int y = 0; y < vectorFieldHeight; y++)
        {
            for (int x = 0; x < vectorFieldWidth; x++)
            {
                float sampleOrigin = (float)noiseImage.GetPixel(x, y)[0];
                float sampleX = (float)noiseImage.GetPixel((x + 30) % vectorFieldWidth, y)[0];
                float sampleY = (float)noiseImage.GetPixel(x, (y + 30) % vectorFieldHeight)[0];
                Vector2 gradient = new(sampleOrigin - sampleX, sampleOrigin - sampleY);
                Vector2 gradientTangent = new(-gradient.Y, gradient.X);
                vectorField[y * vectorFieldWidth + x] = gradientTangent.Normalized();
            }
        }

        // Saves the output for debug purposes
        var imageDebug = Image.Create(vectorFieldWidth, vectorFieldHeight, false, (Image.Format)4);
        for (int i = 0; i < vectorFieldWidth * vectorFieldHeight; ++i)
        {
            float r = Mathf.Clamp(vectorField[i].X, -1.0f, 1.0f) * 0.5f + 0.5f;
            float b =  Mathf.Clamp(vectorField[i].Y, -1.0f, 1.0f) * 0.5f + 0.5f;
            imageDebug.SetPixel(i / vectorFieldWidth, i % vectorFieldWidth,
                new Color(r, 0f, b));
        }
        imageDebug.SavePng("user://testCurrent.png");

    }

    public FluidCurrentsSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_HIGHER_ENTITIES_PER_THREAD)
    {
        LoadNoiseImage();
    }

    /// <summary>
    ///   JSON constructor for creating temporary instances used to apply the child properties
    /// </summary>
    [JsonConstructor]
    public FluidCurrentsSystem(float currentsTimePassed) : base(TemporarySystemHelper.GetDummyWorldForLoad(), null)
    {
        this.currentsTimePassed = currentsTimePassed;

        //noiseImage = null!;
    }

    public Vector2 VelocityAt(Vector2 position)
    {
        var scaledPosition = new Vector2(position.X * vectorFieldWidth, position.Y * vectorFieldHeight) / NOISE_SCALE;
        scaledPosition = scaledPosition.PosMod(new Vector2(vectorFieldWidth, vectorFieldHeight));
        var currentsVelocity = vectorField[((int)scaledPosition.Y) * vectorFieldWidth + ((int)scaledPosition.X)];
        currentsVelocity = currentsVelocity.Normalized();

        // TODO: Re-implement min-velocity
        return currentsVelocity;
    }

    protected override void PreUpdate(float delta)
    {
        base.PreUpdate(delta);

        currentsTimePassed += delta;
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var physics = ref entity.Get<Physics>();

        if (physics.Body == null)
            return;

        ref var position = ref entity.Get<WorldPosition>();
        ref var physicsControl = ref entity.Get<ManualPhysicsControl>();

        var pos = new Vector2(position.Position.X, position.Position.Z);
        var vel = VelocityAt(pos) * 100000.0f * Constants.MAX_FORCE_APPLIED_BY_CURRENTS;

        physicsControl.ImpulseToGive += new Vector3(vel.X, 0, vel.Y) * delta;
        physicsControl.PhysicsApplied = false;

    }
}
