# OsMaSter V0-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertiert aus dem MetaTrader 5 Expert "OsMaSter v0" (MQL-Datei `OsMaSter v0.mq5`).
- Verwendet ein MACD-Histogramm-(OsMA-)Muster, um Momentum-Umkehrungen nach einer kurzen Konsolidierung zu identifizieren.
- Entwickelt für den Betrieb auf einem einzelnen Instrument und Zeitrahmen, ausgewählt durch den `CandleType`-Parameter.
- Konvertiert automatisch pip-basierte Risikoeinstellungen (Stop-Loss und Take-Profit) in absolute Preisoffsets unter Verwendung des Preisschritts und der Dezimalgenauigkeit des Instruments.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `FastPeriod` | 9 | Länge der schnellen EMA für das MACD-Histogramm. |
| `SlowPeriod` | 26 | Länge der langsamen EMA für das MACD-Histogramm. |
| `SignalPeriod` | 5 | Länge der Signal-EMA zur Glättung des Histogramms. |
| `StopLossPips` | 30 | Abstand zum Schutz-Stop in Pips. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPips` | 50 | Abstand zum Gewinnziel in Pips. Auf `0` setzen zum Deaktivieren. |
| `TradeVolume` | 1 | Auftragsvolumen (Lots) für Marktausstiege. |
| `CandleType` | 15-Minuten-Kerzen | Zeitrahmen für Indikatorberechnungen. |

## Signallogik
1. Die Strategie speichert die letzten vier MACD-Histogrammwerte (`hist0` = aktuell, `hist1` = vorher, ..., `hist3` = drei Kerzen zurück).
2. **Long-Einstieg** wenn `hist3 > hist2`, `hist2 < hist1` und `hist1 < hist0` &mdash; eine steigende Sequenz nach einem lokalen Minimum.
3. **Short-Einstieg** wenn `hist3 < hist2`, `hist2 > hist1` und `hist1 > hist0` &mdash; eine fallende Sequenz nach einem lokalen Maximum.
4. Es kann jeweils nur eine Position offen sein. Die Strategie ignoriert neue Signale, während ein Trade aktiv ist.

## Positionsmanagement
- Aufträge werden mit `BuyMarket()` oder `SellMarket()` mit dem konfigurierten `TradeVolume` gesendet.
- `StartProtection` fügt Stop-Loss- und Take-Profit-Offsets basierend auf Pip-Eingaben an. Die Pip-Größe folgt der Forex-Konvention (Preisschritt × 10 für 3/5-Dezimal-Instrumente, andernfalls der Preisschritt selbst).
- Es gibt keine zusätzlichen Ausstiegsregeln; Positionen werden ausschließlich durch Schutzaufträge oder manuelle Eingriffe verwaltet.

## Hinweise
- Stellen Sie sicher, dass das `Security` korrekte `PriceStep`- und `Decimals`-Werte hat, damit die Pip-Konvertierung mit der Broker-Spezifikation übereinstimmt.
- Kerzen-Zeitrahmen und Volumen anpassen, um dem Handelshorizont des Zielmarkts zu entsprechen.
- Da die Strategie auf Stop- oder Zielausführung wartet, werden aufeinanderfolgende Signale in die gleiche Richtung übersprungen, während eine Position offen bleibt.
