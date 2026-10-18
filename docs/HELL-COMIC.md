# Hell: the comic flip-through (branch `hell-comic`)

Built 2026-10-18 early morning. The opening and every ending are now comic pages, not text cards (the creator: "Everything
which is text in the end-scene needs to be a comic flip-through instead"). It also fixes the ending's name running off the
screen (SPARED, AT A PRICE): it is now a splash panel whose words are sized to fit. Not run in Unity yet: the pure tests
and the Unity-side compile check pass.

## Files

| What | Where |
|---|---|
| The data, the layouts, the text fitting, the opening and ending sequences (`HellComic*`, `HellComics`) | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellComic.cs` |
| Tests (14) | `Packages/com.vibejam.jamkit/Tests/Skyline/Hell/HellComicTests.cs` |
| The player (UI Toolkit, its own document at sorting order 950) | `Assets/JamKit/Templates/SkylineRepo/Scripts/Hell/HellComicPlayer.cs` |
| The pictures | `Assets/JamKit/Templates/SkylineRepo/Resources/HellComic/opening/*.jpg`, `.../HellComic/endings/*.jpg` (1920x1080) |
| The import step | `tools/hell/import-comic.ps1` |

Edits elsewhere: `HellRunEvents` (the page events), `HellRunDirector` (the opening plays the comic: `Screens.Hide()`, then
`HellComicPlayer.Play(HellComics.Opening(), Flow.OpeningDone, true)`), `HellEndingDirector` (the cards phase plays
`HellComics.Ending(id)`, then the credits as before), `HellMusicDirector` (the opening's parts follow the pages),
`HellEndingCards` (its old title wraps now). **`HellScreens` is untouched** (its `ShowOpening` and cards still exist; the
shots tool uses them).

## The data format

A sequence is pages; a page is 1 to 4 panels; a panel is a picture and its lines:

```csharp
new HellComicSequence("opening")
    .Page(new HellComicPage(
        new HellComicPanel("opening/1-1", "Your sister's hand, still, on the sheet.")      // picture key, placeholder words
            .Say(HellComicLine.Caption("Your sister is dead.", "narrator", "open-01")),     // text, voice folder, voice key
        new HellComicPanel("opening/1-2", "You in the doorway, furious.", hold: 1.0f)       // hold: seconds after its last line
            .Say(HellComicLine.Caption("...", "narrator", "open-02")))
    { music = "sister" })                                                                   // the opening's music part
    .Page(new HellComicPage(
        new HellComicPanel("opening/2-1", "Baja in her hall.").Focus(0.5f, 0.4f)            // the crop's centre
            .Say(HellComicLine.Balloon("god", "BAJA", "Mortals usually...", "open-03"))));  // speaker, name, text, voice key
```

- **Picture:** `Resources/HellComic/<key>.jpg` (16:9). Missing: a dark crosshatched ink panel with the description, so
  the sequence still plays (logged `HELL_COMIC placeholder <key>`). `fallbackImage` is tried first.
- **Lines:** a caption (parchment box, top left) or a balloon (white, tail toward the panel's middle, the name over the
  words), in Metal Mania. Spots are automatic (`HellComicSpot`), or set per line. Voices load from
  `Resources/SkylineAudio/hell-voice/<speaker>/<key>` (god, vesper, narrator), at the Voices volume.
- **Splash:** a panel with `title` (the ending's name): the words big and fitted over the darkened picture.
- **Layouts:** 1 panel full page; 2 staggered (each 60% of the page, so the 16:9 art keeps its shape; the second laps
  over the first's lower right corner, and no line goes there); 3 a tall panel and two stacked; 4 a 2x2 grid.

## The player

- Each panel slides and scales in; its lines pop in one by one with their voices; the current panel's picture drifts
  slowly; pages turn with a flip (the old page folds away to the left over a shadow), with the card sound.
- **Next:** click, Enter, Space or A (after 0.25 s); or by itself when the voice ends (+0.6 s), or after reading time
  for a line with no voice. **Skip:** hold Escape, Backspace, B or Start for 0.6 s (the bar under the hint fills). The
  pause menu is blocked while it plays (Escape is the skip).
- **Fit:** every box is sized by `HellComicFit` (tested at 16:9 and 16:10, 720p to 4K), and the player shrinks a box
  further if it measures taller than its room.
- **Events:** `HellRunEvents.ComicPage(sequenceId, page)` on every page, `HellRunEvents.OpeningPage(page)` for the
  opening, `HellRunEvents.ComicFinished(id)`. Ids: `opening`, `ending-<ending id>`. `HellComicPlayer.Current.PageIndex`
  is public too.
- **Music:** the director maps the opening's pages to `baja_opening` parts: page 1 sister (already playing), page 2 hall
  (crossfade), page 3 the "wait" stinger (the hall stops dead) then the idea loop when it ends, page 4 idea. The fall on
  the hand-over to play is unchanged. The endings keep their one-shot cues (by ending id).
- Scripted runs (batch, demo) hold each step 0.3 s. The log shows `HELL_COMIC start/page/done/skipped`.

## The sequences

**Opening** (4 pages, the premise art the creator picked, cropped like the trailer's): 1 your sister's hand and you in
the doorway (narrator open-01, open-02); 2 Baja's hall (open-03); 3 the funniest idea (open-04, open-05); 4 the gate and
the fall (open-06). The words are `HellFlowText.Opening`'s placeholders.

**Endings** (kill, spare-kept, spare-cost, damned; the Lucifer forms add the lucifer pages): two staggered pages of the
ending's four panels (`<art>-1-1` .. `<art>-1-4`): the choice, the escape, Baja's two lines in balloons, the epilogue as a
caption; then the splash with `HellEndings.Title`. Baja's panel is the 4th for the kill (its 3rd is your sister home) and
the 3rd for the others (`HellComics.BajaPanel` / `EpiloguePanel`: change there if the art's order differs). The splash
uses `<art>-title` if it exists, else the epilogue's panel.

## The pictures for the endings

Files in `FableAnimationTests/art/incoming/`, imported with:

```
powershell -File tools/hell/import-comic.ps1            # the endings
powershell -File tools/hell/import-comic.ps1 -Opening   # also re-imports the opening's six
```

Names: `hell-end-<art>-1-<panel>[-takeN].jpg` (panel 1 to 4) and optionally `hell-end-<art>-title[-takeN].jpg`, `<art>`
= `kill`, `spare-kept`, `spare-cost`, `damned`, `lucifer`. The highest take wins. Output:
`Resources/HellComic/endings/<art>-1-<panel>.jpg`, center-cropped to 16:9 at 1920x1080 (`-Inset 0.03` trims paper edges
first). Unity makes the metas on import.

## Check in Unity

1. Title > Start: the opening comic (4 pages), the music changing on pages 2 and 3, then the floor card. Hold Escape:
   it skips to the floor card. Click/Enter/A moves on.
2. An ending (the Ending Test Bed: F9, F10/F11, run out): the comic over black, then the credits and the stats.
3. 16:10 and 4K windows: no caption, balloon or title runs off.
4. Missing art (e.g. spare-cost now): ink placeholders with the descriptions.

## What's left

- Nothing has been played in Unity. The flip is a 2D fold (UI Toolkit has no 3D page curl).
- The texture import settings are Unity's defaults (Unity resizes non-power-of-two Default textures to the nearest power
  of two in some settings; the player assumes 16:9 and lays the picture out itself, so it still looks right).
