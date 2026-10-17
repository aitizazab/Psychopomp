"""HellDescent's sound files (docs/HELL-SOUND-PASS.md, "Installed"): cut, layered, synthesised and levelled from the sound pass's
free picks, written to Assets/JamKit/Templates/SkylineRepo/Resources/SkylineAudio/hell/.

It also writes the credits from the same recipes (so a file can never ship without its credit):
  - Packages/com.vibejam.jamkit/Runtime/Skyline/HellSound/HellAudioCredits.Data.cs (pure C#: every source and which files use it)
  - Assets/JamKit/Templates/SkylineRepo/Resources/HellAudioCredits.txt (one line per source, for the credits screen)
  - CREDITS.md's audio section (between the HELL-AUDIO markers)

Sources: the sound pass's levelled pieces (FableAnimationTests/art/review/2026-10-17/hell-sound-pass/audio/<key>.mp3, cut from
the Freesound HQ previews; the same CC0 / CC-BY recordings as the originals) and their licences
(FableAnimationTests/art/hell-pass/sound/downloads/freesound/meta.json). Safe to re-run: it rewrites every output.

  uv run --with numpy --with scipy --with pyloudnorm --with soundfile python tools/audio/make-hell-sounds.py
"""
import json
import os
import re
import subprocess
import sys
from fractions import Fraction

import numpy as np
import pyloudnorm
import soundfile
from scipy import signal

SR = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
FABLE = os.environ.get("FABLE", os.path.join(REPO, "..", "FableAnimationTests"))
SRC = os.path.join(FABLE, "art", "review", "2026-10-17", "hell-sound-pass", "audio")
META = os.path.join(FABLE, "art", "hell-pass", "sound", "downloads", "freesound", "meta.json")
OUT = os.path.join(REPO, "Assets", "JamKit", "Templates", "SkylineRepo", "Resources", "SkylineAudio")
CREDITS_CS = os.path.join(REPO, "Packages", "com.vibejam.jamkit", "Runtime", "Skyline", "HellSound", "HellAudioCredits.Data.cs")
CREDITS_TXT = os.path.join(REPO, "Assets", "JamKit", "Templates", "SkylineRepo", "Resources", "HellAudioCredits.txt")
CREDITS_MD = os.path.join(REPO, "CREDITS.md")

# loudness targets (integrated LUFS of the file) per family; the game's bus then sets the level heard (SkylineMix)
ABILITY, ENEMY, WORLD, BED, UI = -22.0, -17.0, -20.0, -24.0, -24.0
PEAK = 10 ** (-1.0 / 20)   # -1 dBFS

_cache = {}


def load(key, stereo=False):
    k = (key, stereo)
    if k not in _cache:
        path = os.path.join(SRC, key + ".mp3")
        ch = 2 if stereo else 1
        raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-f", "f32le", "-ac", str(ch), "-ar", str(SR), "-"],
                             check=True, capture_output=True).stdout
        a = np.frombuffer(raw, dtype=np.float32).astype(np.float64)
        _cache[k] = a.reshape(-1, ch) if stereo else a
    return _cache[k]


def fades(x, fi=0.0, fo=0.0):
    x = x.copy()
    n = len(x)
    i = min(n, int(fi * SR))
    o = min(n, int(fo * SR))
    if i > 1:
        env = np.linspace(0, 1, i) ** 2
        x[:i] = (x[:i].T * env).T
    if o > 1:
        env = np.linspace(1, 0, o) ** 2
        x[n - o:] = (x[n - o:].T * env).T
    return x


def pitch(x, semis):
    if not semis:
        return x
    r = Fraction(2 ** (-semis / 12.0)).limit_denominator(200)   # tape-style: lower pitch = longer
    return signal.resample_poly(x, r.numerator, r.denominator, axis=0)


def filt(x, lp=None, hp=None):
    if lp:
        x = signal.sosfiltfilt(signal.butter(4, lp, "low", fs=SR, output="sos"), x, axis=0)
    if hp:
        x = signal.sosfiltfilt(signal.butter(2, hp, "high", fs=SR, output="sos"), x, axis=0)
    return x


class L:
    """A layer cut from a source piece: [a, b) seconds, placed at `at`, gain, pitch, reverse, fades, filters."""

    def __init__(self, key, a=0.0, b=None, at=0.0, db=0.0, semis=0, rev=False, fi=0.0, fo=0.0, lp=None, hp=None, stereo=False):
        self.key, self.a, self.b, self.at, self.db, self.semis = key, a, b, at, db, semis
        self.rev, self.fi, self.fo, self.lp, self.hp, self.stereo = rev, fi, fo, lp, hp, stereo

    @property
    def source(self):
        return self.key.split("__", 1)[1]

    def render(self):
        x = load(self.key, self.stereo)
        a = int(self.a * SR)
        b = len(x) if self.b is None else min(len(x), int(self.b * SR))
        x = x[a:b]
        if self.rev:
            x = x[::-1]
        x = pitch(x, self.semis)
        x = filt(x, self.lp, self.hp)
        x = fades(x, self.fi, self.fo)
        return x * 10 ** (self.db / 20.0)


class Synth:
    """A synthesised layer: fn(n samples) -> signal, placed at `at`."""
    source = "synth"

    def __init__(self, seconds, fn, at=0.0, db=0.0):
        self.seconds, self.fn, self.at, self.db = seconds, fn, at, db

    def render(self):
        return self.fn(int(self.seconds * SR)) * 10 ** (self.db / 20.0)


def mix(layers, stereo=False):
    parts = [(int(l.at * SR), l.render()) for l in layers]
    n = max(o + len(p) for o, p in parts)
    out = np.zeros((n, 2) if stereo else n)
    for o, p in parts:
        if stereo and p.ndim == 1:
            p = np.stack([p, p], axis=1)
        out[o:o + len(p)] += p
    return out


def loopify(x, xfade):
    """A seamless loop: the tail crossfaded (equal power) over the head."""
    c = int(xfade * SR)
    body = x[:len(x) - c].copy()
    t = np.linspace(0, np.pi / 2, c)
    head_in, tail_out = np.sin(t), np.cos(t)
    if x.ndim == 2:
        head_in, tail_out = head_in[:, None], tail_out[:, None]
    body[:c] = body[:c] * head_in + x[len(x) - c:] * tail_out
    return body


def level(x, lufs):
    meter = pyloudnorm.Meter(SR)
    probe = x
    if len(probe) < int(0.5 * SR):   # the meter needs 400 ms blocks: measure a short sound padded with itself-free silence
        pad = np.zeros((int(0.5 * SR) - len(x),) + x.shape[1:])
        probe = np.concatenate([x, pad])
    now = meter.integrated_loudness(probe)
    if not np.isfinite(now):
        now = 20 * np.log10(max(1e-9, np.sqrt(np.mean(x ** 2))))
    y = x * 10 ** ((lufs - now) / 20.0)
    for _ in range(2):   # a peak limiter (5 ms look-ahead, at most MaxReduceDb), then measure again and top up once
        y = limit(y)
        probe = y if len(y) >= SR // 2 else np.concatenate([y, np.zeros((SR // 2 - len(y),) + y.shape[1:])])
        got = meter.integrated_loudness(probe)
        if not np.isfinite(got) or got >= lufs - 0.3:
            break
        y = y * 10 ** ((lufs - got) / 20.0)
    y = limit(y)
    peak = np.max(np.abs(y))
    if peak > PEAK:
        y *= PEAK / peak   # never above -1 dBFS (past the limiter's reach it lands quieter than its target: see the build log)
    tp = np.max(np.abs(signal.resample_poly(y, 4, 1, axis=0)))   # and no true peak (between the samples) over -1 dBTP either
    if tp > PEAK:
        y *= PEAK / tp
    return y, now


MAX_REDUCE_DB = 8.0


def limit(y):
    from scipy.ndimage import maximum_filter1d, minimum_filter1d
    mag = np.abs(y) if y.ndim == 1 else np.max(np.abs(y), axis=1)
    w = max(3, int(0.005 * SR))
    env = maximum_filter1d(mag, size=w)
    floor = 10 ** (-MAX_REDUCE_DB / 20.0)
    g = np.clip(PEAK / np.maximum(env, 1e-12), floor, 1.0)
    g = minimum_filter1d(g, size=w)
    k = np.hanning(w)
    g = np.convolve(g, k / k.sum(), mode="same")
    return (y.T * g).T


def trimmed(x, seconds):
    return x[:int(seconds * SR)] if seconds else x


# ---------------------------------------------------------------------------------------------------- synthesis

rng = np.random.default_rng(1666)


def t_of(n):
    return np.arange(n) / SR


def scythe_warn(n):
    """0.9 s: a thin tone rising an octave (440 -> 880 Hz), its tremolo speeding up 8 -> 30 Hz, louder to the end, hard stop."""
    t = t_of(n)
    k = t / t[-1]
    f = 440 * 2 ** k
    ph = 2 * np.pi * np.cumsum(f) / SR
    saw = 2 * ((ph / (2 * np.pi)) % 1.0) - 1
    tone = 0.55 * np.sin(ph) + 0.25 * saw + 0.2 * np.sin(2 * ph)
    trem_rate = 8 + 22 * k ** 2
    trem = 0.6 + 0.4 * np.sin(2 * np.pi * np.cumsum(trem_rate) / SR)
    env = (0.25 + 0.75 * k ** 1.5) * trem
    y = filt(tone * env, hp=300)
    return fades(y, 0.02, 0.003)


def sub_swirl(n):
    """The Rewind's chest: a sub sine falling 75 -> 38 Hz with a 7 Hz wobble, swelling then gone."""
    t = t_of(n)
    k = t / t[-1]
    f = 75 * (38 / 75) ** k * (1 + 0.06 * np.sin(2 * np.pi * 7 * t))
    y = np.sin(2 * np.pi * np.cumsum(f) / SR)
    return y * np.sin(np.pi * k) ** 0.7


def thump(n, hz=60):
    t = t_of(n)
    return np.sin(2 * np.pi * hz * t * (1 - 0.3 * t / t[-1])) * np.exp(-t / 0.04)


def inrush(n):
    """A dark swirl of air rushing in: low-passed noise, its level and brightness rising (a reverse swell)."""
    t = t_of(n)
    k = t / t[-1]
    x = rng.standard_normal(n)
    lo = filt(x, lp=500)
    hi = filt(x, lp=1600, hp=500)
    return (lo + hi * k) * k ** 2.2


def whir(n, rate=12.0):
    """A spinning blade: band-passed noise pulsed at `rate` Hz."""
    t = t_of(n)
    x = filt(rng.standard_normal(n), lp=1800, hp=350)
    return x * (0.5 + 0.5 * np.sin(2 * np.pi * rate * t)) ** 2


def click(n):
    """A soft UI tick: a 1.3 kHz ping with a breath of noise, gone in 40 ms."""
    t = t_of(n)
    ping = np.sin(2 * np.pi * 1300 * t) * np.exp(-t / 0.012)
    noise = filt(rng.standard_normal(n), lp=4000, hp=1500) * np.exp(-t / 0.004) * 0.3
    return ping + noise


def wind(n):
    """The shaft's wind: band-passed noise with a slow gusting brightness; looped by the build."""
    t = t_of(n)
    x = rng.standard_normal(n)
    low = filt(x, lp=700, hp=120)
    high = filt(x, lp=2600, hp=700)
    gust = 0.5 + 0.5 * np.sin(2 * np.pi * 0.37 * t) * np.sin(2 * np.pi * 0.11 * t + 1.0)
    return low + 0.6 * high * gust


def peak_cut(key, length, pre=0.03, nth=0, semis=0, fo=None):
    """The `nth` loudest short event in a piece, `length` seconds from `pre` before its peak (single bubbles, single pops)."""
    x = load(key)
    w = int(0.03 * SR)
    e = np.convolve(x ** 2, np.ones(w) / w, mode="same")
    picks, guard = [], int(length * SR)
    for i in np.argsort(e)[::-1]:
        if all(abs(i - p) > guard for p in picks):
            picks.append(i)
        if len(picks) > nth:
            break
    c = picks[nth]
    a = max(0, c - int(pre * SR)) / SR
    return L(key, a, a + length, semis=semis, fi=0.004, fo=fo if fo is not None else length * 0.6)


# ---------------------------------------------------------------------------------------------------- the recipes
# (file under SkylineAudio, layers, loudness target, options). The cue -> file table is HellSoundTable.cs; these are the files.

R = []


def out(file, layers, lufs, loop=0.0, length=0.0, fi=0.0, fo=0.0, stereo=False):
    R.append(dict(file=file, layers=layers, lufs=lufs, loop=loop, length=length, fi=fi, fo=fo, stereo=stereo))


A, E, W, B, U = "hell/abilities/", "hell/", "hell/world/", "hell/beds/", "hell/ui/"

# --- abilities (the player's own: the Abilities bus, at the guns' level)
out(A + "shadowstep_aim", [L("shadowstep_aim__427292", 1.0, 5.0, lp=2500)], -34, loop=0.5)
_step_out = [L("shadowstep_out__446010", 0.30, 0.62, fi=0.03), L("shadowstep_out__428336", 0.07, 0.40, at=0.26, db=-4)]
out(A + "shadowstep_out", _step_out, ABILITY, fo=0.12)
out(A + "shadowstep_out_free", [L(l.key, l.a, l.b, at=l.at * 0.8, db=l.db, semis=4, fi=l.fi) for l in _step_out]
    + [L("umbral__802472", 0.0, 0.24, db=-6)], ABILITY, fo=0.1)
out(A + "shadowstep_in", [L("shadowstep_in__714257", 0.0, 0.5), L("shadowstep_in__504626", 0.28, 0.6, semis=-3, db=-3, fi=0.005)],
    ABILITY, fo=0.15)
out(A + "denied", [L("ability_denied__133449", 0.0, 0.45, hp=300)], -24, fo=0.15)
out(A + "rewind", [L("rewind_end__74385", 0.0, 0.35, db=-3), L("rewind_start__280222", 1.9, 3.0, at=0.2),
                   Synth(1.2, sub_swirl, at=0.05, db=-4), L("rewind_start__23127", 0.3, 1.13, at=0.4, db=-8)], -20, fo=0.08)
out(A + "rewind_end", [L("rewind_start__23127", 0.85, 1.13, db=-6, fi=0.1), L("rewind_end__188033", 0.0, 0.14, at=0.26, db=2),
                       Synth(0.15, thump, at=0.26, db=-6)], ABILITY, fo=0.05)
out(A + "dilation_in", [L("dilation_in__366092", 0.6, 2.2, fi=0.05, fo=0.4)], ABILITY)
out(A + "dilation_heartbeat", [L("dilation_in__22416", 0.0, 3.6, lp=900)], -28, loop=0.05)
out(A + "dilation_out", [L("dilation_out__123438", 0.5, 1.86, fi=0.05, fo=0.1)], ABILITY)
out(A + "ambush", [L("ambush__512389", 0.05, 1.3, fo=0.6), L("shade__234044", 0.5, 1.2, at=0.1, db=-10, fi=0.1, fo=0.4)], -20)
# the creator's pick (2026-10-18): "Breath of Death" (60571, CC BY 4.0). Its 1.4 s fade-up is cut: the decoy appears at once, so the
# breath starts 1.6 s in (15 dB under its peak) and dies away by 3.6 s
out(A + "shade", [L("shade__60571", 1.6, 3.6, fi=0.12, fo=0.8)], -24)
out(A + "nightfall", [Synth(0.6, inrush, db=-4), L("nightfall__657541", 0.3, 2.8, at=0.1, fi=0.05, fo=0.8),
                      L("nightfall__189779", 0.0, 2.5, at=0.5, lp=200, fo=1.5, db=-2)], -20)
out(A + "borrowed_time", [L("borrowed_time__211203", 0.0, 0.26, fo=0.08)], -26)
out(A + "lava_hop", [L("lava_hop__234782", 0.15, 0.8, fi=0.01, fo=0.3)], -24)
out(A + "lava_walk", [L("lava_hop__155660", 0.01, 0.35, fo=0.15, hp=200)], -28)
out(A + "combustion", [L("combustion__431174", 0.0, 0.9, fo=0.3)], -20)

# --- the UI moments (the Feedback bus)
out(U + "skill_open", [L("skill_pick__462879", 0.17, 2.4, fi=0.2, fo=1.0), L("nightfall__657541", 0.3, 2.0, db=-8, fo=0.8)], -26)
out(U + "skill_hover", [Synth(0.06, click)], -30)
out(U + "skill_pick", [L("skill_pick__199940", 0.03, 1.2, fo=0.5)], UI)
out(U + "death", [L("shadowstep_out__181889", 0.43, 0.75, db=-6, fi=0.05), L("death__430978", 0.0, 3.0, at=0.15, fo=1.5)], -20)
out(U + "room_cleared", [L("room_cleared__76405", 0.0, 4.38, fo=1.5)], -22)

# --- the fire imp
out(E + "fire-imp/hover_loop", [L("imp_hover__71417", 0.0, 3.0), L("imp_hover__650574", 2.0, 5.0, db=4)], -24, loop=0.3)
out(E + "fire-imp/telegraph", [L("imp_telegraph__172631", 0.0, 0.45, fi=0.1), L("imp_telegraph__534702", 0.0, 0.45, db=-3, fi=0.3)],
    -18, fo=0.05)
out(E + "fire-imp/attack", [L("imp_attack__267887", 0.0, 0.6, fo=0.25)], ENEMY)
out(E + "fire-imp/bolt_hit", [L("imp_bolt_hit__539972", 0.0, 0.55, fo=0.25)], -19)
out(E + "fire-imp/alert", [L("imp_voice__202100", 0.0, 1.0, fo=0.35)], -18)
out(E + "fire-imp/hurt", [L("imp_voice__442815", 0.0, 0.4, semis=3, fo=0.15)], -19)
out(E + "fire-imp/wreck", [L("imp_wreck__249813", 0.47, 0.87, fo=0.08), L("imp_wreck__431174", 0.0, 0.78, at=0.32),
                           L("imp_hover__650574", 2.0, 3.0, at=0.6, db=-8, fo=0.4)], -16)
out(E + "fire-imp/plate_shed", [L("imp_plate__144159", 0.12, 0.62, fo=0.15)], -19)

# --- the scythe demon and its scythes
out(E + "scythe-demon/alert", [L("demon_alert__222523", 0.25, 2.6, fo=0.8)], ENEMY)
out(E + "scythe-demon/hurt", [L("demon_alert__444445", 0.28, 0.9, fo=0.3)], -18)
out(E + "scythe-demon/telegraph", [L("demon_telegraph__581594", 0.27, 0.95, fo=0.3)], -18)
out(E + "scythe-demon/attack", [L("scythe_appear__657653", 0.11, 0.7, fo=0.3), L("demon_telegraph__107589", 0.1, 0.6, semis=-5, db=-3),
                                Synth(0.6, whir, db=-6)], -18, fo=0.25)
# made the way the sound pass describes (the creator's pick, 2026-10-18): the heavy body fall's impact, its first 0.25 s from the
# hit (onset 0.36 s; the old cut started 80 ms early, in the scuff), pitched down 4 semitones, with a small stone-debris tail
out(E + "scythe-demon/footstep", [L("demon_stride__504626", 0.355, 0.605, semis=-4, fi=0.002, fo=0.1),
                                  L("ooze_plate__262118", 0.01, 0.3, at=0.12, db=-12, fi=0.01, fo=0.2)], -18)
out(E + "scythe-demon/plate_shed", [L("demon_plate__616492", 0.0, 0.41), L("demon_plate__530486", 0.04, 0.35, db=-4, fo=0.2)], -18)
# the creator's pick (2026-10-18): the first part of "Dragon death" (366096): the roar from its rise (0.08 s) to just before its
# second breath (1.44 s), faded out over the last 0.5 s, with the fireball low under it (the death is in a burst of fire)
out(E + "scythe-demon/wreck", [L("demon_wreck__366096", 0.08, 1.42, fo=0.5), L("combustion__431174", 0.0, 0.78, at=0.05, db=-8)], -16)
out(E + "scythe/warn", [Synth(0.9, scythe_warn)], -16, length=0.9)
out(E + "scythe/beam", [L("scythe_beam__369516", 0.5, 1.5, fi=0.004, fo=0.35), L("scythe_beam__447941", 0.15, 1.0, db=-4, fo=0.3)], -16)

# --- the lava ooze and its pools
out(E + "lava-ooze/throb", [peak_cut("ooze_pulse__456806", 0.25)], -20)
out(E + "lava-ooze/alert", [peak_cut("ooze_pulse__456806", 0.4, nth=1, semis=-5)], -19)
out(E + "lava-ooze/telegraph", [L("ooze_pulse__352904", 1.5, 2.3, fi=0.2, semis=3)], -18, fo=0.1)
out(E + "lava-ooze/attack", [L("ooze_attack__414296", 0.0, 0.9, fo=0.4), L("ooze_attack__445109", 0.01, 0.33)], -16)
out(E + "lava-ooze/hurt", [L("ooze_attack__445109", 0.01, 0.33, semis=2)], -19)
out(E + "lava-ooze/wreck", [L("ooze_wreck__167074", 0.01, 0.53), L("ooze_wreck__155660", 0.05, 1.0, at=0.25, db=-4, fo=0.5)], ENEMY)
out(E + "lava-ooze/plate_shed", [L("ooze_plate__321478", 0.0, 0.5, fo=0.2)], -18)
out(E + "lava-ooze/pool_loop", [L("lava_pool__382313", 0.5, 3.5)], -24, loop=0.4)
out(E + "lava-ooze/pool_cool", [L("lava_pool__365071", 0.1, 1.6, fi=0.05, fo=1.0)], -24)
out(E + "spawn/warning", [L("spawn_warning__186374", 1.0, 2.1, fi=0.3, fo=0.2), L("scythe_appear__657653", 0.11, 1.1, db=-6, fi=0.4)], ENEMY)

# --- the world: lava, platforms, the shaft, braziers
for i in range(3):
    out(W + "lava_bubble_%d" % (i + 1), [peak_cut("lava_bubble__145036", 0.35, nth=i)], -22)
out(W + "lava_touch", [L("lava_touch__234782", 0.15, 0.7, fi=0.005, fo=0.25), L("lava_hop__155660", 0.01, 0.3, db=-4)], -19)
out(W + "sink", [L("lava_sink__315931", 0.0, 0.59, semis=-3), L("death__430978", 0.0, 1.2, db=-6, semis=-5, fo=0.6),
                 L("lava_touch__234782", 0.2, 1.2, at=0.3, db=-6, fo=0.5)], -18)
out(W + "platform_shake", [L("collapse_shake__347149", 0.05, 0.69, fo=0.1), L("collapse_shake__347149", 0.05, 0.6, at=0.4, fi=0.05),
                           L("collapse_shake__222521", 0.5, 1.4, lp=400, db=2, fi=0.6)], -19, length=0.9, fo=0.05)
out(W + "platform_fall", [L("ooze_plate__321478", 0.0, 0.3, db=-2), L("collapse_fall__567249", 0.0, 2.6, at=0.05, fo=1.2)], -18)
out(W + "fire_loop", [L("torch__186924", 1.0, 4.0)], -24, loop=0.4)
out(W + "shaft_wind", [Synth(6.5, wind)], -22, loop=0.5)
out(W + "shaft_start", [L("shaft__697432", 0.5, 3.5, fi=0.3, fo=1.0)], WORLD)
out(W + "shaft_land", [L("shadowstep_in__504626", 0.28, 0.9, semis=-2), L("nightfall__189779", 0.0, 1.0, lp=150, fo=0.6, db=-4)], -18)

# --- the objectives
out(W + "sigil", [L("sigil__18654", 0.0, 3.5, fo=1.5), L("spawn_warning__186374", 1.0, 2.0, db=-6, fi=0.02, fo=0.6)], -19)
out(W + "lantern_pickup", [L("lantern_pick__171939", 0.0, 0.33), L("shade__234044", 1.0, 1.6, at=0.1, db=-10, fi=0.1, fo=0.3)], -21)
out(W + "lantern_carry", [L("lantern_loop__808032", 1.0, 7.0, lp=4000)], -30, loop=1.0)
out(W + "lantern_place", [L("lantern_place__61521", 0.4, 4.4, fo=2.0)], WORLD)
# the creator's pick (2026-10-18): the "Metal impact" (530486) alone, from its hit (0.063 s; 30 ms of handling noise before it cut) with its ring cut to 0.55 s
out(W + "anchor_hit", [L("demon_plate__530486", 0.063, 0.63, fi=0.002, fo=0.3)], -18)
out(W + "anchor_deflect", [L("anchor_deflect__570853", 0.02, 0.9, fo=0.4)], WORLD)
out(W + "anchor_break", [L("anchor_break__703247", 0.0, 2.5, fo=1.2), L("anchor_break__387240", 0.08, 1.07, at=0.15, db=-3)], -17)
out(W + "brand_charge", [L("brand_charge__407052", 4.0, 7.0)], -24, loop=0.5)
out(W + "brand_done", [L("brand_done__854670", 0.09, 3.0, fo=1.2)], -19)
# the creator's pick (2026-10-18): "Heavy Stone Push" (558254) for the sealing and the closing. The grind loop is the push's
# body (0.10-0.78 s) twice, the second a semitone lower so the repeat is less obvious, crossfaded into a seamless loop; the sealed
# one-shot is the whole push to its stop (0.04-0.86 s: the lid lands) and the steam hiss of the dying lava after it
out(W + "well_grind", [L("well_seal__558254", 0.10, 0.78), L("well_seal__558254", 0.10, 0.78, at=0.55, semis=-1, fi=0.12)], -24, loop=0.12)
out(W + "well_sealed", [L("well_seal__558254", 0.04, 0.86, fi=0.01, fo=0.06), L("well_seal__365071", 0.1, 1.6, at=0.75, db=-6, fi=0.05, fo=0.9)], -19)
out(W + "gate_open", [L("gate_open__352829", 0.19, 3.5, fo=1.5)], -19)

# --- the beds (2D, stereo; the Ambience bus): one per floor, the lava sea under each
_lava = L("lava_bed__474852", 0.0, 10.0, db=-4, stereo=True)
out(B + "ember_gate", [L("room_ember__397092", 0.0, 10.0, stereo=True), _lava], BED, loop=1.0, stereo=True)
out(B + "molten_works", [L("room_molten__393398", 0.0, 10.0, stereo=True), L("room_molten__108265", 0.78, 7.14, db=-8, stereo=True), _lava],
    BED, loop=1.0, stereo=True)
out(B + "black_glass", [L("room_glass__155142", 0.0, 10.0, stereo=True), L("lava_bed__474852", 0.0, 10.0, db=-8, stereo=True)],
    BED, loop=1.0, stereo=True)


# ---------------------------------------------------------------------------------------------------- build

def build():
    log = []
    for r in R:
        x = mix(r["layers"], r["stereo"])
        x = trimmed(x, r["length"])
        if r["loop"]:
            x = loopify(x, r["loop"])
        else:
            x = fades(x, r["fi"], max(r["fo"], 0.005))
        y, before = level(x, r["lufs"])
        path = os.path.join(OUT, r["file"].replace("/", os.sep) + ".wav")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        soundfile.write(path, y.astype(np.float32), SR, subtype="PCM_16")
        meter = pyloudnorm.Meter(SR)
        probe = y if len(y) >= SR // 2 else np.concatenate([y, np.zeros((SR // 2 - len(y),) + y.shape[1:])])
        got = meter.integrated_loudness(probe)
        log.append("%-34s %5.2f s  target %5.1f  got %6.1f LUFS  peak %5.1f dBFS%s" % (
            r["file"], len(y) / SR, r["lufs"], got, 20 * np.log10(np.max(np.abs(y)) + 1e-12), "  loop" if r["loop"] else ""))
    return log


def credits():
    meta = json.load(open(META, encoding="utf-8"))
    by_source, files = {}, []
    for r in R:
        ids = []
        for l in r["layers"]:
            s = l.source
            if s not in ids:
                ids.append(s)
        files.append((r["file"], ids))
        for s in ids:
            by_source.setdefault(s, []).append(r["file"])
    sources = []
    for s in sorted(by_source, key=lambda k: (k != "synth", int(k) if k.isdigit() else 0)):
        if s == "synth":
            sources.append(dict(id="synth", title="Synthesised layers", author="HellDescent (made for this game)",
                                url="", licence="CC BY 4.0 (this repo's assets)",
                                credit="Synthesised layers made for HellDescent (tools/audio/make-hell-sounds.py)"))
            continue
        m = meta[s]
        title = re.sub(r"\.(wav|aif|aiff|mp3|flac|ogg)$", "", m["title"], flags=re.I)
        lic = m["licence"]
        if lic.startswith("CC0"):
            credit = '"%s" by %s (%s), %s' % (title, m["user"], m["url"], lic)
        else:
            credit = '"%s" by %s (%s) licensed under %s' % (title, m["user"], m["url"], lic)
        sources.append(dict(id=s, title=title, author=m["user"], url=m["url"], licence=lic, credit=credit))

    def cs(s):
        return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'

    lines = ["// GENERATED by tools/audio/make-hell-sounds.py from the recipes and the sound pass's meta.json. Do not edit by hand:",
             "// re-run the script. The hand-written half (the lookups) is HellAudioCredits.cs.",
             "namespace JamKit.Skyline", "{", "    public static partial class HellAudioCredits", "    {",
             "        /// <summary>Every recording (and the synthesised layers) in the shipped Hell sounds, with its credit line.</summary>",
             "        public static readonly HellAudioSource[] Sources =", "        {"]
    for s in sources:
        lines.append("            new HellAudioSource(%s, %s, %s, %s, %s, %s)," % tuple(cs(s[k]) for k in ("id", "title", "author", "url", "licence", "credit")))
    lines += ["        };", "",
              "        /// <summary>Each shipped file (its path under Resources/SkylineAudio, no extension) and the sources layered into it.</summary>",
              "        public static readonly string[][] FileSources =", "        {"]
    for f, ids in files:
        lines.append("            new[] { %s }," % ", ".join(cs(v) for v in [f] + ids))
    lines += ["        };", "    }", "}", ""]
    os.makedirs(os.path.dirname(CREDITS_CS), exist_ok=True)
    open(CREDITS_CS, "w", encoding="utf-8", newline="\n").write("\n".join(lines))

    txt = ["# HellDescent sound credits (generated by tools/audio/make-hell-sounds.py). One line per source:",
           "# licence <TAB> credit line. CC BY lines must be shown; CC0 lines are shown as thanks."]
    for s in sources:
        txt.append("%s\t%s" % (s["licence"], s["credit"]))
    open(CREDITS_TXT, "w", encoding="utf-8", newline="\n").write("\n".join(txt) + "\n")

    md = ["<!-- HELL-AUDIO:BEGIN (generated by tools/audio/make-hell-sounds.py; edit the script, not this section) -->",
          "## Audio: HellDescent's sound effects", "",
          "Every Hell sound is cut, layered or synthesised from these recordings (Freesound; read from each sound's page on",
          "2026-10-17). The files are built by `tools/audio/make-hell-sounds.py`; which recordings each file uses is in",
          "`Packages/com.vibejam.jamkit/Runtime/Skyline/HellSound/HellAudioCredits.Data.cs`. The credits screen reads",
          "`Assets/JamKit/Templates/SkylineRepo/Resources/HellAudioCredits.txt` (or `JamKit.Skyline.HellAudioCredits`).", "",
          "| Title | Author | Licence | Source | Used in |", "|---|---|---|---|---|"]
    for s in sources:
        used = ", ".join("`%s`" % f.replace("hell/", "") for f in by_source[s["id"]])
        md.append("| %s | %s | %s | %s | %s |" % (s["title"], s["author"], s["licence"], s["url"] or "-", used))
    md += ["", "Credit lines (CC BY: required; CC0: thanks):", ""]
    for s in sources:
        md.append("- " + s["credit"])
    md += ["", "Music: none yet (the creator composes it with us).", "<!-- HELL-AUDIO:END -->"]
    block = "\n".join(md)
    head = "# Credits\n\nThird-party content in this repo and its licences.\n\n"
    text = open(CREDITS_MD, encoding="utf-8").read() if os.path.exists(CREDITS_MD) else head
    if "<!-- HELL-AUDIO:BEGIN" in text:
        text = re.sub(r"<!-- HELL-AUDIO:BEGIN.*?HELL-AUDIO:END -->", lambda _: block, text, flags=re.S)
    else:
        text = text.rstrip("\n") + "\n\n" + block + "\n"
    open(CREDITS_MD, "w", encoding="utf-8", newline="\n").write(text)
    return sources


if __name__ == "__main__":
    for line in build():
        print(line)
    src = credits()
    print("%d files, %d sources (%d CC BY)" % (len(R), len(src), sum(1 for s in src if s["licence"].startswith("CC BY") and s["id"] != "synth")))
    sys.exit(0)
