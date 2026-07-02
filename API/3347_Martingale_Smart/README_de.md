# Martingale Intelligente Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Martingale Smart ist eine Umsetzung des MetaTrader Expertenberaters „Martingale Smart“. Die Strategie behält immer nur eine offene Position bei und wechselt nach jedem Verlustzyklus zwischen zwei verschiedenen Einstiegsfiltern:

1. **Primärfilter** – Kreuzung zwischen zwei einfachen gleitenden Durchschnitten kombiniert mit der Richtung eines Histogramms mit höherem Zeitrahmen MACD. Dies ist der Standardeingabemodus.
2. **Sekundärer Filter** – Hüllkurven mit gleitendem Durchschnitt. Wenn der gleitende Verlust des vorherigen Zyklus negativ ist, wechselt die Strategie zu diesem Filter. Ein weiterer Verlust schaltet zurück auf den Primärfilter.

Die Martingal-Komponente erhöht das Volumen des nächsten Handels nach einem Verlustzyklus. Sie können entweder das letzte Volumen multiplizieren (klassisches Martingal) oder ein festes Inkrement hinzufügen.

## Datenabonnements

* `CandleType` – Zeitrahmen, der für die Hauptberechnungen und das Handelsmanagement verwendet wird.
* `MacdTimeFrame` – sekundärer Zeitrahmen für den MACD-Filter. Der Standardwert ist ein Monat, um den ursprünglichen EA nachzuahmen, der den Zeitrahmen `PERIOD_MN1` verwendete.

Beide Abonnements werden automatisch in `OnStarted` gestartet.

## Handelslogik

1. Ein neuer Handel wird nur dann in Betracht gezogen, wenn keine offene Position vorliegt und alle Indikatoren gebildet sind.
2. Der Primärfilter geht long, wenn der schnelle MA unter dem langsamen MA liegt und die MACD-Linie über dem Signal liegt (dieselbe Logik für bärische Fälle). Diese Bedingungen folgen dem ursprünglichen EA, das `iMA` und `iMACD` mit einer Verschiebung um einen Takt verwendete.
3. Der Sekundärfilter verwendet eine einfache Hüllkurve mit gleitendem Durchschnitt. Ein Schlusskurs oberhalb des unteren Bandes signalisiert einen Long-Einstieg, während ein Schlusskurs unterhalb des oberen Bandes einen Short-Einstieg signalisiert. Dies reproduziert die auf `iEnvelopes` basierende Logik.
4. Wenn ein Zyklus mit einem negativen Gewinn endet, wechselt die Strategie zum alternativen Filter und berechnet das nächste Volumen gemäß den Martingal-Parametern. Ein profitabler Zyklus behält den aktuellen Filter bei und setzt die Lautstärke auf den Anfangswert zurück.
5. Unmittelbar nach jedem Einstieg werden schützende Stop-Loss- und Take-Profit-Levels mithilfe von Pip-basierten Abständen festgelegt.

## Risikomanagement

* **Break-Even-Stop** – sobald der nicht realisierte Gewinn `BreakEvenTriggerPips` erreicht, springt der Stop-Loss auf den Einstiegspreis zuzüglich eines optionalen Offsets.
* **Klassischer Trailing Stop** – behält einen beweglichen Stop bei, der `TrailingStopPips` vom letzten Schlusskurs entfernt bleibt.
* **Gewinn in Geld mitnehmen** – schließt die Position, wenn der variable Gewinn `MoneyTakeProfit` übersteigt.
* **Take Profit in Prozent** – ähnlich dem Geldziel, jedoch ausgedrückt als Prozentsatz des aktuellen Portfoliowerts.
* **Geld-Trailing-Stop** – wird aktiviert, wenn der variable Gewinn `MoneyTrailingTarget` erreicht; Anschließend verfolgt die Strategie die Gewinnspitze und liquidiert die Position, wenn der Drawdown `MoneyTrailingDrawdown` überschreitet.

Alle monetären Berechnungen basieren auf den `PriceStep` und `StepPrice` des Instruments. Wenn die Datenquelle diese nicht bereitstellt, greift die Strategie auf eine einfache Preis-Volumen-Schätzung zurück.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `UseMoneyTakeProfit` | Aktivieren Sie die feste monetäre Take-Profit-Regel. |
| `MoneyTakeProfit` | Variables Gewinnziel in der Kontowährung. |
| `UsePercentTakeProfit` | Aktivieren Sie den prozentualen Take-Profit. |
| `PercentTakeProfit` | Variables Gewinnziel in % des Portfoliowerts. |
| `EnableMoneyTrailing` | Ermöglichen Sie einen Gewinnrücklauf in Geld. |
| `MoneyTrailingTarget` | Gewinnniveau, das den Trailing Block aktiviert. |
| `MoneyTrailingDrawdown` | Maximal zulässige Gewinnrückgabe, sobald das Trailing aktiv ist. |
| `UseBreakEven` | Verschieben Sie den Stop-Loss nach der konfigurierten Distanz auf die Gewinnschwelle. |
| `BreakEvenTriggerPips` | Erforderliche Gewinndistanz in Pips, bevor sich der Stop bewegt. |
| `BreakEvenOffsetPips` | Zusätzliche Pips werden zum Break-Even-Stopp hinzugefügt. |
| `MartingaleMultiplier` | Nach einem Verlustzyklus angewendeter Multiplikationsfaktor. |
| `InitialVolume` | Für die erste Ordnung jedes Zyklus verwendetes Volumen. |
| `UseDoubleVolume` | Wenn wahr, multiplizieren Sie das Volumen; andernfalls gilt `LotIncrement`. |
| `LotIncrement` | Festes Losinkrement, das verwendet wird, wenn die Verdoppelung deaktiviert ist. |
| `TrailingStopPips` | Abstand des klassischen Trailing Stops in Pips. |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz in Pips. |
| `TakeProfitPips` | Anfängliche Take-Profit-Distanz in Pips. |
| `FastMaPeriod` | Periode des schnellen gleitenden Durchschnitts. |
| `SlowMaPeriod` | Periode des langsamen gleitenden Durchschnitts. |
| `EnvelopePeriod` | Periode des gleitenden Hüllkurvendurchschnitts. |
| `EnvelopeDeviation` | Umschlagbreite in Prozent. |
| `MacdFastLength` | Schnelle EMA-Länge innerhalb von MACD. |
| `MacdSlowLength` | Langsame EMA-Länge innerhalb von MACD. |
| `MacdSignalLength` | Signallänge EMA innerhalb von MACD. |
| `CandleType` | Zeitrahmen des Hauptsignals. |
| `MacdTimeFrame` | Zeitrahmen für die MACD Kerzen. |

## Nutzungshinweise

1. Der Martingal-Schritt wird nur ausgeführt, wenn die vorherige Position vollständig mit einem Verlust geschlossen wurde.
2. Die Strategie erwartet jeweils eine offene Position; Es liquidiert immer die aktuelle Position, bevor es in die entgegengesetzte Richtung einsteigt.
3. Für genaue geldbasierte Schwellenwerte konfigurieren Sie die Vertragsspezifikationen des Instruments (`PriceStep`, `StepPrice` und `VolumeStep`).
4. Break-even- und Trailing-Stops werden bei geschlossenen Kerzen im Hauptzeitrahmen ausgewertet; Intrabar-Spikes werden ignoriert.

## Unterschiede zum MetaTrader EA

* Die Konvertierung verwendet StockSharps übergeordnetes API (`SubscribeCandles` + `Bind`) und den Indikator `MovingAverageConvergenceDivergenceSignal` anstelle direkter Aufrufe von `iMACD`.
* Einige Broker-spezifische Prüfungen (Einfrierstufen, manuelle E-Mail-/Benachrichtigungsaufrufe, Ticket-basierte Schleifen) werden weggelassen, da die StockSharp-Engine diese Aspekte intern verwaltet.
* Geldbasierte Schutzmaßnahmen basieren auf aggregierten Positionen und nicht auf Berechnungen pro Ticket und entsprechen dem Kontomodell von StockSharp.
