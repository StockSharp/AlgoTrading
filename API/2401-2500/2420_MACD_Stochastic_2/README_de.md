# MACD Stochastic 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert die MetaTrader-Expertenlogik "MACD Stochastic 2" mit der High-Level-API von StockSharp. Sie kombiniert einen Drei-Bar-MACD-Swing-Filter mit einem Stochastik-Oszillator, um Momentum-Umkehrungen nahe überverkauften und überkauften Bereichen zu identifizieren. Das Risiko wird durch richtungsspezifische Stops, Take-Profits und einen optionalen Trailing Stop in Pip-Einheiten kontrolliert.

## Überblick

- Funktioniert auf jedem Instrument und Zeitrahmen, der über den Parameter `CandleType` angegeben wird.
- Verwendet die MACD-Hauptlinie zur Bestätigung lokaler Tiefs/Hochs, während MACD-Histogramm und Signallinie für die Visualisierung verfügbar bleiben.
- Bestätigt Einstiege mit einer Stochastik-%K-Messung unter 20 für Longs und über 80 für Shorts.
- Adaptiert die MetaTrader-Pip-Berechnung, indem die Pip-Größe aus dem Kursschritt des Instruments abgeleitet und mit 10 multipliziert wird, wenn das Symbol 3 oder 5 Dezimalstellen hat.

## Handelslogik

### Long-Einstieg

1. MACD-Hauptlinienwerte der aktuellen und der beiden vorherigen abgeschlossenen Kerzen liegen alle unter null.
2. Der aktuelle MACD-Wert ist größer als der vorherige, während der vorherige kleiner als der Wert vor zwei Bars ist (lokales Tief).
3. Stochastik-%K liegt unter 20 (überverkauft).
4. Es ist keine bestehende Long-Position offen (`Position <= 0`). Jede Short-Position wird vor dem Eingehen des neuen Longs glattgestellt.

### Short-Einstieg

1. MACD-Hauptlinienwerte der aktuellen und der beiden vorherigen abgeschlossenen Kerzen liegen alle über null.
2. Der aktuelle MACD-Wert ist kleiner als der vorherige, während der vorherige größer als der Wert vor zwei Bars ist (lokales Hoch).
3. Stochastik-%K liegt über 80 (überkauft).
4. Es ist keine bestehende Short-Position offen (`Position >= 0`). Jede Long-Position wird vor dem Eingehen des neuen Shorts geschlossen.

### Risikomanagement & Ausstiege

- **Harter Stop / Take Profit:** Jede Richtung hat unabhängige Pip-basierte Stop-Loss- und Take-Profit-Abstände. Pips werden anhand der berechneten Pip-Größe in absolute Preisoffsets umgerechnet.
- **Trailing Stop:** Wenn aktiviert, tritt der Trailing Stop in Kraft, nachdem der Preis über die Trailing-Distanz hinausgeht. Der Stop wird nur angehoben/gesenkt, wenn die Bewegung den konfigurierten Trailing-Schritt überschreitet, um übermäßige Order-Fluktuationen zu vermeiden.
- **Gegenläufige Signale:** Beim Eingehen eines entgegengesetzten Signals wird zuerst die bestehende Position glattgestellt, dann die neue mit dem konfigurierten Handelsvolumen eröffnet.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|-------------|
| `TradeVolume` | `1` | Order-Volumen, das mit jedem neuen Trade gesendet wird. |
| `StopLossBuyPips` | `50` | Pip-Abstand für Long-Stop-Loss. Auf `0` setzen zum Deaktivieren. |
| `StopLossSellPips` | `50` | Pip-Abstand für Short-Stop-Loss. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitBuyPips` | `50` | Pip-Abstand für Long-Take-Profit. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitSellPips` | `50` | Pip-Abstand für Short-Take-Profit. Auf `0` setzen zum Deaktivieren. |
| `TrailingStopPips` | `0` | Trailing-Stop-Abstand in Pips. `0` deaktiviert Trailing. |
| `TrailingStepPips` | `5` | Minimaler Pip-Gewinn vor der Aktualisierung des Trailing Stops. Muss bei aktiviertem Trailing positiv bleiben. |
| `MacdFastPeriod` | `12` | Schnelle EMA-Länge für MACD. |
| `MacdSlowPeriod` | `26` | Langsame EMA-Länge für MACD. |
| `MacdSignalPeriod` | `9` | Signal-Glättungslänge für MACD. |
| `StochasticKPeriod` | `5` | Lookback-Periode für Stochastik-%K. |
| `StochasticDPeriod` | `3` | Glättungsperiode für Stochastik-%D. |
| `StochasticSlowing` | `3` | Zusätzliche Glättung, die auf Stochastik-%K angewendet wird. |
| `CandleType` | `1h Zeitrahmen` | Kerzentyp (Zeitrahmen), der für Indikatorberechnungen verwendet wird. |

## Hinweise

- Die Pip-Größenberechnung spiegelt den ursprünglichen MetaTrader-Experten wider: `pip = PriceStep` und wird mit 10 multipliziert, wenn das Instrument mit 3 oder 5 Dezimalstellen notiert wird.
- Stochastik-Schwellenwerte (20/80) bleiben wie im Originalskript als Konstanten. Passen Sie diese direkt im Code an, wenn benutzerdefinierte Niveaus benötigt werden.
- Die Strategie operiert nur auf vollständig abgeschlossenen Kerzen und gewährleistet so Konsistenz mit der Bar-Close-Ausführung von MetaTrader.

## Verwendung

1. Konfigurieren Sie das gewünschte Instrument, `CandleType` und das Volumen vor dem Start der Strategie.
2. Passen Sie Stop-, Take-Profit- und Trailing-Parameter an die Volatilität des Instruments an.
3. Optimieren Sie optional MACD- und Stochastik-Längen mit dem Optimizer von StockSharp dank der exponierten Parameter.
4. Überwachen Sie die Chart-Objekte (Kerzen, MACD, Stochastik, eigene Trades), die automatisch hinzugefügt werden, wenn ein Chart-Bereich verfügbar ist.
