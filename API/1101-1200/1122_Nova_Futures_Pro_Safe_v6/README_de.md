# Nova Futures PRO SAFE v6 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert Trend-, Volatilitäts- und Struktursignale. Sie verwendet einen 200er EMA mit ADX zur Trendbestätigung, Bollinger Bands gegenüber Keltner Channels zur Erkennung von Squeeze-Ausbrüchen und Donchian-Levels für Strukturbrüche bei Hochs oder Tiefs. Optionale Higher-Timeframe-Filter und ein Choppiness-Index vermeiden den Handel in Phasen geringer Qualität. Eine Abkühlungsphase verhindert unmittelbaren Wiedereinstieg nach Positionsschließung.

## Eingaben
- **EMA Length** — Länge der Basis-Exponentialglättung
- **DMI Length** — Periode für ADX und direktionale Bewegung
- **Min ADX** — Mindest-ADX-Wert zur Trendbeurteilung
- **BB Length** — Bollinger-Bands-Periode
- **BB Mult** — Bollinger-Bands-Multiplikator
- **KC Length** — Keltner-Channels-Periode
- **KC Mult** — Keltner-Channels-Multiplikator
- **Donchian Length** — Rückblick für Strukturlevel
- **Use HTF** — Higher-Timeframe-Bestätigung aktivieren
- **HTF Candle** — Höherer Zeitrahmen für Filter
- **HTF EMA** — EMA-Länge auf höherem Zeitrahmen
- **HTF Min ADX** — Mindest-ADX auf höherem Zeitrahmen
- **Use Choppiness** — Choppiness-Filter aktivieren
- **Chop Length** — Choppiness-Index-Periode
- **Chop Threshold** — Maximal zulässiger Choppiness-Wert
- **Cooldown** — Kerzen Wartezeit nach einem Ausstieg
- **Candle Type** — Haupt-Kerzenzeitrahmen

## Hinweise
Vereinfachter Port des TradingView-Skripts „Nova Futures PRO (SAFE v6) — HTF + Choppiness + Cooldown".
