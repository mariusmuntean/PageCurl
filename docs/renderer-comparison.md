# Renderer choice

Assessment on 2026-10-04: keep PageCurl's Skia renderer. Simple3D-Maui is useful
for small interactive 3D diagrams, but is not a replacement for this page
renderer in its current form.

| Requirement | PageCurl | Simple3D-Maui |
| --- | --- | --- |
| Page images and MAUI snapshots | Front/back textures | No texture mapping |
| Transparent composition over live pages | Supported | Opaque triangles only |
| Page interaction and release settling | Dedicated controller | Orbit, zoom and selection |
| Paper constraints and anchored curls | Cylinder/cone geometry | Generic indexed meshes |
| Depth visibility | Ordered page surfaces | Software depth buffer |
| Lighting | Smooth per-vertex underside light | Directional flat lighting |

The [Simple3D README](https://github.com/mariusmuntean/Simple3D-Maui#documentation)
explicitly lists no texture mapping, transparency, or shadows. Its depth buffer
would help with arbitrary intersecting 3D geometry, but PageCurl's current
surfaces do not need that general pipeline. Moving page drawing into Simple3D
would first require UV textures, two-sided materials and transparency, and
would retain PageCurl's interaction, snapshots and paper math.

There is no evidence that this migration would be faster. Simple3D rasterizes
triangles in software, whereas PageCurl submits textured triangles through
Skia's GL adapter. Comparable device measurements would be required after the
missing features were implemented. The current choice avoids those additional
features and dependencies while preserving the platform-neutral core's ability
to support a different renderer later.

The relevant improvements are local: full cross-spine travel, immediate
lifecycle cancellation, stable snapshot ownership, readable back-face UVs,
coalesced capture requests, and explicit disposal of Skia's native vertex copies.
