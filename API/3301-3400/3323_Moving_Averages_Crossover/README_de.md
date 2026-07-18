# Moving-Averages-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den klassischen Moving-Average-Crossover-Expert-Advisor aus MQL. Sie nutzt StockSharp-High-Level-APIs, um zwei einfache gleitende Durchschnitte aus der gewählten Kerzenserie zu überwachen. Signale entstehen, wenn der schnelle Durchschnitt den langsamen kreuzt, und die Strategie kann optional eine aktive Position schließen, wenn das entgegengesetzte Crossover auftritt.

## Handelslogik
- Den konfigurierten Kerzentyp abonnieren und schnelle sowie langsame SMA auf jeder abgeschlossenen Kerze berechnen.
- Ein bullisches Crossover erkennen, wenn die schnelle SMA von unterhalb auf oberhalb der langsamen SMA wechselt. Ist keine Position aktiv, eine Long-Position mit dem angegebenen Volumen öffnen.
- Ein bärisches Crossover erkennen, wenn die schnelle SMA von oberhalb auf unterhalb der langsamen SMA wechselt. Ist keine Position aktiv, eine Short-Position mit dem angegebenen Volumen öffnen.
- Optional eine bestehende Position sofort schließen, wenn das entgegengesetzte Crossover erkannt wird, entsprechend dem "Close on Opposite Signal"-Schalter des ursprünglichen Skripts.

## Risikomanagement
- Feste Stop-Loss- und Take-Profit-Werte in Preispunkten anwenden. Beide Niveaus werden für jeden neuen Einstieg neu berechnet.
- Den Schutzstop auf Break-even verschieben, nachdem der Preis die konfigurierte Triggerdistanz zurückgelegt hat, und einen zusätzlichen Offset als gesicherten Gewinn behalten.
- Einen Trailing Stop aktivieren, sobald die Position die definierte Startdistanz gewinnt. Der Stop wird anhand des günstigsten Kerzenpreises verschoben und nie gegen den Trade bewegt.

## Parameter
- **Fast MA Period:** Länge der schnellen SMA zur Crossover-Erkennung.
- **Slow MA Period:** Länge der langsamen SMA zur Crossover-Erkennung.
- **Trade Volume:** Ordergröße in Lots.
- **Stop Loss (points):** Distanz in Instrumentpunkten für den anfänglichen Stop Loss.
- **Take Profit (points):** Distanz in Instrumentpunkten für den anfänglichen Take Profit.
- **Break-even Trigger:** Gewinndistanz, die die Verschiebung des Stops auf Break-even aktiviert.
- **Break-even Offset:** Zusätzliche Punkte, die nach Break-even als Gewinn behalten werden.
- **Trailing Start:** Gewinndistanz, die vor Aktivierung des Trailing Stops erforderlich ist.
- **Trailing Distance:** Abstand zwischen Preis und Trailing Stop.
- **Close On Opposite:** Ob ein aktiver Trade geschlossen wird, wenn ein entgegengesetztes Crossover erscheint.
- **Candle Type:** Kerzenserie für Indikatorberechnungen.

## Nutzungshinweise
- Stellen Sie sicher, dass die Strategie an eine Security mit gültigem `PriceStep` gebunden ist. Wenn der Schritt nicht verfügbar ist, wird der Wert 1 verwendet.
- Trailing- und Break-even-Verwaltung arbeiten auf abgeschlossenen Kerzen und entsprechen dem Verhalten des ursprünglichen EA, der auf Barschluss reagiert.
- Optimieren Sie Moving-Average-Längen und Risikoeinstellungen, um das System an verschiedene Märkte oder Zeitrahmen anzupassen.
