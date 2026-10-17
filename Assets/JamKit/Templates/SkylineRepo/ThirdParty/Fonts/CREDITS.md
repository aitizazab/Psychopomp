# Skyline Repo: fonts

Display faces for the HUD and the comic lettering. Loaded with `Resources.Load<Font>` (so they sit under `Resources/SkylineFonts/`),
each with its licence beside it. Unity makes the .meta files; do not write them by hand.

| Face | Used for | Licence | Source |
|---|---|---|---|
| Anton Regular | kept for a later "Condensed" UI option (digits set closer together) | SIL Open Font License 1.1 (`Resources/SkylineFonts/Anton/OFL.txt`) | https://github.com/google/fonts/tree/main/ofl/anton (https://fonts.google.com/specimen/Anton) |
| Bangers Regular | comic lettering (C2) | SIL Open Font License 1.1 (`Resources/SkylineFonts/Bangers/OFL.txt`) | https://github.com/google/fonts/tree/main/ofl/bangers (https://fonts.google.com/specimen/Bangers) |
| Luckiest Guy Regular | the ammo digits (the creator's pick, 2026-10-14; `SkylineHudStyle.AmmoFontDefault`); comic lettering (C2); Hell's numbers and hint cards | Apache License 2.0 (`Resources/SkylineFonts/LuckiestGuy/LICENSE.txt`) | https://github.com/google/fonts/tree/main/apache/luckiestguy |
| Cinzel Decorative Black | Hell: the game's title only (`HellFaces.Title`; the creator's pick, 2026-10-18) | SIL Open Font License 1.1, Reserved Font Name "Cinzel" (`Resources/SkylineFonts/CinzelDecorative/OFL.txt`); Copyright (c) 2012 Natanael Gama | https://github.com/google/fonts/tree/main/ofl/cinzeldecorative |
| Metal Mania Regular | Hell: important subtext (banners, objective lines, job card titles, ability names, the decision prompt, menu items; `HellFaces.Heading`) | SIL Open Font License 1.1, Reserved Font Name "Metal Mania" (`Resources/SkylineFonts/MetalMania/OFL.txt`); Copyright (c) 2012 Open Window | https://github.com/google/fonts/tree/main/ofl/metalmania |
| Grenze Gotisch SemiBold | Hell: non-game text (settings and their descriptions, credits small print, stat labels, screen hints, control labels; `HellFaces.Fine`; replaced Barlow Condensed, the creator's call 2026-10-18) | SIL Open Font License 1.1 (`Resources/SkylineFonts/GrenzeGotisch/OFL.txt`); Copyright 2020 The Grenze Gotisch Project Authors | https://github.com/google/fonts/tree/main/ofl/grenzegotisch (the variable font instanced at weight 600 with fontTools for small-size readability; a Modified Version under the OFL, no Reserved Font Name) |

The Hell faces came from the art pass's download folder (`FableAnimationTests/art/hell-pass/art/downloads/google-fonts/`,
Google Fonts); each OFL.txt carries the copyright line read from the font file's own name table.

To try another ammo face in a still without editing code: launch with `-skyline-ammo-font Bangers`.
To change it for good: edit `SkylineHudStyle.AmmoFontDefault` (and add the face to `SkylineHudStyle.Faces`).
