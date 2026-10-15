# Handelskanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Handelskanal-Strategie** handelt Ausbrüche und Rückläufer rund um einen Donchian-Preiskanal. Wenn das obere Band unverändert bleibt und der Preis es berührt oder darunter schließt, aber über dem Pivot, wird eine Long-Position eröffnet. Für Short-Einstiege gilt die umgekehrte Logik. Der Stop-Loss wird über das gegenüberliegende Band um den ATR-Wert gesetzt. Ein optionaler Trailing-Stop kann den Stop anziehen, während sich der Trade in Gewinnrichtung bewegt.

## Parameter

- `ChannelPeriod` — Länge des Donchian-Kanals.
- `AtrPeriod` — ATR-Periode für die Stop-Loss-Berechnung.
- `Trailing` — Trailing-Stop-Abstand in Preiseinheiten (0 deaktiviert Trailing).
- `CandleType` — Kerzentyp für Berechnungen.
