# BARS Alligator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die BARS Alligator-Strategie ist ein direkter Port des MetaTrader-Expertenberaters mit demselben Namen. Sie verlässt sich auf Bill Williams' Alligator-Indikator, um erwachende Trends zu erkennen: Wenn die grüne Lips-Linie die blaue Jaw-Linie von unten kreuzt, behandelt das System dies als bullischen Ausbruch, während ein Abwärtskreuz bärisches Momentum signalisiert. Ausstiege beruhen darauf, dass Lips die rote Teeth-Linie kreuzt, damit Positionen geschlossen werden, sobald das Momentum nachlässt. Schutz-Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände werden in Pips konfiguriert und automatisch in Preiseinheiten auf Basis des Preisschritts und der Dezimalgenauigkeit des Instruments umgerechnet.

## Handelslogik

1. **Indikatoraufbau**
   - Drei gleitende Durchschnitte mit konfigurierbaren Längen, Verschiebungen und Typ (einfach, exponentiell, geglättet oder gewichtet) bilden den Alligator.
   - Der angewendete Preis kann der Schluss-, Eröffnungs-, Hoch-, Tief-, Median-, Typisch- oder Gewichtetpreis jeder Kerze sein.
   - Verschiebungen werden durch Speichern eines kleinen rollierenden Puffers für jede Linie respektiert, sodass Crossover dieselben Werte verwenden, die auf einem MetaTrader-Chart erscheinen würden.
2. **Einstiegsbedingungen**
   - **Long**: Die Lips-Linie der vorherigen Bar liegt über der Jaw und war zwei Bars zuvor darunter (bullischer Kreuzaufwärts).
   - **Short**: Die Lips-Linie der vorherigen Bar liegt unter der Jaw und war zwei Bars zuvor darüber (bärischer Kreuzabwärts).
   - Neue Einstiege sind nur erlaubt, wenn die aktuelle Position flach oder bereits in der Signalrichtung ausgerichtet ist und die Gesamtpositionsgröße unter `MaxPositions × OrderVolume` bleibt (oder das risikoskalierte Äquivalent).
3. **Ausstiegsbedingungen**
   - **Long-Ausstieg**: Die Lips-Linie kreuzt unter die Teeth-Linie und die Position ist relativ zum gemittelten Einstiegspreis profitabel.
   - **Short-Ausstieg**: Die Lips-Linie kreuzt über die Teeth-Linie und die Position ist profitabel.
   - Ausstiege erfolgen auch, wenn statische Stop-Loss- oder Take-Profit-Niveaus verletzt werden.
4. **Trailing Stop**
   - Wenn aktiviert, repositioniert ein Trailing Stop den Schutz-Stop, sobald sich der Preis um `TrailingStopPips + TrailingStepPips` in der Trade-Richtung bewegt. Der Stop folgt dann dem Preis in einem Abstand von `TrailingStopPips` Pips, rückt aber nur vor, wenn der Preis neuen Fortschritt von mindestens `TrailingStepPips` Pips macht.
5. **Geldverwaltung**
   - Mit `MoneyMode = FixedVolume` verwenden Orders direkt die Größe `OrderVolume`.
   - Mit `MoneyMode = RiskPercent` weist die Strategie Volumen so zu, dass der konfigurierte Prozentsatz `MoneyValue` des Portfoliokapitals verloren gehen würde, wenn der Stop-Loss getroffen würde. Das Risiko pro Einheit entspricht dem Stop-Loss-Abstand ausgedrückt in Preiseinheiten. Das Ergebnis wird auf den nächsten `VolumeStep` abgerundet (oder auf 1, wenn Step-Informationen fehlen).

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|-----|----------|--------------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Zeitrahmen für Alligator-Berechnungen. |
| `OrderVolume` | `decimal` | `0.1` | Festes Trade-Volumen wenn `MoneyMode` `FixedVolume` ist. |
| `MoneyMode` | `MoneyManagementMode` | `FixedVolume` | Wählt zwischen festem Volumen und risikoprozentualer Größenbestimmung. |
| `MoneyValue` | `decimal` | `1` | Risikoprozentsatz wenn `MoneyMode` `RiskPercent` ist; sonst ignoriert. |
| `MaxPositions` | `int` | `1` | Maximale Anzahl additiver Einstiege pro Richtung (ausgedrückt als Vielfaches des berechneten Ordervolumens). |
| `StopLossPips` | `int` | `150` | Stop-Loss-Abstand in Pips. Null deaktiviert den Schutz-Stop. |
| `TakeProfitPips` | `int` | `150` | Take-Profit-Abstand in Pips. Null deaktiviert das Gewinnziel. |
| `TrailingStopPips` | `int` | `5` | Trailing-Stop-Abstand in Pips. Null deaktiviert das Trailing. |
| `TrailingStepPips` | `int` | `5` | Extra-Abstand, den der Preis zurücklegen muss, bevor der Trailing Stop vorrückt. Muss positiv sein, wenn Trailing aktiviert ist. |
| `JawPeriod` | `int` | `13` | Länge des gleitenden Jaw-Durchschnitts. |
| `JawShift` | `int` | `8` | Vorwärtsverschiebung (in Bars) auf die Jaw-Serie angewendet. |
| `TeethPeriod` | `int` | `8` | Länge des gleitenden Teeth-Durchschnitts. |
| `TeethShift` | `int` | `5` | Vorwärtsverschiebung auf die Teeth-Serie angewendet. |
| `LipsPeriod` | `int` | `5` | Länge des gleitenden Lips-Durchschnitts. |
| `LipsShift` | `int` | `3` | Vorwärtsverschiebung auf die Lips-Serie angewendet. |
| `MaType` | `MovingAverageType` | `Smoothed` | Gleitender-Durchschnitt-Algorithmus für alle drei Alligator-Linien. |
| `AppliedPrice` | `AppliedPriceType` | `Median` | Den gleitenden Durchschnitten zugeführter Kerzenpreis (Schluss, Eröffnung, Hoch, Tief, Median, Typisch oder Gewichtet). |

### Pip-Konvertierung

Die Strategie multipliziert Pip-Einstellungen mit dem Sicherheits-`PriceStep`. Wenn das Instrument 3 oder 5 Dezimalstellen verwendet, wird der Wert um ×10 angepasst, um MetaTraders Pip-Definition für Bruchkurse nachzuahmen. Wenn kein Preisschritt verfügbar ist, wird ein Wert von 1 angenommen.

## Implementierungshinweise

- `MaxPositions` wirkt auf die aggregierte Positionsgröße, da StockSharp im Netting-Modus arbeitet. Zusätzliche Einstiege erhöhen den Durchschnittspreis, anstatt separate Positionstickets zu erstellen.
- Stop-Loss und Take-Profit werden intern verfolgt und mit Marktorders auf der ersten Kerze ausgeführt, die die Schwellen verletzt, was dem Verhalten des ursprünglichen MQL-Experten entspricht.
- Risikobasiertes Sizing erfordert eine von Null verschiedene Stop-Loss-Distanz; andernfalls fällt das System auf das feste `OrderVolume` zurück.
- Alle Indikatorwerte werden nur auf abgeschlossenen Kerzen (`CandleStates.Finished`) aktualisiert, um vorzeitige Signale zu vermeiden.
