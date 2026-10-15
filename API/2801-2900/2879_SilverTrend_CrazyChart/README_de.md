# SilverTrend CrazyChart-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Experten "Exp_SilverTrend_CrazyChart" mithilfe der StockSharp High-Level-API. Sie handelt auf beiden Marktseiten, indem sie zwei Buffer des benutzerdefinierten SilverTrend CrazyChart-Indikators vergleicht. Wenn das verzögerte Band das aktuelle Band kreuzt, wird eine Position in Richtung des dominanten Bandes eröffnet und jedes entgegengesetzte Engagement geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: Die vorherige abgeschlossene Signalkerze zeigt das aktuelle Band über dem verzögerten Band, und auf der ausgewerteten Kerze fällt das aktuelle Band unter das verzögerte Band oder berührt es. Long-Einstiege können mit `AllowBuyEntry` deaktiviert werden.
  - **Short**: Die vorherige abgeschlossene Signalkerze zeigt das aktuelle Band unter dem verzögerten Band, und auf der ausgewerteten Kerze steigt das aktuelle Band über das verzögerte Band oder berührt es. Short-Einstiege können mit `AllowSellEntry` deaktiviert werden.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Long-Positionen werden geschlossen, wenn das verzögerte Band das aktuelle Band überholt (`AllowBuyExit`) oder wenn Stop-Loss/Take-Profit-Limits ausgelöst werden.
  - Short-Positionen werden geschlossen, wenn das aktuelle Band das verzögerte Band überholt (`AllowSellExit`) oder wenn Stop-Loss/Take-Profit-Limits ausgelöst werden.
- **Stops**: Verwendet absolute Preisabstände, die durch `StopLossPoints` und `TakeProfitPoints` angegeben werden. Wenn einer der Werte auf null gesetzt ist, wird dieses Limit ignoriert.
- **Filter**:
  - `SignalBar` legt fest, wie viele abgeschlossene Kerzen zurück die Kreuzungslogik ausgewertet wird.
  - `CandleType` steuert den Zeitrahmen, der für alle Berechnungen verwendet wird.

## Parameter

- `CandleType` – Für den Indikator verwendete Kerzenserie (Standard: 1-Stunden-Kerzen).
- `Length` – Swing-Periode (`SSP`), die an den SilverTrend CrazyChart-Indikator übergeben wird.
- `KMin` – Unterer Kanalkoeffizient, der den Abstand des verzögerten Bandes steuert.
- `KMax` – Oberer Kanalkoeffizient, der den Abstand des aktuellen Bandes steuert.
- `SignalBar` – Anzahl abgeschlossener Kerzen zurück für die Kreuzungsauswertung (entspricht dem originalen `SignalBar`).
- `AllowBuyEntry` / `AllowSellEntry` – Long/Short-Einstiege ein-/ausschalten.
- `AllowBuyExit` / `AllowSellExit` – Schließen bestehender Long/Short-Positionen ein-/ausschalten.
- `StopLossPoints` – Absoluter Preisabstand vom Einstieg für Long-Stop-Loss und Short-Take-Profit.
- `TakeProfitPoints` – Absoluter Preisabstand vom Einstieg für Long-Take-Profit und Short-Stop-Loss.
- `Volume` – Geerbtes Strategie-Volumen, das die Basisordergröße definiert.

## Indikatorlogik

Der enthaltene `SilverTrendCrazyChartIndicator` reproduziert die originalen MQL-Buffer:

- `Length`, `KMin` und `KMax` berechnen einen Swing-Kanal aus dem höchsten Hoch und niedrigsten Tief über das Lookback-Fenster.
- Das "aktuelle" Band entspricht Buffer 0 in MetaTrader und reagiert sofort auf die neueste Bar.
- Das "verzögerte" Band ist Buffer 1, das das aktuelle Band um `Length + 1` Bars verschiebt, um der ursprünglichen Zeichnungslogik zu entsprechen.

Ein Kauf wird ausgelöst, wenn das verzögerte Band als Trendfilter das aktuelle Band von unten kreuzt, während ein Verkauf erscheint, wenn die Beziehung sich umkehrt. Der Parameter `SignalBar` stellt sicher, dass nur abgeschlossene Kerzen an der Entscheidung teilnehmen, was dem Verhalten des Original-Experten entspricht.
