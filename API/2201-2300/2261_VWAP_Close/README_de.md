# VWAP Schlusskurs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie berechnet einen volumengewichteten gleitenden Durchschnitt (VWMA) der Schlusskurse. Wenn der VWMA die Richtung ändert, dient dies als Signal für potenzielle Ein- oder Ausstiege:

- Wenn der VWMA fiel und sich nach oben dreht (ein Tal bildet), schließt die Strategie alle Short-Positionen und kann eine Long-Position eröffnen.
- Wenn der VWMA stieg und sich nach unten dreht (eine Spitze bildet), schließt die Strategie alle Long-Positionen und kann eine Short-Position eröffnen.

## Parameter
- **Period** – Anzahl der Kerzen für die VWMA-Berechnung.
- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen.
- **Buy Open** – Eröffnung von Long-Positionen aktivieren.
- **Sell Open** – Eröffnung von Short-Positionen aktivieren.
- **Buy Close** – Schließen von Long-Positionen erlauben, wenn der VWMA nach unten dreht.
- **Sell Close** – Schließen von Short-Positionen erlauben, wenn der VWMA nach oben dreht.

## Hinweise
Die Strategie verwendet den `VolumeWeightedMovingAverage`-Indikator von StockSharp und verarbeitet nur abgeschlossene Kerzen. Das Handelsvolumen wird aus der `Volume`-Eigenschaft der Strategie entnommen; beim Eröffnen einer neuen Position wird die entgegengesetzte Position automatisch geschlossen.
