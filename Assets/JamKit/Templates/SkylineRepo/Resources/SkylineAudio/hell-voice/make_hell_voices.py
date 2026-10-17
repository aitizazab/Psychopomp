# === HELL VOICES: placeholder voice lines for HellDescent, from docs/HELL-SCRIPT.md ===
#
#   C:/Users/User/tools/seed-vc/.venv/Scripts/python.exe <this file> [--page <review html path>] [--alts]
#
# Reads every table row of docs/HELL-SCRIPT.md shaped like
#   | `key` | GOD / VESPER / NARRATOR | trigger | line | ...
# and speaks the line with Kokoro-82M (local, Apache-2.0 voices; no real person cloned) on the CPU.
# Writes <this folder>/<speaker>/<key>.wav (24 kHz mono, levelled to -18 LUFS, peaks under -1 dBFS) and
# manifest.json (key -> speaker, line, Resources path, seconds). PLACEHOLDERS for timing only: the creator writes
# all dialogue and picks or records the final voices. Safe to re-run: it rewrites everything it owns.
#
# --page: also writes a listening page (no autoplay, volume 0.5) and copies the clips next to it.
# --alts: also renders a few lines per speaker in alternative voices for the page.

import json
import os
import re
import shutil
import sys

os.environ.setdefault("CUDA_VISIBLE_DEVICES", "")  # CPU: small model, and the GPU belongs to the main session

import numpy as np
import pyloudnorm as pyln
import soundfile as sf
from scipy.signal import fftconvolve

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, *[".."] * 7))
SCRIPT = os.path.join(REPO, "docs", "HELL-SCRIPT.md")
SR = 24000
TARGET_LUFS = -18.0
PEAK = 10 ** (-1.0 / 20)

# speaker -> Kokoro voice, speed, reverb (wet mix, seconds)
SPEAKERS = {
    "god": {"label": "GOD", "voice": "bf_isabella", "speed": 1.0, "wet": 0.10, "tail": 0.6},
    "vesper": {"label": "VESPER", "voice": "af_kore", "speed": 0.92, "wet": 0.22, "tail": 1.4},
    "narrator": {"label": "NARRATOR", "voice": "bm_george", "speed": 0.92, "wet": 0.06, "tail": 0.5},
}
ALTS = {"god": ["bf_emma", "af_nicole"], "vesper": ["af_river", "af_alloy"], "narrator": ["am_fenrir", "bm_fable"]}
ALT_KEYS = {"god": ["open-04", "job-viscount-brief", "end-favour-01"], "vesper": ["v2-02", "boss-mercy2", "dec-spare-01"],
            "narrator": ["tr-01", "tr-04", "tr-09"]}

ROW = re.compile(r"^\|\s*`([a-z0-9-]+)`\s*\|\s*(GOD|VESPER|NARRATOR)\s*\|\s*([^|]*)\|\s*([^|]*)\|")
SECTION = re.compile(r"^#{2,3}\s+(.*)")


def read_lines():
    rows, section, seen = [], "", set()
    with open(SCRIPT, encoding="utf-8") as f:
        for raw in f:
            m = SECTION.match(raw)
            if m:
                section = m.group(1).strip()
                continue
            m = ROW.match(raw)
            if not m:
                continue
            key, label, trigger, line = m.group(1), m.group(2), m.group(3).strip(), m.group(4).strip()
            if key in seen:
                raise SystemExit(f"duplicate key {key}")
            seen.add(key)
            text = line.replace("[PLACEHOLDER]", "").replace("*", "").strip()
            speaker = next(k for k, v in SPEAKERS.items() if v["label"] == label)
            rows.append({"key": key, "speaker": speaker, "section": section, "trigger": trigger, "text": text})
    return rows


def spoken(text):
    # What the TTS reads: shouting caps as words, ellipses as pauses.
    t = re.sub(r"\b([A-Z]{2,})\b", lambda m: m.group(1).capitalize(), text)
    return t.replace("...", "... ")


def reverb(x, wet, tail, seed=7):
    if wet <= 0:
        return x
    rng = np.random.default_rng(seed)
    n = int(tail * SR)
    ir = rng.standard_normal(n) * np.exp(-np.linspace(0, 7, n))
    ir[: int(0.012 * SR)] = 0  # a short pre-delay
    ir /= np.sqrt(np.sum(ir ** 2))
    pad = np.concatenate([x, np.zeros(n)])
    w = fftconvolve(pad, ir)[: len(pad)]
    return (1 - wet) * pad + wet * w


def level(x, meter):
    loud_idx = np.nonzero(np.abs(x) > 0.003 * np.max(np.abs(x)))[0]  # trim the reverb's silent tail
    x = x[: loud_idx[-1] + 1] if len(loud_idx) else x
    x = np.concatenate([np.zeros(int(0.08 * SR)), x, np.zeros(int(0.2 * SR))])
    probe = x if len(x) >= int(0.5 * SR) else np.concatenate([x, np.zeros(int(0.5 * SR) - len(x))])
    loud = meter.integrated_loudness(probe)
    x = x * 10 ** ((TARGET_LUFS - loud) / 20)
    peak = np.max(np.abs(x))
    if peak > PEAK:  # gentle: scale the loudest few down with a soft knee instead of the whole clip
        x = np.where(np.abs(x) > 0.7 * PEAK, np.sign(x) * (0.7 * PEAK + 0.3 * PEAK * np.tanh((np.abs(x) - 0.7 * PEAK) / (0.3 * PEAK))), x)
    return x.astype(np.float32), loud


def main():
    args = sys.argv[1:]
    page = args[args.index("--page") + 1] if "--page" in args else None
    alts = "--alts" in args

    from kokoro import KPipeline
    pipes = {}

    def say(text, voice, speed):
        lang = voice[0]
        if lang not in pipes:
            pipes[lang] = KPipeline(lang_code=lang, repo_id="hexgrad/Kokoro-82M")
        chunks = [np.asarray(a, dtype=np.float32) for _, _, a in pipes[lang](spoken(text), voice=voice, speed=speed)]
        return np.concatenate(chunks).astype(np.float64)

    meter = pyln.Meter(SR)
    rows = read_lines()
    for sp in SPEAKERS:
        d = os.path.join(HERE, sp)
        if os.path.isdir(d):
            for f in os.listdir(d):
                if f.endswith(".wav"):
                    os.remove(os.path.join(d, f))
        os.makedirs(d, exist_ok=True)

    for r in rows:
        cfg = SPEAKERS[r["speaker"]]
        x = reverb(say(r["text"], cfg["voice"], cfg["speed"]), cfg["wet"], cfg["tail"])
        x, _ = level(x, meter)
        path = os.path.join(HERE, r["speaker"], r["key"] + ".wav")
        sf.write(path, x, SR, subtype="PCM_16")
        r["seconds"] = round(len(x) / SR, 2)
        r["resource"] = f"SkylineAudio/hell-voice/{r['speaker']}/{r['key']}"
        print(f"{r['speaker']:9s} {r['key']:22s} {r['seconds']:5.2f}s  {r['text'][:60]}")

    manifest = {
        "about": "PLACEHOLDER voices for timing only (Kokoro-82M TTS). The creator writes all dialogue and picks the final voices. Source: docs/HELL-SCRIPT.md; rebuild with make_hell_voices.py.",
        "loudnessLufs": TARGET_LUFS,
        "sampleRate": SR,
        "speakers": {k: {"voice": v["voice"], "speed": v["speed"]} for k, v in SPEAKERS.items()},
        "lines": [{k: r[k] for k in ("key", "speaker", "text", "resource", "seconds", "trigger")} for r in rows],
    }
    with open(os.path.join(HERE, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=1, ensure_ascii=False)
    print(f"{len(rows)} lines, {sum(r['seconds'] for r in rows):.0f} s total")

    if not page:
        return
    out = os.path.dirname(os.path.abspath(page))
    clips = os.path.join(out, "hell-voices")
    if os.path.isdir(clips):
        shutil.rmtree(clips)
    for sp in SPEAKERS:
        shutil.copytree(os.path.join(HERE, sp), os.path.join(clips, sp), ignore=shutil.ignore_patterns("*.meta"))

    alt_rows = []
    if alts:
        os.makedirs(os.path.join(clips, "alts"), exist_ok=True)
        by_key = {r["key"]: r for r in rows}
        for sp, voices in ALTS.items():
            cfg = SPEAKERS[sp]
            for v in [cfg["voice"]] + voices:
                for k in ALT_KEYS[sp]:
                    if v == cfg["voice"]:
                        src = f"hell-voices/{sp}/{k}.wav"
                    else:
                        x = reverb(say(by_key[k]["text"], v, cfg["speed"]), cfg["wet"], cfg["tail"])
                        x, _ = level(x, meter)
                        src = f"hell-voices/alts/{sp}-{v}-{k}.wav"
                        sf.write(os.path.join(out, src), x, SR, subtype="PCM_16")
                    alt_rows.append((sp, v, k, by_key[k]["text"], src))
                print(f"alt {sp} {v}")

    write_page(page, rows, alt_rows)
    print("page", page)


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def write_page(page, rows, alt_rows):
    colour = {"god": "#7fd6c2", "vesper": "#b48cff", "narrator": "#e8b35a"}
    parts, section = [], None
    for r in rows:
        if r["section"] != section:
            section = r["section"]
            parts.append(f"<h2>{esc(section)}</h2>")
        c = colour[r["speaker"]]
        parts.append(
            f'<div class="row"><div class="who" style="color:{c}">{r["speaker"].upper()}</div>'
            f'<div class="txt"><code>{r["key"]}</code> <span class="t">{esc(r["text"])}</span>'
            f'<div class="trig">{esc(r["trigger"])} · {r["seconds"]:.1f} s</div></div>'
            f'<audio controls preload="none" src="hell-voices/{r["speaker"]}/{r["key"]}.wav"></audio></div>')
    alts_html = ""
    if alt_rows:
        alts_html = "<h2>Voice options (the same lines in other voices)</h2><p>First voice in each group is the one used above.</p>"
        cur = None
        for sp, v, k, text, src in alt_rows:
            if (sp, v) != cur:
                cur = (sp, v)
                alts_html += f'<h3 style="color:{colour[sp]}">{sp.upper()}: {v}</h3>'
            alts_html += (f'<div class="row"><div class="who"></div><div class="txt"><code>{k}</code> '
                          f'<span class="t">{esc(text)}</span></div><audio controls preload="none" src="{src}"></audio></div>')
    total = sum(r["seconds"] for r in rows)
    html = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Hell Voices</title><style>
:root{{--bg:#121014;--fg:#ece6df;--mut:#9a918a;--card:#1d191f;--line:#2e2830;--acc:#ff8a3d}}
body{{background:var(--bg);color:var(--fg);font:16px/1.45 system-ui,Segoe UI,sans-serif;margin:0;padding:24px 16px 80px;max-width:1100px;margin-inline:auto}}
h1{{color:var(--acc);margin:0 0 6px}} h2{{margin:34px 0 8px;border-bottom:1px solid var(--line);padding-bottom:4px}} h3{{margin:18px 0 4px}}
.q{{background:var(--card);border:1px solid var(--acc);border-radius:10px;padding:14px 18px;margin:16px 0}}
.q li{{margin:6px 0;font-size:18px}}
.row{{display:grid;grid-template-columns:90px 1fr 300px;gap:12px;align-items:center;padding:6px 0;border-bottom:1px solid var(--line)}}
.who{{font-weight:700;font-size:13px;letter-spacing:.05em}} code{{color:var(--mut);font-size:12px}} .trig{{color:var(--mut);font-size:13px}}
audio{{width:100%}} p{{color:var(--mut)}}
@media (max-width:700px){{.row{{grid-template-columns:1fr}}}}
</style></head><body>
<h1>HellDescent: placeholder voices</h1>
<p>Every voiced line from docs/HELL-SCRIPT.md, in play order ({len(rows)} lines, {total:.0f} s). Kokoro TTS on this PC, levelled to about -18 LUFS, players start at half volume, nothing autoplays. <b>All lines and voices are placeholders</b> for timing: you write the dialogue and pick or record the voices.</p>
<div class="q"><b>Questions for you</b><ol>
<li>Do these three voices fit, as stand-ins? The God of Death dry and amused, Vesper low and tired, the narrator deep. Options at the bottom.</li>
<li>Rewrite any line in docs/HELL-SCRIPT.md (the "Line" column); the voices rebuild from it in under a minute.</li>
<li>The title and the God of Death's name: the lists are in HELL-SCRIPT.md sections 13 and 14 (my picks: <b>Overdue</b>; <b>Morrow</b>).</li>
<li>Does the player speak when offering Vesper a hand (HELL-SCRIPT.md, open question 1)?</li>
</ol></div>
{''.join(parts)}
{alts_html}
<script>document.querySelectorAll('audio').forEach(a=>{{a.volume=0.5;a.autoplay=false;}});</script>
</body></html>"""
    with open(page, "w", encoding="utf-8") as f:
        f.write(html)


if __name__ == "__main__":
    main()
