# Volume Weighted MA Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erstellt volumengewichtete gleitende Durchschnitte (VWMA) für Kerzen-Eröffnungs- und Schlusskurse. Die relative Position dieser VWMAs definiert eine Kerzen-"Farbe".

## Handelslogik
1. Eine Kerze ist **bullisch**, wenn VWMA(Eröffnung) unter VWMA(Schluss) liegt.
2. Eine Kerze ist **bärisch**, wenn VWMA(Eröffnung) über VWMA(Schluss) liegt.
3. Wenn die vorherige Kerze bullisch war und die aktuelle neutral oder bärisch wird, eröffnet die Strategie eine Long-Position und schließt alle Short-Positionen.
4. Wenn die vorherige Kerze bärisch war und die aktuelle neutral oder bullisch wird, eröffnet die Strategie eine Short-Position und schließt alle Long-Positionen.

## Parameter
- `VWMA Period` – Länge für die Berechnung beider volumengewichteter gleitender Durchschnitte.
- `Candle Type` – Zeitrahmen der für Berechnungen verwendeten Kerzen.

Ein Schutzblock ist standardmäßig aktiviert: 2% Take‑Profit und 1% Stop‑Loss.
