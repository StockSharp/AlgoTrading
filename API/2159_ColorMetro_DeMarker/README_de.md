# ColorMetro DeMarker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **ColorMetro DeMarker-Strategie** ist eine StockSharp-Implementierung des MQL5-Expertenberaters `Exp_ColorMETRO_DeMarker`.
Sie verwendet den DeMarker-Indikator in Kombination mit Stufenniveaus, um Handelssignale zu generieren.

## Parameter
- **DeMarker Period** – Periode des DeMarker-Indikators.
- **Fast Step** – Schrittgröße für das schnelle Niveau (MPlus).
- **Slow Step** – Schrittgröße für das langsame Niveau (MMinus).
- **Candle Type** – Zeitrahmen der Kerzen für die Analyse.
- **Enable Buy Open** – Long-Positionen öffnen erlauben.
- **Enable Sell Open** – Short-Positionen öffnen erlauben.
- **Enable Buy Close** – Long-Positionen schließen erlauben.
- **Enable Sell Close** – Short-Positionen schließen erlauben.

## Handelslogik
1. Der DeMarker-Wert wird auf 0–100 skaliert und zwei dynamische Niveaus (MPlus und MMinus) werden mit schnellen und langsamen Schrittgrößen berechnet.
2. Wenn das vorherige schnelle Niveau über dem langsamen lag und das aktuelle schnelle Niveau unter das langsame kreuzt, kauft die Strategie und schließt optional Short-Positionen.
3. Wenn das vorherige schnelle Niveau unter dem langsamen lag und das aktuelle schnelle Niveau über das langsame kreuzt, verkauft die Strategie und schließt optional Long-Positionen.
4. Alle Berechnungen verwenden ausschließlich abgeschlossene Kerzen.

Dieser Ansatz ermöglicht es, Trendwechsel zu verfolgen, die durch die gestuften DeMarker-Niveaus angezeigt werden.
