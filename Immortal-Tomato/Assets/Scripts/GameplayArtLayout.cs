using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The kitchen backgrounds are native 2172 x 724 pixel panoramas, imported at
/// 100 PPU and displayed at 1.55 scale.  This is the single source of truth
/// for every gameplay collider: values here are measured from the painted
/// source art, never copied back from an existing Unity collider.
/// </summary>
public static class GameplayArtLayout
{
    public const float NativeWidthPixels = 2172f;
    public const float NativeHeightPixels = 724f;
    public const float PixelsPerUnit = 100f;
    public const float BackgroundScale = 1.55f;
    public const float UnitsPerPixel = BackgroundScale / PixelsPerUnit;
    public const float PlatformThickness = .12f;
    public const float CheckpointRespawnOffsetY = -.94f;
    public const float DefaultCameraOrthoSize = 5f;
    public const float DefaultCameraAspect = 16f / 9f;
    // Enemy roots sit on their supporting counter. Their hitbox offsets keep
    // that baseline fixed while these authored visual/hitbox scales change.
    public const float ChefScale = .38f;
    public const float WaiterScale = .34f;

    static readonly float[] SectionCenters = { -33.666f, 0f, 33.666f };

    public readonly struct SurfaceSpec
    {
        public readonly string Name;
        public readonly int Section;
        public readonly float LeftPixel;
        public readonly float RightPixel;
        public readonly float TopPixel;
        public readonly string PaintedReference;
        public readonly bool OneWay;

        public SurfaceSpec(string name, int section, float leftPixel, float rightPixel, float topPixel, string paintedReference, bool oneWay = false)
        {
            Name = name;
            Section = section;
            LeftPixel = leftPixel;
            RightPixel = rightPixel;
            TopPixel = topPixel;
            PaintedReference = paintedReference;
            OneWay = oneWay;
        }

        public float WidthWorld => (RightPixel - LeftPixel) * UnitsPerPixel;
        public float TopWorld => PixelToWorldY(TopPixel);
        public float LeftWorld => PixelToWorldX(Section, LeftPixel);
        public float RightWorld => PixelToWorldX(Section, RightPixel);
        public Vector2 CenterWorld => new((LeftWorld + RightWorld) * .5f, TopWorld - PlatformThickness * .5f);
        public Vector2 ColliderSize => new(WidthWorld, PlatformThickness);
    }

    public readonly struct TriggerSpec
    {
        public readonly string Name;
        public readonly int Section;
        public readonly float LeftPixel;
        public readonly float RightPixel;
        public readonly float TopPixel;
        public readonly float BottomPixel;
        public readonly string PaintedReference;

        public TriggerSpec(string name, int section, float leftPixel, float rightPixel, float topPixel, float bottomPixel, string paintedReference)
        {
            Name = name;
            Section = section;
            LeftPixel = leftPixel;
            RightPixel = rightPixel;
            TopPixel = topPixel;
            BottomPixel = bottomPixel;
            PaintedReference = paintedReference;
        }

        public Bounds ExpectedBounds => BoundsFromPixels(Section, LeftPixel, RightPixel, TopPixel, BottomPixel);
    }

    /// <summary>
    /// A real painted fire opening.  The same measured rectangle is used by
    /// its visual-placement code and by its kill trigger; this prevents a
    /// decorative animation from drifting away from the heat that kills.
    /// </summary>
    public readonly struct FireSpec
    {
        public readonly string Name;
        public readonly int Section;
        public readonly float LeftPixel;
        public readonly float RightPixel;
        public readonly float TopPixel;
        public readonly float BottomPixel;
        public readonly float TriggerLeftPixel;
        public readonly float TriggerRightPixel;
        public readonly float TriggerTopPixel;
        public readonly float TriggerBottomPixel;
        public readonly string PaintedReference;

        public FireSpec(string name, int section, float leftPixel, float rightPixel, float topPixel, float bottomPixel, string paintedReference)
            : this(name, section, leftPixel, rightPixel, topPixel, bottomPixel, leftPixel, rightPixel, topPixel, bottomPixel, paintedReference)
        {
        }

        public FireSpec(string name, int section, float leftPixel, float rightPixel, float topPixel, float bottomPixel,
            float triggerLeftPixel, float triggerRightPixel, float triggerTopPixel, float triggerBottomPixel, string paintedReference)
        {
            Name = name;
            Section = section;
            LeftPixel = leftPixel;
            RightPixel = rightPixel;
            TopPixel = topPixel;
            BottomPixel = bottomPixel;
            TriggerLeftPixel = triggerLeftPixel;
            TriggerRightPixel = triggerRightPixel;
            TriggerTopPixel = triggerTopPixel;
            TriggerBottomPixel = triggerBottomPixel;
            PaintedReference = paintedReference;
        }

        public Bounds TargetBounds => BoundsFromPixels(Section, LeftPixel, RightPixel, TopPixel, BottomPixel);
        public Bounds TriggerBounds => BoundsFromPixels(Section, TriggerLeftPixel, TriggerRightPixel, TriggerTopPixel, TriggerBottomPixel);
        public TriggerSpec Trigger => new(Name, Section, TriggerLeftPixel, TriggerRightPixel, TriggerTopPixel, TriggerBottomPixel, PaintedReference);
    }

    public readonly struct WorldTriggerSpec
    {
        public readonly string Name;
        public readonly Bounds ExpectedBounds;
        public readonly string Reference;

        public WorldTriggerSpec(string name, Bounds expectedBounds, string reference)
        {
            Name = name;
            ExpectedBounds = expectedBounds;
            Reference = reference;
        }
    }

    public readonly struct CheckpointSpec
    {
        public readonly string Name;
        public readonly string SupportName;
        public readonly float SourcePixelX;
        public readonly string PaintedReference;

        public CheckpointSpec(string name, string supportName, float sourcePixelX, string paintedReference)
        {
            Name = name;
            SupportName = supportName;
            SourcePixelX = sourcePixelX;
            PaintedReference = paintedReference;
        }
    }

    public readonly struct EnemyAnchorSpec
    {
        public readonly string Name;
        public readonly string SupportName;
        public readonly float SourcePixelX;
        public readonly string PaintedReference;

        public EnemyAnchorSpec(string name, string supportName, float sourcePixelX, string paintedReference)
        {
            Name = name;
            SupportName = supportName;
            SourcePixelX = sourcePixelX;
            PaintedReference = paintedReference;
        }
    }

    // Measurements are the bright upper edge of the painted metal/ice
    // counter trim.  They deliberately do not use the dark wall/floor below
    // it.  The order is the playable route from the green entrance to the red
    // exit door.
    static readonly SurfaceSpec[] routeSurfaces =
    {
        new("S1 Door Service Counter", 0,    0,  444, 449, "S1 green-door service counter top"),
        new("S1 Stove Worktop",        0,  538,  724, 398, "S1 left stove preparation table top"),
        new("S1 Prep Island",          0,  857, 1191, 411, "S1 central produce-island counter top"),
        // The raised shelf is the painted, safe route over the stove flame.
        // The lower sink front under it is scenery: making it solid forced the
        // capsule into the flame's full-height art rectangle.
        new("S1 Flame Bypass Shelf",   0, 1264, 1424, 358, "S1 raised bowl shelf immediately before stove flame"),
        new("S1 Right Service Counter",0, 1625, 1955, 416, "S1 right stove counter top"),
        new("S1 Exit Counter",         0, 1931, 2172, 309, "S1 raised right-hand service counter top"),

        new("S2 Entry Counter",        1,    0,  139, 289, "S2 left seam counter top"),
        new("S2 Left Stove Counter",   1,  113,  383, 419, "S2 left burner counter top"),
        new("S2 Knife Gap Ledge",      1,  384,  560, 454, "S2 short counter lip before knife pit"),
        new("S2 Oven Counter",         1,  733, 1193, 369, "S2 pizza-oven counter top"),
        new("S2 Burner Counter",       1, 1194, 1454, 382, "S2 right stove counter top"),
        new("S2 Burner Exit Ledge",    1, 1656, 1788, 454, "S2 small right ledge after burner gap"),
        new("S2 Mixer Counter",        1, 1795, 2140, 419, "S2 mixer workstation counter top"),

        new("S3 Entry Counter",        2,    0,  160, 310, "S3 left seam counter top"),
        new("S3 Left Counter",         2,  122,  328, 423, "S3 left chilled counter top"),
        new("S3 Freezer Ledge",        2,  472,  684, 438, "S3 freezer-front ice ledge"),
        new("S3 Produce Counter",      2,  833, 1073, 378, "S3 chilled produce counter top"),
        new("S3 Crate Shelf",          2, 1073, 1202, 423, "S3 crate shelf counter top"),
        new("S3 Frozen Bridge",        2, 1336, 1551, 520, "S3 low frozen bridge top"),
        new("S3 Sausage Counter",      2, 1555, 1799, 421, "S3 sausage-station counter top"),
        new("S3 Exit Counter",         2, 1920, 2172, 309, "S3 red-door exit counter top"),
    };

    // These are the small wall-mounted drawers/shelves which are visibly
    // usable above the main route.  They are deliberately a separate catalog:
    // the main route audit remains readable, while every painted support in
    // the scene is still authored from an exact source-pixel top edge.
    static readonly SurfaceSpec[] upperShelfSurfaces =
    {
        new("S1 Upper Left Plate Shelf",       0,    0,  164, 207, "S1 upper-left wall shelf / plate drawer top", true),
        new("S1 Upper Condiment Shelf",        0,  408,  624, 171, "S1 upper condiment wall shelf top", true),
        new("S1 Upper Plate Drawer",           0,  732,  878, 326, "S1 small centre plate drawer top", true),
        new("S1 Upper Produce Shelf",          0,  907, 1222, 151, "S1 long upper produce wall shelf top", true),
        new("S1 Upper Pass Shelf",             0, 1215, 1362, 231, "S1 small hanging pass shelf top", true),
        new("S1 Upper Right Service Shelf",    0, 1427, 1547, 331, "S1 right service plate-and-bread shelf top", true),
        new("S1 Upper Pot Shelf",              0, 1464, 1646, 258, "S1 upper pot service shelf top", true),
        new("S1 Upper Exit Produce Shelf",     0, 1819, 2031, 151, "S1 upper exit produce wall shelf top", true),
        new("S1 Upper Exit Plate Shelf",       0, 1874, 2047, 289, "S1 upper exit plate shelf top", true),

        new("S2 Upper Entry Produce Shelf",    1,    0,  151, 170, "S2 upper entry produce wall shelf top", true),
        new("S2 Upper Entry Condiment Shelf",  1,  160,  307, 250, "S2 small entry condiment drawer top", true),
        new("S2 Upper Left Plate Shelf",       1,  510,  754, 220, "S2 upper-left plate shelf top", true),
        new("S2 Upper Oven Plate Drawer",      1,  579,  720, 334, "S2 small oven-side plate drawer top", true),
        new("S2 Upper Oven Ingredient Shelf",  1, 1044, 1340, 194, "S2 long oven ingredient wall shelf top", true),
        new("S2 Upper Right Plate Drawer",     1, 1503, 1640, 250, "S2 small right plate drawer top", true),
        new("S2 Upper Mixer Produce Shelf",    1, 1654, 1824, 221, "S2 upper mixer produce shelf top", true),
        new("S2 Upper Far Plate Shelf",        1, 2110, 2172, 206, "S2 far-right clipped plate shelf top", true),

        new("S3 Upper Entry Produce Shelf",    2,   39,  310, 194, "S3 upper entry produce wall shelf top", true),
        new("S3 Upper Entry Plate Drawer",     2,   36,  159, 301, "S3 small entry plate drawer top", true),
        new("S3 Upper Freezer Shelf",          2,  222,  403, 301, "S3 upper freezer condiment shelf top", true),
        new("S3 Upper Sausage Shelf",          2,  824, 1063, 301, "S3 upper sausage ingredient shelf top", true),
        new("S3 Upper Cold Plate Shelf",       2, 1190, 1391, 242, "S3 upper cold-room plate shelf top", true),
        new("S3 Upper Exit Produce Shelf",     2, 1537, 1716, 153, "S3 upper exit produce wall shelf top", true),
        new("S3 Upper Exit Cloche Shelf",      2, 1559, 1750, 270, "S3 upper exit cloche shelf top", true),
        new("S3 Upper Exit Plate Shelf",       2, 1757, 1923, 301, "S3 upper exit plate drawer top", true),
    };

    // Every visible S1/S2 fire opening was measured directly in its panorama.
    // The flame frames are keyed full-canvas art, so AnimatedFirePlacement
    // fits their keyed pixels inside these rectangles rather than placing a
    // guessed sprite centre somewhere near the stove.
    static readonly FireSpec[] fires =
    {
        // Its left 94 source pixels overlap the green-door spawn capsule in
        // the background depth plane. The reachable right-hand 106px flame
        // tongue is still a large, exact visual sub-rectangle and leaves a
        // 13px measured gap to the initial capsule.
        // Keep the lethal trigger on the reachable right-hand tongue.  The
        // painted left tongue sits directly beside the green-door spawn and
        // is decorative until the player has cleared the opening.
        new("S1 Left Stove Fire",     0,  144,  344, 366, 405, 270, 340, 370, 402, "S1 left stove burner flame row; reachable right flame tongue"),
        new("S1 Right Stove Fire",    0, 1435, 1538, 377, 421, "S1 right stove flame tongues"),
        new("S2 Left Stove Fire",     1,  151,  316, 337, 384, "S2 left stove burner flame row"),
        new("S2 Pizza Oven Fire",    1,  887, 1009, 275, 366, "S2 pizza-oven firebox"),
        new("S2 Counter Burner Fire",1, 1304, 1438, 352, 386, "S2 counter burner flame row"),
        new("S2 Floor Flame Columns",1, 1456, 1644, 454, 568, "S2 three foreground burner-flame columns"),
    };

    // Death triggers cover every fire/knife/spike/hot-pot zone that the
    // player can touch. Fire rectangles intentionally stay tight to the
    // painted flame pixels, even when that makes a counter-top fire lethal.
    static readonly TriggerSpec[] hazards =
    {
        new("S1 Knife Tip",         0,  462,  515, 500, 568, "S1 floor knife-cluster sharp blades"),
        new("S1 Boiling Pot",       0,  742,  851, 504, 551, "S1 boiling stock-pot surface"),
        fires[0].Trigger,
        fires[1].Trigger,
        new("S2 Knife Pit",         1,  567,  723, 500, 568, "S2 knife-pit sharp blades"),
        fires[2].Trigger,
        fires[3].Trigger,
        fires[4].Trigger,
        fires[5].Trigger,
        new("S3 Ice Spike Group A", 2,  310,  491, 488, 545, "S3 freezer approach ice spikes"),
        new("S3 Ice Spike Group B", 2, 1208, 1350, 514, 568, "S3 centre floor ice-spike bank"),
        new("S3 Ice Spike Group C", 2, 1799, 1928, 488, 546, "S3 exit approach ice spikes"),
    };

    // The painted panorama ends at y=-5.611. This trigger extends 24.5 units
    // beyond both panorama edges, so a full-speed jump past the open final
    // counter still falls into it.
    static readonly WorldTriggerSpec fallHazard = new(
        "Out Of Map Fall",
        new Bounds(new Vector3(0f, -8.1f), new Vector3(150f, 4f)),
        "full map width below the painted panorama");

    public readonly struct BoundarySpec
    {
        public readonly string Name;
        public readonly float InnerFacePixelX;
        public readonly float BottomWorld;
        public readonly float TopWorld;
        public readonly float Thickness;

        public BoundarySpec(string name, float innerFacePixelX, float bottomWorld, float topWorld, float thickness)
        {
            Name = name;
            InnerFacePixelX = innerFacePixelX;
            BottomWorld = bottomWorld;
            TopWorld = topWorld;
            Thickness = thickness;
        }

        public float InnerFaceWorldX => PixelToWorldX(0, InnerFacePixelX);
        public Vector2 CenterWorld => new(InnerFaceWorldX - Thickness * .5f, (BottomWorld + TopWorld) * .5f);
        public Vector2 ColliderSize => new(Thickness, TopWorld - BottomWorld);
    }

    // The collider's inside face is exactly at the left edge of the painted
    // panorama.  Its height safely covers all player jump/recovery space, but
    // it is not a floor or a kill wall.
    static readonly BoundarySpec leftWorldBoundary = new("World Left Boundary", 0f, -9f, 15f, .12f);

    static readonly CheckpointSpec[] checkpoints =
    {
        new("Start Checkpoint",             "S1 Door Service Counter", 180,  "S1 green door threshold / starting service counter"),
        new("S1 Red Door Checkpoint",       "S1 Exit Counter",         2060, "S1 red door threshold on raised exit counter"),
        new("S3 Freezer Door Checkpoint",   "S3 Freezer Ledge",         600, "S3 freezer door threshold on ice ledge"),
        new("S3 Final Red Door Checkpoint", "S3 Exit Counter",         2040, "S3 final red door threshold on exit counter"),
    };

    static readonly EnemyAnchorSpec[] enemyAnchors =
    {
        new("Chef Enemy",   "S2 Mixer Counter",   1990, "chef standing on S2 mixer workstation"),
        new("Waiter Enemy", "S2 Burner Counter",   1396, "waiter standing on S2 burner counter"),
    };

    // The root is 37 source pixels to the right of the green door's right
    // jamb (x=143).  With the 0.54-scale capsule this puts its left edge at
    // x≈136: it emerges from the doorway rather than beginning on the old
    // arbitrary prep table.
    public const float DoorSpawnPixelX = 180f;
    public const string DoorSupportName = "S1 Door Service Counter";

    public static IReadOnlyList<SurfaceSpec> RouteSurfaces => routeSurfaces;
    public static IReadOnlyList<SurfaceSpec> UpperShelfSurfaces => upperShelfSurfaces;
    public static IReadOnlyList<FireSpec> Fires => fires;
    public static IReadOnlyList<TriggerSpec> Hazards => hazards;
    public static WorldTriggerSpec FallHazard => fallHazard;
    public static IReadOnlyList<CheckpointSpec> Checkpoints => checkpoints;
    public static IReadOnlyList<EnemyAnchorSpec> EnemyAnchors => enemyAnchors;
    public static BoundarySpec LeftWorldBoundary => leftWorldBoundary;

    public static IEnumerable<SurfaceSpec> AllSolidSurfaces
    {
        get
        {
            foreach (SurfaceSpec surface in routeSurfaces) yield return surface;
            foreach (SurfaceSpec surface in upperShelfSurfaces) yield return surface;
        }
    }

    public static float PixelToWorldX(int section, float sourcePixelX) =>
        SectionCenters[section] + (sourcePixelX - NativeWidthPixels * .5f) * UnitsPerPixel;

    public static float PixelToWorldY(float sourcePixelY) =>
        (NativeHeightPixels * .5f - sourcePixelY) * UnitsPerPixel;

    public static Bounds BoundsFromPixels(int section, float leftPixel, float rightPixel, float topPixel, float bottomPixel)
    {
        float left = PixelToWorldX(section, leftPixel);
        float right = PixelToWorldX(section, rightPixel);
        float top = PixelToWorldY(topPixel);
        float bottom = PixelToWorldY(bottomPixel);
        return new Bounds(new Vector3((left + right) * .5f, (top + bottom) * .5f), new Vector3(right - left, top - bottom));
    }

    public static SurfaceSpec Surface(string name)
    {
        foreach (SurfaceSpec surface in AllSolidSurfaces)
            if (surface.Name == name) return surface;
        throw new ArgumentException("Unknown painted route surface: " + name, nameof(name));
    }

    public static TriggerSpec Hazard(string name)
    {
        foreach (TriggerSpec hazard in hazards)
            if (hazard.Name == name) return hazard;
        throw new ArgumentException("Unknown painted hazard: " + name, nameof(name));
    }

    public static FireSpec Fire(string name)
    {
        foreach (FireSpec fire in fires)
            if (fire.Name == name) return fire;
        throw new ArgumentException("Unknown painted fire: " + name, nameof(name));
    }

    public static CheckpointSpec Checkpoint(string name)
    {
        foreach (CheckpointSpec checkpoint in checkpoints)
            if (checkpoint.Name == name) return checkpoint;
        throw new ArgumentException("Unknown checkpoint: " + name, nameof(name));
    }

    public static Vector2 SupportPoint(string supportName, float sourcePixelX)
    {
        SurfaceSpec support = Surface(supportName);
        return new Vector2(PixelToWorldX(support.Section, sourcePixelX), support.TopWorld);
    }

    public static Vector2 DoorSpawn => SupportPoint(DoorSupportName, DoorSpawnPixelX);

    // Camera clamp centres for the 1280x720 game view.  At the green-door
    // spawn this shows the entire tomato capsule with 2.10 world units of
    // room to its left, instead of leaving it outside the frame.
    public static float DefaultCameraHalfWidth => DefaultCameraOrthoSize * DefaultCameraAspect;
    public static float DefaultCameraMinX => PixelToWorldX(0, 0f) + DefaultCameraHalfWidth;
    public static float DefaultCameraMaxX => PixelToWorldX(2, NativeWidthPixels) - DefaultCameraHalfWidth;

    public static Vector2 CheckpointCenter(CheckpointSpec checkpoint)
    {
        Vector2 support = SupportPoint(checkpoint.SupportName, checkpoint.SourcePixelX);
        return support + Vector2.up * -CheckpointRespawnOffsetY;
    }

    public static Vector2 EnemyRoot(EnemyAnchorSpec enemy) => SupportPoint(enemy.SupportName, enemy.SourcePixelX);

    public static float SectionCenter(int section) => SectionCenters[section];
}
