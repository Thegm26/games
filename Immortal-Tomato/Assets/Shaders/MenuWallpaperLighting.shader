Shader "ImmortalTomato/MenuWallpaperLighting"
{
    Properties
    {
        [PerRendererData] _MainTex ("Wallpaper", 2D) = "white" {}
        _SelectionRect ("Selection Rect", Vector) = (0, 0, 0, 0)
        _LampAnchor ("Lamp Anchor", Vector) = (0, 0, 0, 0)
        _SelectionStrength ("Selection Strength", Range(0, 1)) = 0
        _PressFlash ("Press Flash", Range(0, 1)) = 0
        _Pulse ("Pulse", Range(0, 1)) = 0
        _AmbientTime ("Ambient Time", Float) = 0
        _LoadingActive ("Loading Active", Range(0, 1)) = 0
        _LoadingProgress ("Loading Progress", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _SelectionRect;
            float4 _LampAnchor;
            float _SelectionStrength;
            float _PressFlash;
            float _Pulse;
            float _AmbientTime;
            float _LoadingActive;
            float _LoadingProgress;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // `point` is a reserved token in some desktop Cg/GLCore compilers.
            // Use explicit coordinate names so this helper compiles consistently.
            float softEllipse(float2 uvPosition, float2 center, float2 radius, float edge)
            {
                float distance = length((uvPosition - center) / radius);
                return 1.0 - smoothstep(1.0 - edge, 1.0 + edge, distance);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Drift only the already-painted steam areas. The masks sit in
                // empty kitchen air (not over characters or menu lettering) and
                // feather completely away at their edges.
                float overheadSteam = softEllipse(i.uv, float2(.475, .805), float2(.080, .135), .62);
                float coolDoorSteam = softEllipse(i.uv, float2(.355, .355), float2(.105, .145), .66);
                float potSteam = softEllipse(i.uv, float2(.522, .535), float2(.039, .080), .65);
                float steamMask = max(overheadSteam, max(coolDoorSteam, potSteam));
                // Tight, lower-amplitude wisps at the two Uzi muzzles and cigarette.
                // Their centers sit just inside painted smoke, not on the gun barrels,
                // hands, or tomato face.
                float leftUziWisp = softEllipse(i.uv, float2(.575, .552), float2(.020, .055), .70);
                float rightUziWisp = softEllipse(i.uv, float2(.875, .566), float2(.020, .060), .70);
                float cigaretteWisp = softEllipse(i.uv, float2(.772, .465), float2(.021, .058), .70);
                float wispMask = max(leftUziWisp, max(rightUziWisp, cigaretteWisp));
                float2 steamFlow = float2(
                    sin(i.uv.y * 38.0 + _AmbientTime * 1.18) + sin(i.uv.x * 21.0 - _AmbientTime * .73) * .45,
                    sin(i.uv.x * 31.0 - _AmbientTime * .92) + sin(i.uv.y * 17.0 + _AmbientTime * 1.31) * .35);
                float2 wispFlow = float2(
                    sin(i.uv.y * 52.0 + _AmbientTime * 1.67),
                    cos(i.uv.x * 47.0 - _AmbientTime * 1.26));
                float2 animatedUv = i.uv
                    + steamFlow * (.00135 + .00095 * steamMask) * steamMask
                    + wispFlow * .00155 * wispMask;
                fixed4 baseColor = tex2D(_MainTex, animatedUv) * i.color;
                // The displacement alone is easy to miss in a still illustration.
                // A restrained moving highlight makes the painted vapour visibly
                // breathe without touching the characters, signs, or background.
                float steamShimmer = (.5 + .5 * sin(i.uv.x * 44.0 + i.uv.y * 29.0 - _AmbientTime * 2.15)) * steamMask;
                float wispShimmer = (.5 + .5 * sin(i.uv.y * 57.0 - _AmbientTime * 2.85)) * wispMask;
                baseColor.rgb += steamShimmer * float3(.030, .038, .041);
                baseColor.rgb += wispShimmer * float3(.042, .047, .046);

                float2 rectCenter = (_SelectionRect.xy + _SelectionRect.zw) * 0.5;
                float2 rectHalf = max((_SelectionRect.zw - _SelectionRect.xy) * 0.5, float2(.001, .001));

                // These wide, feathered light pools colour the original pixels only.
                // They deliberately have no hard perimeter, contour, or duplicate art.
                float signLight = softEllipse(i.uv, rectCenter + float2(.025, 0), rectHalf * float2(1.22, 1.30), .48);
                float wallBounce = softEllipse(i.uv, rectCenter + float2(.10, .005), rectHalf * float2(2.65, 2.20), .60);
                float lampLight = softEllipse(i.uv, _LampAnchor.xy, float2(.052, .082), .58);
                float textLight = softEllipse(i.uv, rectCenter + float2(.043, .0), rectHalf * float2(.62, .36), .52);

                float breathing = .75 + _Pulse * .35;
                float energy = _SelectionStrength * breathing + _PressFlash * .42;
                float warmMetal = (signLight * .39 + wallBounce * .12 + lampLight * .78 + textLight * .30) * energy;
                float redLamp = lampLight * energy;

                // Existing fixtures gently breathe even when no menu item is being
                // selected, giving the room a little life without a screen filter.
                float menuLampA = softEllipse(i.uv, float2(.106, .826), float2(.025, .040), .68);
                float menuLampB = softEllipse(i.uv, float2(.106, .667), float2(.025, .040), .68);
                float menuLampC = softEllipse(i.uv, float2(.106, .509), float2(.025, .040), .68);
                float ceilingWarmth = softEllipse(i.uv, float2(.128, .938), float2(.072, .075), .72)
                    + softEllipse(i.uv, float2(.710, .900), float2(.075, .085), .72)
                    + softEllipse(i.uv, float2(.962, .906), float2(.072, .082), .72);
                float fixtureMask = saturate(menuLampA + menuLampB + menuLampC + ceilingWarmth);
                float fixtureBreath = (.030 + (.5 + .5 * sin(_AmbientTime * 1.58)) * .052) * fixtureMask;

                // Multiplication preserves the painted texture while a small additive
                // component lets the existing red bulb and lettering feel energized.
                baseColor.rgb *= 1.0 + warmMetal * float3(.48, .16, .035);
                baseColor.rgb += warmMetal * float3(.095, .024, .004);
                baseColor.rgb += redLamp * float3(.11, .006, .001);
                baseColor.rgb *= 1.0 + fixtureBreath * float3(.85, .32, .08);
                baseColor.rgb += fixtureBreath * float3(.025, .006, .001);

                // The loading gauge lives *inside* the weathered brass plate
                // already painted at the foot of the information board. This mask
                // only changes the plate's own pixels: its edge, scratches, bolts,
                // and perspective stay visible, so it cannot read as a pasted UI
                // rail or a flat red rectangle.
                float plateLeft = smoothstep(.109, .121, i.uv.x);
                float plateRight = 1.0 - smoothstep(.268, .280, i.uv.x);
                float plateBottom = smoothstep(.430, .441, i.uv.y);
                float plateTop = 1.0 - smoothstep(.495, .506, i.uv.y);
                float plateMask = plateLeft * plateRight * plateBottom * plateTop;

                // Leave the two painted mounting bolts completely untouched.
                float leftBolt = softEllipse(i.uv, float2(.122, .468), float2(.010, .020), .48);
                float rightBolt = softEllipse(i.uv, float2(.267, .468), float2(.010, .020), .48);
                plateMask *= 1.0 - saturate(max(leftBolt, rightBolt));

                float platePosition = saturate((i.uv.x - .123) / .143);
                float completed = 1.0 - smoothstep(_LoadingProgress - .008, _LoadingProgress + .008, platePosition);
                // A slim travelling gleam communicates the active boundary while
                // the completed region remains a warm version of the real brass.
                float leadingGleam = 1.0 - smoothstep(.0, .012, abs(platePosition - _LoadingProgress));
                float loadingMask = plateMask * completed * _LoadingActive;
                float gleamMask = plateMask * leadingGleam * _LoadingActive;
                float flicker = .86 + .14 * sin(_AmbientTime * 9.0 + i.uv.x * 95.0);
                // The baked plate starts warm, so the powered section needs a
                // confident but still texture-preserving amber lift to be legible.
                baseColor.rgb *= 1.0 + loadingMask * float3(.40, .115, .018) * flicker;
                baseColor.rgb += loadingMask * float3(.105, .023, .003) * flicker;
                baseColor.rgb += gleamMask * float3(.150, .052, .008);
                return baseColor;
            }
            ENDCG
        }
    }
}
