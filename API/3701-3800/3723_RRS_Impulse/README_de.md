# RRS-Impulsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **RRS Impulse Strategy** ist eine High-Level-StockSharp-Portierung des MetaTrader-Expertenberaters „RRS Impulse“. Der ursprüngliche Roboter
kombinierte RSI-, Stochastic- und Bollinger-Bandfilter, rotiert zwischen mehreren Signalstärkemodi und verwendete Schutzstopps und
virtuelle Trailing-Exits. Diese C#-Version behält das gleiche Verhalten bei, verlässt sich jedoch ausschließlich auf die StockSharp High-Level-Kerze API:
Abonnements füttern die Indikatoren, während `BuyMarket`, `SellMarket` und `ClosePosition` die Aufträge ausführen.

## Handelslogik

1. **Anzeigemodi** – Wählen Sie zwischen vier Optionen:
   - `Rsi`: Handeln Sie mit dem Oszillator, wenn er die überkaufte/überverkaufte Zone verlässt.
   - `Stochastic`: erfordern, dass sowohl %K als auch %D über/unter den konfigurierten Werten liegen.
   - `BollingerBands`: Reagieren Sie auf Schlusskurse oberhalb des oberen Bandes oder unterhalb des unteren Bandes.
   - `RsiStochasticBollinger`: wird nur ausgelöst, wenn alle drei Filter die gleiche Richtung bestätigen.
2. **Handelsrichtung** – `Trend` folgt dem Indikator (überkauft führt zu Short-Positionen, überverkauft zu Long-Positionen). `CounterTrend` blendet das aus
Bewegung (überkauft löst Long-Positionen aus, überverkauft löst Short-Positionen aus).
3. **Signalstärke** – Steuert, wie viele Zeitrahmen übereinstimmen müssen, bevor ein Handel abgeschlossen wird:
   - `SingleTimeFrame`: Verwenden Sie nur den von `CandleType` bereitgestellten Basiszeitrahmen.
   - `MultiTimeFrame`: erfordert eine Ausrichtung über die Kerzen M1, M5, M15, M30, H1 und H4.
   - `Strong`: Konzentrieren Sie sich auf das Intraday-Momentum, indem Sie M1, M5, M15 und M30 überprüfen.
   - `VeryStrong`: Bestätigung von der vollständigen Leiter M1 … H4 anfordern. Wenn der kombinierte Anzeigemodus in jedem Zeitrahmen aktiviert ist
muss *alle* drei Filter erfüllen.
4. **Risikomanagement** – Jede Position verfolgt den durchschnittlichen Ausführungspreis und überwacht drei Ausstiegsbedingungen:
   - feste Stop-Loss-Distanz in Pips;
   - feste Take-Profit-Distanz in Pips;
   - Trailing Stop wird aktiviert, sobald der Gewinn `TrailingStartPips` übersteigt, und wird von `TrailingGapPips` beibehalten.
Immer wenn die Richtung umkehrt, ruft die Strategie zuerst `ClosePosition()` auf, um abzuflachen, und eröffnet erst danach den entgegengesetzten Handel
das nächste Bestätigungshäkchen.

## Parameter

| Gruppe       | Name | Beschreibung |
|-------------|------|-------------|
| Daten        | `CandleType` | Basiskerzenserien, die für Handelsentscheidungen verarbeitet werden. |
| Bestellungen      | `TradeVolume` | Beim Versenden von Marktaufträgen verwendetes Volumen. |
| Risiko        | `StopLossPips`, `TakeProfitPips`, `TrailingStartPips`, `TrailingGapPips` | Virtuelle Schutzausgänge, ausgedrückt in Pips. |
| Signale     | `IndicatorMode`, `TradeDirection`, `SignalStrength` | Verhaltensschalter, die aus dem Eingabeblock MQL kopiert wurden. |
| RSI         | `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | RSI-Konfiguration zur Erkennung von Überkauft/Überverkauft. |
| Stochastic  | `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing`, `StochasticUpperLevel`, `StochasticLowerLevel` | Langsame stochastische Oszillatoreinstellungen. |
| Bollinger   | `BollingerPeriod`, `BollingerDeviation` | Bollinger Bands-Rückblick und Abweichungsmultiplikator. |

Alle Parameter unterstützen Optimierungsbereiche, die mit der MetaTrader-Version identisch sind, wo es sinnvoll war (z. B. Stoppen und Distanzen nehmen).
oder Oszillatorschwellen).

## Datenanforderungen

Die Strategie benötigt winzige Kerzen für die Bestätigungsleiter. Wenn `SignalStrength` zusätzliche Zeitrahmen für die Strategie anfordert
fügt automatisch die erforderlichen Abonnements hinzu (`GetWorkingSecurities` kündigt sie der Suchmaschine an). Anführungszeichen der Stufe 1 werden nicht verwendet;
Nur die Schlusskurse fertiger Kerzen steuern Ein- und Ausstiege. Die Schutzlogik bildet somit den „virtuellen“ Stopp/Ziel nach
Verhalten des ursprünglichen Roboters.

## Hinweise zur Konvertierung

- Die zufällige Symbolrotation von EA wurde absichtlich entfernt. StockSharp-Strategien funktionieren mit einem einzigen `Security`, also
port konzentriert sich auf die Abstimmung der Indikatorlogik und des Risikomanagements und überlässt die Instrumentenrotation dem Benutzer.
- Die Auftragsverwaltung erfolgt marktbasiert: Wenn sich die Richtung ändert oder eine Schutzbedingung auslöst, wird `ClosePosition()` aufgerufen.
Spiegelung der MetaTrader-Schleifen, die Tickets durchlaufen haben.
- Bei der Konvertierung werden alle Kommentare auf Englisch beibehalten und Tabulatoren zum Einrücken verwendet, um den Repository-Richtlinien zu entsprechen.
