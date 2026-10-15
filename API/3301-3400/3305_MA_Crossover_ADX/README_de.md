# MA-Crossover-ADX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MA-Crossover-ADX**-Strategie ist ein direkter Port des MetaTrader-Expert-Advisors `MA_Crossover_ADX`. Sie kombiniert die Steigung eines exponentiellen gleitenden Durchschnitts (EMA) mit Bestätigung durch den Average Directional Index (ADX), um nur in trendstarken Umgebungen zu handeln. Die StockSharp-Implementierung verarbeitet abgeschlossene Kerzen eines konfigurierbaren Zeitrahmens und synchronisiert EMA- und ADX-Aktualisierungen vor der Signalerzeugung. Schutzdistanzen für Stop Loss und Take Profit werden jeder neuen Position automatisch über die punktbasierten Risikoparameter der Strategie angefügt.

## Indikatoren und Daten
- **Exponentieller gleitender Durchschnitt (EMA):** dient als primärer Trendfilter. Die Strategie verfolgt die letzten drei EMA-Werte, um zwei aufeinanderfolgende Steigungen zu berechnen und die `StateEMA(0)`- und `StateEMA(1)`-Prüfungen des ursprünglichen EA nachzubilden.
- **Average Directional Index (ADX):** liefert sowohl die Hauptlinie der Trendstärke als auch die positiven/negativen Richtungsindikatoren (DI+/DI-). Der Abstand zwischen DI+ und DI- repliziert die `StateADX(0)`-Bedingung des EA, während die Hauptlinie eine Mindeststärke erzwingt.
- **Schlusskursserie:** Der Schlusskurs der vorherigen Kerze wird mit der vorherigen EMA verglichen, um sicherzustellen, dass sich der Markt vor einem Einstieg von der gleitenden Durchschnittslinie entfernt hat.

Alle Indikatoren laufen auf demselben Kerzenabonnement, sodass EMA- und ADX-Werte für exakt dieselbe Bar finalisiert sind, bevor eine Entscheidung getroffen wird.

## Handelslogik
### Long-Einstieg
1. Die aktuelle EMA-Steigung (`EMA[0] - EMA[1]`) ist positiv.
2. Die vorherige EMA-Steigung (`EMA[1] - EMA[2]`) ist ebenfalls positiv und signalisiert Beschleunigung.
3. Der Schlusskurs der vorherigen Kerze liegt über dem vorherigen EMA-Wert.
4. Die ADX-Hauptlinie liegt über der konfigurierten Schwelle.
5. DI+ übersteigt DI- und zeigt bullische Richtungsdominanz.

Wenn alle Regeln übereinstimmen und keine Position offen ist, sendet die Strategie eine Markt-Kauforder mit dem konfigurierten Handelsvolumen. Besteht eine Short-Position, wird sie geschlossen, sobald die bullischen Bedingungen erscheinen.

### Short-Einstieg
1. Die aktuelle EMA-Steigung ist negativ.
2. Die vorherige EMA-Steigung ist ebenfalls negativ.
3. Der Schlusskurs der vorherigen Kerze liegt unter dem vorherigen EMA-Wert.
4. Die ADX-Hauptlinie liegt über der Schwelle.
5. DI- übersteigt DI+ und zeigt bärisches Momentum.

Eine Markt-Verkaufsorder wird platziert, sobald alle fünf Bedingungen erfüllt sind und die Strategie flach ist. Offene Long-Positionen werden sofort geschlossen, wenn bärische Filter erscheinen.

### Ausstiegsregeln
- **Long-Positionen:** Ausstieg, wenn die Short-Einstiegsbedingungen auftreten, sodass das System Longs verlässt, wenn das Marktmomentum nach unten dreht.
- **Short-Positionen:** Ausstieg, wenn die Long-Einstiegsbedingungen auftreten.
- **Schutzorders:** `StartProtection` fügt Stop-Loss- und Take-Profit-Orders an, berechnet aus dem `PriceStep` des Instruments multipliziert mit den konfigurierten Punktdistanzen. Diese Orders folgen der aktiven Position über die native Schutzorder-Engine von StockSharp.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `AdxPeriod` | 33 | Anzahl der Bars zur ADX-Berechnung. |
| `AdxThreshold` | 22 | Mindestwert der ADX-Hauptlinie zur Trendvalidierung. |
| `EmaPeriod` | 39 | Länge der EMA für die Steigungserkennung. |
| `StopLossPoints` | 400 | Stop-Loss-Distanz in Instrumentpunkten (multipliziert mit `PriceStep`). |
| `TakeProfitPoints` | 900 | Take-Profit-Distanz in Instrumentpunkten. |
| `TradeVolume` | 0.1 | Volumen jeder neuen Marktorder. |
| `CandleType` | 1-Stunden-Zeitrahmen | Kerzentyp für alle Indikatorberechnungen. |

## Nutzungshinweise
- Stellen Sie sicher, dass das Instrument einen gültigen `PriceStep` liefert. Wenn kein Schritt verfügbar ist, verwendet die Strategie standardmäßig `1` Punkt, damit Schutzorders weiterhin berechnet werden können.
- Die Parameter sind über `SetCanOptimize(true)` optimierungsfreundlich, wodurch Backtests oder Optimierungen verschiedener EMA/ADX-Kombinationen möglich sind.
- Alle Kommentare in der C#-Implementierung sind absichtlich auf Englisch geschrieben, wie von den Projektrichtlinien verlangt.
