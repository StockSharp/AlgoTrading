# Fibonacci-Trendumkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie erstellt einen Fibonacci-Kanal aus den letzten Hochs und Tiefs. Eine Position wird eröffnet, wenn der Kurs das 50%-Niveau in Ausbruchsrichtung kreuzt. Die Risikosteuerung basiert auf einem ATR-basierten Stop-Loss und Risiko/Rendite-basierten Take-Profits mit optionalem Teilausstieg.

## Parameter
- **Candle Type** — Kerzenserie.
- **Sensitivity** — Basissensitivität für die Kanalberechnung.
- **ATR Period** — ATR-Länge für den Stop-Loss.
- **ATR Multiplier** — ATR-Faktor für den Stop-Loss.
- **Risk Reward** — Take-Profit-Multiplikator des Risikos.
- **Use Partial TP** — halbe Position beim ersten Ziel schließen.
- **Trade Direction** — erlaubte Handelsrichtung.
