# Heiken Ashi Geglättete Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet EMA-geglättete Heiken-Ashi-Kerzen, um Trendumkehrungen zu erkennen. Eine bullische Kerze, die von Rot zu Grün wechselt, eröffnet eine Long-Position und schließt jede Short-Position. Umgekehrt eröffnet eine bärische Kerze, die von Grün zu Rot wechselt, eine Short-Position und schließt jede Long-Position.

- **Indikatoren**: Heikin-Ashi (mit EMA-Glättung)
- **Einstiegsregeln**:
  - Long eingehen, wenn die geglättete Heikin-Ashi-Kerze bullisch wird.
  - Short eingehen, wenn die geglättete Kerze bärisch wird.
- **Ausstiegsregeln**:
  - Position beim entgegengesetzten Signal umkehren.
- **Parameter**:
  - `EmaLength` – Glättungsperiode für den EMA.
  - `CandleType` – Zeitrahmen der Kerzen.

Der Algorithmus berechnet für jede abgeschlossene Kerze die geglättete Eröffnung und den Schlusskurs neu und wechselt die Position entsprechend.
