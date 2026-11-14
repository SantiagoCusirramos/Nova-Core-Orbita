using UnityEngine;
using System.Collections.Generic;

namespace NovaCore
{
    /// <summary>
    /// PORTADO de NASA-SPACE-APPS (Assets/SpaceStation5/Scripts/StationMeshBuilder.cs), sin cambios
    /// de lógica salvo el namespace. En Nova se usa a escala ISS (módulos de pocos metros):
    /// p. ej. CreateTaperedCylinderMesh para el casco de la Cúpula y CreateBeveledArchFrameMesh
    /// para marcos de escotilla. Las mallas generadas en Editor se guardan como .asset.
    ///
    /// Original — Advanced procedural mesh generation for Space Station 5:
    /// - Octagonal chamfered sci-fi corridors (straight and curved)
    /// - Seamless beveled bulkhead arch frames and recessed LED light channels
    /// - Continuous aerodynamic curved hull sections (no sawteeth or disjointed boxes)
    /// - Structural fairings, gussets, and tapered spine adapters
    /// </summary>
    public static class StationMeshBuilder
    {
        public enum DoorwayWall { None, South, North, East, West }

        // =========================================================================
        // 1. OCTAGONAL / CHAMFERED STRAIGHT CORRIDOR (Radial Spokes & Walkways)
        // =========================================================================

        /// <summary>
        /// Creates an 8-sided octagonal interior corridor mesh along the Z axis.
        /// Features flat walking floor, 45° kickplate chamfers, vertical side walls,
        /// 45° upper cable coves, and flat ceiling.
        /// </summary>
        public static Mesh CreateStraightCorridorMesh(float length, float width, float height)
        {
            Mesh mesh = new Mesh();
            mesh.name = "OctagonalStraightCorridor";

            float hw = width * 0.5f;
            float wFloor = hw * 0.65f;   // Width of flat floor center
            float hBase = 0.40f;        // Height of lower kickplate chamfer
            float wCeil = hw * 0.65f;   // Width of flat ceiling center
            float hCove = 0.50f;        // Height of upper cove chamfer

            // 8 cross-section profile points (ordered counter-clockwise when looking down +Z)
            Vector2[] profile = new Vector2[]
            {
                new Vector2(-wFloor, 0f),           // 0: Floor Left
                new Vector2(-hw,     hBase),         // 1: Wall Bottom Left
                new Vector2(-hw,     height - hCove),// 2: Wall Top Left
                new Vector2(-wCeil,  height),        // 3: Ceiling Left
                new Vector2( wCeil,  height),        // 4: Ceiling Right
                new Vector2( hw,     height - hCove),// 5: Wall Top Right
                new Vector2( hw,     hBase),         // 6: Wall Bottom Right
                new Vector2( wFloor, 0f)            // 7: Floor Right
            };

            int zSegments = Mathf.Max(1, Mathf.CeilToInt(length / 4.0f));
            float zStep = length / zSegments;

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            // Generate quads for each of the 8 panels across each Z segment with dedicated normals (hard-surface)
            for (int s = 0; s < zSegments; s++)
            {
                float z0 = s * zStep;
                float z1 = (s + 1) * zStep;
                float v0 = z0 / 4.0f;
                float v1 = z1 / 4.0f;

                for (int p = 0; p < 8; p++)
                {
                    int nextP = (p + 1) % 8;
                    Vector2 pA = profile[p];
                    Vector2 pB = profile[nextP];

                    Vector3 vA0 = new Vector3(pA.x, pA.y, z0);
                    Vector3 vB0 = new Vector3(pB.x, pB.y, z0);
                    Vector3 vB1 = new Vector3(pB.x, pB.y, z1);
                    Vector3 vA1 = new Vector3(pA.x, pA.y, z1);

                    // Panel normal pointing INWARD
                    Vector2 tangent = pB - pA;
                    Vector3 inwardNorm = new Vector3(tangent.y, -tangent.x, 0f).normalized;

                    int baseIdx = verts.Count;
                    verts.Add(vA0);
                    verts.Add(vB0);
                    verts.Add(vB1);
                    verts.Add(vA1);

                    normals.Add(inwardNorm);
                    normals.Add(inwardNorm);
                    normals.Add(inwardNorm);
                    normals.Add(inwardNorm);

                    uvs.Add(new Vector2(0f, v0));
                    uvs.Add(new Vector2(1f, v0));
                    uvs.Add(new Vector2(1f, v1));
                    uvs.Add(new Vector2(0f, v1));

                    // Winding for inward face
                    tris.Add(baseIdx + 0);
                    tris.Add(baseIdx + 1);
                    tris.Add(baseIdx + 2);

                    tris.Add(baseIdx + 0);
                    tris.Add(baseIdx + 2);
                    tris.Add(baseIdx + 3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        // =========================================================================
        // 2. OCTAGONAL / CHAMFERED CURVED CORRIDOR (Ring Corridors)
        // =========================================================================

        /// <summary>
        /// Creates an 8-sided octagonal curved corridor section along an arc in the XZ plane.
        /// If innerWallOpen is true, the vertical inner wall (facing center) is omitted for seamless spoke portals.
        /// </summary>
        public static Mesh CreateCurvedCorridorMesh(float radius, float startAngleDeg, float arcAngleDeg, float width, float height, int segments, bool innerWallOpen = false, bool outerWallOpen = false)
        {
            Mesh mesh = new Mesh();
            mesh.name = "OctagonalCurvedCorridor";

            float hw = width * 0.5f;
            float wFloor = hw * 0.65f;
            float hBase = 0.40f;
            float wCeil = hw * 0.65f;
            float hCove = 0.50f;

            // 8 cross-section profile points in (local radial offset x, height y)
            // -hw is towards station center (inner), +hw is towards deep space (outer)
            Vector2[] profile = new Vector2[]
            {
                new Vector2(-wFloor, 0f),            // 0: Floor Inner
                new Vector2(-hw,     hBase),          // 1: Inner Wall Bottom
                new Vector2(-hw,     height - hCove), // 2: Inner Wall Top
                new Vector2(-wCeil,  height),         // 3: Ceiling Inner
                new Vector2( wCeil,  height),         // 4: Ceiling Outer
                new Vector2( hw,     height - hCove), // 5: Outer Wall Top
                new Vector2( hw,     hBase),          // 6: Outer Wall Bottom
                new Vector2( wFloor, 0f)             // 7: Floor Outer
            };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float startRad = startAngleDeg * Mathf.Deg2Rad;
            float angleStep = (arcAngleDeg * Mathf.Deg2Rad) / segments;

            for (int s = 0; s < segments; s++)
            {
                float a0 = startRad + s * angleStep;
                float a1 = startRad + (s + 1) * angleStep;

                float cos0 = Mathf.Cos(a0), sin0 = Mathf.Sin(a0);
                float cos1 = Mathf.Cos(a1), sin1 = Mathf.Sin(a1);

                float v0 = (float)s / segments * (arcAngleDeg / 8.0f);
                float v1 = (float)(s + 1) / segments * (arcAngleDeg / 8.0f);

                for (int p = 0; p < 8; p++)
                {
                    // If inner wall is open, skip panel 0 & 1 (Inner Chamfer & Inner Wall)
                    if (innerWallOpen && (p == 0 || p == 1)) continue;

                    // If outer wall is open, skip panel 5 & 6 (Outer Wall & Outer Chamfer)
                    if (outerWallOpen && (p == 5 || p == 6)) continue;

                    int nextP = (p + 1) % 8;
                    Vector2 pA = profile[p];
                    Vector2 pB = profile[nextP];

                    float rA = radius + pA.x;
                    float rB = radius + pB.x;

                    Vector3 vA0 = new Vector3(cos0 * rA, pA.y, sin0 * rA);
                    Vector3 vB0 = new Vector3(cos0 * rB, pB.y, sin0 * rB);
                    Vector3 vB1 = new Vector3(cos1 * rB, pB.y, sin1 * rB);
                    Vector3 vA1 = new Vector3(cos1 * rA, pA.y, sin1 * rA);

                    // Average inward normal for panel
                    Vector3 mid0 = (vA0 + vB0) * 0.5f;
                    Vector3 mid1 = (vA1 + vB1) * 0.5f;
                    Vector3 dirArc = (mid1 - mid0).normalized;
                    Vector3 dirPerim = (vB0 - vA0).normalized;
                    Vector3 norm = Vector3.Cross(dirArc, dirPerim).normalized;

                    int baseIdx = verts.Count;
                    verts.Add(vA0);
                    verts.Add(vB0);
                    verts.Add(vB1);
                    verts.Add(vA1);

                    normals.Add(norm);
                    normals.Add(norm);
                    normals.Add(norm);
                    normals.Add(norm);

                    uvs.Add(new Vector2(0f, v0));
                    uvs.Add(new Vector2(1f, v0));
                    uvs.Add(new Vector2(1f, v1));
                    uvs.Add(new Vector2(0f, v1));

                    tris.Add(baseIdx + 0);
                    tris.Add(baseIdx + 1);
                    tris.Add(baseIdx + 2);

                    tris.Add(baseIdx + 0);
                    tris.Add(baseIdx + 2);
                    tris.Add(baseIdx + 3);
                }

                // If inner wall is open, bridge flat floor from -wFloor to -hw at y = 0
                if (innerWallOpen)
                {
                    Vector3 vF0 = new Vector3(cos0 * (radius - wFloor), 0f, sin0 * (radius - wFloor));
                    Vector3 vOut0 = new Vector3(cos0 * (radius - hw), 0f, sin0 * (radius - hw));
                    Vector3 vOut1 = new Vector3(cos1 * (radius - hw), 0f, sin1 * (radius - hw));
                    Vector3 vF1 = new Vector3(cos1 * (radius - wFloor), 0f, sin1 * (radius - wFloor));

                    int bIdx = verts.Count;
                    verts.Add(vF0); verts.Add(vOut0); verts.Add(vOut1); verts.Add(vF1);
                    normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up);
                    uvs.Add(new Vector2(0f, v0)); uvs.Add(new Vector2(1f, v0)); uvs.Add(new Vector2(1f, v1)); uvs.Add(new Vector2(0f, v1));
                    tris.Add(bIdx + 0); tris.Add(bIdx + 1); tris.Add(bIdx + 2);
                    tris.Add(bIdx + 0); tris.Add(bIdx + 2); tris.Add(bIdx + 3);
                }

                // If outer wall is open, bridge flat floor from wFloor to hw at y = 0
                if (outerWallOpen)
                {
                    Vector3 vF0 = new Vector3(cos0 * (radius + wFloor), 0f, sin0 * (radius + wFloor));
                    Vector3 vOut0 = new Vector3(cos0 * (radius + hw), 0f, sin0 * (radius + hw));
                    Vector3 vOut1 = new Vector3(cos1 * (radius + hw), 0f, sin1 * (radius + hw));
                    Vector3 vF1 = new Vector3(cos1 * (radius + wFloor), 0f, sin1 * (radius + wFloor));

                    int bIdx = verts.Count;
                    verts.Add(vF0); verts.Add(vOut0); verts.Add(vOut1); verts.Add(vF1);
                    normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up);
                    uvs.Add(new Vector2(0f, v0)); uvs.Add(new Vector2(1f, v0)); uvs.Add(new Vector2(1f, v1)); uvs.Add(new Vector2(0f, v1));
                    tris.Add(bIdx + 0); tris.Add(bIdx + 2); tris.Add(bIdx + 1);
                    tris.Add(bIdx + 0); tris.Add(bIdx + 3); tris.Add(bIdx + 2);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        // =========================================================================
        // 3. UNIFIED BEVELED BULKHEAD ARCH FRAME (Replaces 3 primitive Lego boxes)
        // =========================================================================

        /// <summary>
        /// Generates a unified, solid titanium archway frame with 45° chamfered upper corners
        /// and front/rear edge bevels. Completely replaces raw disconnected box primitives.
        /// </summary>
        public static Mesh CreateBeveledArchFrameMesh(float openWidth, float openHeight, float frameThickness, float depth, float chamferSize = 0.65f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "BeveledArchFrame";

            float hwIn = openWidth * 0.5f;
            float hwOut = hwIn + frameThickness;
            float hIn = openHeight;
            float hOut = openHeight + frameThickness;
            float cIn = Mathf.Min(chamferSize, hwIn * 0.5f);
            float cOut = cIn + frameThickness * 0.70f;
            float halfDepth = depth * 0.5f;

            // Inner contour points (6 points along inner doorway cutout)
            Vector2[] inner = new Vector2[]
            {
                new Vector2(-hwIn, 0f),
                new Vector2(-hwIn, hIn - cIn),
                new Vector2(-hwIn + cIn, hIn),
                new Vector2( hwIn - cIn, hIn),
                new Vector2( hwIn, hIn - cIn),
                new Vector2( hwIn, 0f)
            };

            // Outer contour points (6 points along outer perimeter)
            Vector2[] outer = new Vector2[]
            {
                new Vector2(-hwOut, 0f),
                new Vector2(-hwOut, hOut - cOut),
                new Vector2(-hwOut + cOut, hOut),
                new Vector2( hwOut - cOut, hOut),
                new Vector2( hwOut, hOut - cOut),
                new Vector2( hwOut, 0f)
            };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
            {
                int b = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
                normals.Add(n); normals.Add(n); normals.Add(n); normals.Add(n);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
            }

            // 1. Front Face (+Z)
            for (int i = 0; i < 5; i++)
            {
                Vector3 o0 = new Vector3(outer[i].x, outer[i].y, halfDepth);
                Vector3 o1 = new Vector3(outer[i + 1].x, outer[i + 1].y, halfDepth);
                Vector3 i1 = new Vector3(inner[i + 1].x, inner[i + 1].y, halfDepth);
                Vector3 i0 = new Vector3(inner[i].x, inner[i].y, halfDepth);
                AddQuad(o0, o1, i1, i0, Vector3.forward);
            }

            // 2. Back Face (-Z)
            for (int i = 0; i < 5; i++)
            {
                Vector3 o0 = new Vector3(outer[i].x, outer[i].y, -halfDepth);
                Vector3 o1 = new Vector3(outer[i + 1].x, outer[i + 1].y, -halfDepth);
                Vector3 i1 = new Vector3(inner[i + 1].x, inner[i + 1].y, -halfDepth);
                Vector3 i0 = new Vector3(inner[i].x, inner[i].y, -halfDepth);
                AddQuad(o0, i0, i1, o1, Vector3.back);
            }

            // 3. Inner Rim (inside the opening, facing center)
            for (int i = 0; i < 5; i++)
            {
                Vector3 p0 = new Vector3(inner[i].x, inner[i].y, halfDepth);
                Vector3 p1 = new Vector3(inner[i + 1].x, inner[i + 1].y, halfDepth);
                Vector3 p2 = new Vector3(inner[i + 1].x, inner[i + 1].y, -halfDepth);
                Vector3 p3 = new Vector3(inner[i].x, inner[i].y, -halfDepth);

                Vector2 edge = inner[i + 1] - inner[i];
                Vector3 norm = new Vector3(-edge.y, edge.x, 0f).normalized;
                AddQuad(p0, p1, p2, p3, norm);
            }

            // 4. Outer Rim (perimeter facing outward)
            for (int i = 0; i < 5; i++)
            {
                Vector3 p0 = new Vector3(outer[i].x, outer[i].y, halfDepth);
                Vector3 p1 = new Vector3(outer[i + 1].x, outer[i + 1].y, halfDepth);
                Vector3 p2 = new Vector3(outer[i + 1].x, outer[i + 1].y, -halfDepth);
                Vector3 p3 = new Vector3(outer[i].x, outer[i].y, -halfDepth);

                Vector2 edge = outer[i + 1] - outer[i];
                Vector3 norm = new Vector3(edge.y, -edge.x, 0f).normalized;
                AddQuad(p0, p3, p2, p1, norm);
            }

            // 5. Left and Right Base Footings (at y = 0)
            // Left footing
            AddQuad(new Vector3(-hwOut, 0, halfDepth), new Vector3(-hwOut, 0, -halfDepth),
                    new Vector3(-hwIn, 0, -halfDepth), new Vector3(-hwIn, 0, halfDepth), Vector3.down);
            // Right footing
            AddQuad(new Vector3(hwIn, 0, halfDepth), new Vector3(hwIn, 0, -halfDepth),
                    new Vector3(hwOut, 0, -halfDepth), new Vector3(hwOut, 0, halfDepth), Vector3.down);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        // =========================================================================
        // 4. RECESSED LED ACCENT CHANNEL MESH (Integrated architectural glow strip)
        // =========================================================================

        /// <summary>
        /// Creates a sleek, recessed LED strip mesh that nests inside the beveled arch frame opening.
        /// Replaces blinding flat cuboid lights with refined movie-style inset lighting.
        /// </summary>
        public static Mesh CreateArchRecessedLightStripMesh(float openWidth, float openHeight, float stripThickness = 0.12f, float stripDepth = 0.15f, float chamferSize = 0.65f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ArchRecessedLightStrip";

            float hw = openWidth * 0.5f;
            float h = openHeight;
            float c = Mathf.Min(chamferSize, hw * 0.5f);
            float halfDepth = stripDepth * 0.5f;

            Vector2[] pts = new Vector2[]
            {
                new Vector2(-hw, 0.05f),
                new Vector2(-hw, h - c),
                new Vector2(-hw + c, h),
                new Vector2( hw - c, h),
                new Vector2( hw, h - c),
                new Vector2( hw, 0.05f)
            };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int i = 0; i < 5; i++)
            {
                Vector3 p0 = new Vector3(pts[i].x, pts[i].y, halfDepth);
                Vector3 p1 = new Vector3(pts[i + 1].x, pts[i + 1].y, halfDepth);
                Vector3 p2 = new Vector3(pts[i + 1].x, pts[i + 1].y, -halfDepth);
                Vector3 p3 = new Vector3(pts[i].x, pts[i].y, -halfDepth);

                Vector2 edge = pts[i + 1] - pts[i];
                Vector3 norm = new Vector3(-edge.y, edge.x, 0f).normalized;

                int b = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
                normals.Add(norm); normals.Add(norm); normals.Add(norm); normals.Add(norm);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));

                tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        // =========================================================================
        // 5. CONTINUOUS SEAMLESS HULL TOROID SECTION (Aerodynamic Outer Station Ring)
        // =========================================================================

        /// <summary>
        /// Creates a continuous curved aerodynamic hull segment for the station's main ring.
        /// Replaces the 36 isolated blocky cubes with seamless curved outer plating,
        /// chamfered top/bottom decks, and smooth cylindrical curvature.
        /// </summary>
        public static Mesh CreateCurvedHullSectionMesh(float centerRadius, float startAngleDeg, float arcAngleDeg, float halfWidth = 16f, float halfHeight = 15f, float deckChamfer = 3.5f, int steps = 6)
        {
            Mesh mesh = new Mesh();
            mesh.name = "SeamlessRingHullSection";

            // Hull cross section in (radial offset x from centerRadius, y):
            // 8 points: Inner deck, Inner wall, Upper chamfer, Outer wall, Lower chamfer
            Vector2[] profile = new Vector2[]
            {
                new Vector2(-halfWidth, -halfHeight + deckChamfer), // 0: Inner Bottom Chamfer
                new Vector2(-halfWidth,  halfHeight - deckChamfer), // 1: Inner Top Chamfer
                new Vector2(-halfWidth + deckChamfer,  halfHeight), // 2: Top Deck Inner
                new Vector2( halfWidth - deckChamfer,  halfHeight), // 3: Top Deck Outer
                new Vector2( halfWidth,  halfHeight - deckChamfer), // 4: Outer Flank Top
                new Vector2( halfWidth, -halfHeight + deckChamfer), // 5: Outer Flank Bottom
                new Vector2( halfWidth - deckChamfer, -halfHeight), // 6: Bottom Deck Outer
                new Vector2(-halfWidth + deckChamfer, -halfHeight)  // 7: Bottom Deck Inner
            };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float startRad = startAngleDeg * Mathf.Deg2Rad;
            float stepRad = (arcAngleDeg * Mathf.Deg2Rad) / steps;

            for (int s = 0; s < steps; s++)
            {
                float a0 = startRad + s * stepRad;
                float a1 = startRad + (s + 1) * stepRad;

                float cos0 = Mathf.Cos(a0), sin0 = Mathf.Sin(a0);
                float cos1 = Mathf.Cos(a1), sin1 = Mathf.Sin(a1);

                float u0 = (float)s / steps * 2.0f;
                float u1 = (float)(s + 1) / steps * 2.0f;

                for (int p = 0; p < 8; p++)
                {
                    int nextP = (p + 1) % 8;
                    Vector2 pA = profile[p];
                    Vector2 pB = profile[nextP];

                    float rA = centerRadius + pA.x;
                    float rB = centerRadius + pB.x;

                    Vector3 vA0 = new Vector3(cos0 * rA, pA.y, sin0 * rA);
                    Vector3 vB0 = new Vector3(cos0 * rB, pB.y, sin0 * rB);
                    Vector3 vB1 = new Vector3(cos1 * rB, pB.y, sin1 * rB);
                    Vector3 vA1 = new Vector3(cos1 * rA, pA.y, sin1 * rA);

                    // Outward facing normal
                    Vector3 mid0 = (vA0 + vB0) * 0.5f;
                    Vector3 mid1 = (vA1 + vB1) * 0.5f;
                    Vector3 dirArc = (mid1 - mid0).normalized;
                    Vector3 dirPerim = (vB0 - vA0).normalized;
                    Vector3 norm = Vector3.Cross(dirPerim, dirArc).normalized;

                    int b = verts.Count;
                    verts.Add(vA0); verts.Add(vB0); verts.Add(vB1); verts.Add(vA1);
                    normals.Add(norm); normals.Add(norm); normals.Add(norm); normals.Add(norm);
                    uvs.Add(new Vector2(u0, 0f)); uvs.Add(new Vector2(u0, 1f));
                    uvs.Add(new Vector2(u1, 1f)); uvs.Add(new Vector2(u1, 0f));

                    tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        // =========================================================================
        // 6. AERODYNAMIC SPOKE-TO-RING FAIRING GUSSET (Transition piece)
        // =========================================================================

        /// <summary>
        /// Creates a flared structural fairing that welds the radial spokes into the outer ring and central hub,
        /// eliminating hard perpendicular collisions between rectangular beams and cylindrical rings.
        /// </summary>
        public static Mesh CreateSpokeFairingGussetMesh(float spokeWidth, float expandedWidth, float height, float length)
        {
            Mesh mesh = new Mesh();
            mesh.name = "SpokeFairingGusset";

            float hwTip = spokeWidth * 0.5f;
            float hwBase = expandedWidth * 0.5f;
            float hh = height * 0.5f;

            // Tip vertices at Z = 0
            Vector3[] tip = new Vector3[]
            {
                new Vector3(-hwTip, -hh, 0f),
                new Vector3(-hwTip,  hh, 0f),
                new Vector3( hwTip,  hh, 0f),
                new Vector3( hwTip, -hh, 0f)
            };

            // Base vertices at Z = length (expanded)
            Vector3[] baseV = new Vector3[]
            {
                new Vector3(-hwBase, -hh, length),
                new Vector3(-hwBase,  hh, length),
                new Vector3( hwBase,  hh, length),
                new Vector3( hwBase, -hh, length)
            };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            void AddFace(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
            {
                int b = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
                normals.Add(n); normals.Add(n); normals.Add(n); normals.Add(n);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
            }

            // Left flared side
            AddFace(tip[0], tip[1], baseV[1], baseV[0], new Vector3(-1, 0, 0.3f).normalized);
            // Top flared side
            AddFace(tip[1], tip[2], baseV[2], baseV[1], new Vector3(0, 1, 0.1f).normalized);
            // Right flared side
            AddFace(tip[2], tip[3], baseV[3], baseV[2], new Vector3(1, 0, 0.3f).normalized);
            // Bottom flared side
            AddFace(tip[3], tip[0], baseV[0], baseV[3], new Vector3(0, -1, 0.1f).normalized);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        // =========================================================================
        // 7. ROOM INTERIOR WITH DOORWAY CUTOUTS
        // =========================================================================

        public static Mesh CreateRoomMesh(float width, float length, float height)
        {
            return CreateRoomMeshWithDoorway(width, length, height, 0f, 0f, DoorwayWall.None);
        }

        public static Mesh CreateRoomMeshWithDoorway(float width, float length, float height, float doorWidth, float doorHeight, DoorwayWall doorWall, bool openNorthWindow = false)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RoomInterior";

            float hw = width * 0.5f;
            float hl = length * 0.5f;
            float hdw = doorWidth * 0.5f;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvScale)
            {
                int baseIndex = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(uvScale.x, 0));
                uvs.Add(new Vector2(uvScale.x, uvScale.y));
                uvs.Add(new Vector2(0, uvScale.y));

                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 2);
                tris.Add(baseIndex + 1);

                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 3);
                tris.Add(baseIndex + 2);
            }

            // Floor (+Y)
            AddQuad(new Vector3(-hw, 0, -hl), new Vector3(hw, 0, -hl), new Vector3(hw, 0, hl), new Vector3(-hw, 0, hl), new Vector2(width / 4f, length / 4f));
            // Ceiling (-Y)
            AddQuad(new Vector3(-hw, height, hl), new Vector3(hw, height, hl), new Vector3(hw, height, -hl), new Vector3(-hw, height, -hl), new Vector2(width / 4f, length / 4f));

            // North Wall (+Z)
            if (openNorthWindow)
            {
                // North wall is open for panoramic space glass viewport
            }
            else if (doorWall == DoorwayWall.North && doorWidth > 0f)
            {
                AddQuad(new Vector3(hw, 0, hl), new Vector3(hw, height, hl), new Vector3(hdw, height, hl), new Vector3(hdw, 0, hl), new Vector2(1, 1));
                AddQuad(new Vector3(-hdw, 0, hl), new Vector3(-hdw, height, hl), new Vector3(-hw, height, hl), new Vector3(-hw, 0, hl), new Vector2(1, 1));
                if (height > doorHeight)
                    AddQuad(new Vector3(hdw, doorHeight, hl), new Vector3(hdw, height, hl), new Vector3(-hdw, height, hl), new Vector3(-hdw, doorHeight, hl), new Vector2(1, 1));
            }
            else
            {
                AddQuad(new Vector3(hw, 0, hl), new Vector3(hw, height, hl), new Vector3(-hw, height, hl), new Vector3(-hw, 0, hl), new Vector2(width / 4f, height / 4f));
            }

            // South Wall (-Z)
            if (doorWall == DoorwayWall.South && doorWidth > 0f)
            {
                AddQuad(new Vector3(-hw, 0, -hl), new Vector3(-hw, height, -hl), new Vector3(-hdw, height, -hl), new Vector3(-hdw, 0, -hl), new Vector2(1, 1));
                AddQuad(new Vector3(hdw, 0, -hl), new Vector3(hdw, height, -hl), new Vector3(hw, height, -hl), new Vector3(hw, 0, -hl), new Vector2(1, 1));
                if (height > doorHeight)
                    AddQuad(new Vector3(-hdw, doorHeight, -hl), new Vector3(-hdw, height, -hl), new Vector3(hdw, height, -hl), new Vector3(hdw, doorHeight, -hl), new Vector2(1, 1));
            }
            else
            {
                AddQuad(new Vector3(-hw, 0, -hl), new Vector3(-hw, height, -hl), new Vector3(hw, height, -hl), new Vector3(hw, 0, -hl), new Vector2(width / 4f, height / 4f));
            }

            // East Wall (+X)
            if (doorWall == DoorwayWall.East && doorWidth > 0f)
            {
                AddQuad(new Vector3(hw, 0, -hl), new Vector3(hw, height, -hl), new Vector3(hw, height, -hdw), new Vector3(hw, 0, -hdw), new Vector2(1, 1));
                AddQuad(new Vector3(hw, 0, hdw), new Vector3(hw, height, hdw), new Vector3(hw, height, hl), new Vector3(hw, 0, hl), new Vector2(1, 1));
                if (height > doorHeight)
                    AddQuad(new Vector3(hw, doorHeight, -hdw), new Vector3(hw, height, -hdw), new Vector3(hw, height, hdw), new Vector3(hw, doorHeight, hdw), new Vector2(1, 1));
            }
            else
            {
                AddQuad(new Vector3(hw, 0, -hl), new Vector3(hw, height, -hl), new Vector3(hw, height, hl), new Vector3(hw, 0, hl), new Vector2(length / 4f, height / 4f));
            }

            // West Wall (-X)
            if (doorWall == DoorwayWall.West && doorWidth > 0f)
            {
                AddQuad(new Vector3(-hw, 0, hl), new Vector3(-hw, height, hl), new Vector3(-hw, height, hdw), new Vector3(-hw, 0, hdw), new Vector2(1, 1));
                AddQuad(new Vector3(-hw, 0, -hdw), new Vector3(-hw, height, -hdw), new Vector3(-hw, height, -hl), new Vector3(-hw, 0, -hl), new Vector2(1, 1));
                if (height > doorHeight)
                    AddQuad(new Vector3(-hw, doorHeight, hdw), new Vector3(-hw, height, hdw), new Vector3(-hw, height, -hdw), new Vector3(-hw, doorHeight, -hdw), new Vector2(1, 1));
            }
            else
            {
                AddQuad(new Vector3(-hw, 0, hl), new Vector3(-hw, height, hl), new Vector3(-hw, height, -hl), new Vector3(-hw, 0, -hl), new Vector2(length / 4f, height / 4f));
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        // =========================================================================
        // 8. TAPERED / CONICAL SPINE ADAPTER CYLINDER
        // =========================================================================

        public static Mesh CreateCylinderMesh(float radius, float height, int segments)
        {
            return CreateTaperedCylinderMesh(radius, radius, height, segments);
        }

        public static Mesh CreateTaperedCylinderMesh(float bottomRadius, float topRadius, float height, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "TaperedSpineSection";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            float halfH = height * 0.5f;
            float step = (Mathf.PI * 2f) / segments;
            float slopeAngle = Mathf.Atan2(bottomRadius - topRadius, height);
            float cosSlope = Mathf.Cos(slopeAngle);
            float sinSlope = Mathf.Sin(slopeAngle);

            for (int i = 0; i <= segments; i++)
            {
                float a = i * step;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);

                Vector3 pBottom = new Vector3(cos * bottomRadius, -halfH, sin * bottomRadius);
                Vector3 pTop = new Vector3(cos * topRadius, halfH, sin * topRadius);

                vertices.Add(pBottom);
                vertices.Add(pTop);

                Vector3 n = new Vector3(cos * cosSlope, sinSlope, sin * cosSlope).normalized;
                normals.Add(n);
                normals.Add(n);

                float u = (float)i / segments * 4f;
                uvs.Add(new Vector2(u, 0));
                uvs.Add(new Vector2(u, 1));

                if (i < segments)
                {
                    int b = i * 2;
                    triangles.Add(b + 0);
                    triangles.Add(b + 1);
                    triangles.Add(b + 3);

                    triangles.Add(b + 0);
                    triangles.Add(b + 3);
                    triangles.Add(b + 2);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}
