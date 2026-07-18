# CashMachine-5-Minuten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine direkte Konvertierung des Expert Advisors **CashMachine 5min** von MQL auf die StockSharp High-Level-API. Sie ist für Fünf-Minuten-Kerzen ausgelegt und kombiniert den DeMarker-Indikator mit einem Stochastik-Crossover-Filter. Das Trade-Management verwendet versteckte Stop-Loss/Take-Profit-Levels zusammen mit stufenweisen Trailing-Regeln, die versuchen, Gewinne zu sichern, sobald Preismomentum erscheint.

## Handelslogik
### Einstiegsbedingungen
- **Long**: Vorheriger DeMarker-Wert unter 0.30 und aktueller Wert bei oder über 0.30 **und** Stochastik-%K kreuzt über 20 auf derselben Kerze. Es darf keine Position offen sein.
- **Short**: Vorheriger DeMarker-Wert über 0.70 und aktueller Wert bei oder unter 0.70 **und** Stochastik-%K kreuzt unter 80. Es darf keine Position offen sein.

### Positionsmanagement
- Es wird nur eine Position gleichzeitig gehalten; entgegengesetzte Signale werden ignoriert, bis der aktuelle Trade geschlossen ist.
- Versteckte Exits schließen die Position, wenn der Preis `Entry ± HiddenStopLoss` oder `Entry ± HiddenTakeProfit` berührt (Werte in Pips interpretiert).
- Drei Zwischengewinnziele (`TargetTp1/2/3`) bewegen einen versteckten Trailing Stop auf `aktueller Preis - (Ziel - 13)` Pips für Longs und `aktueller Preis + (Ziel + 13)` Pips für Shorts. Die zusätzlichen 13 Pips imitieren das ursprüngliche EA-Verhalten und sichern Gewinne nach jedem Meilenstein ohne sofortigen Ausstieg.
- Wenn der Trailing Stop nach der Aktivierung berührt wird, wird die Position zum Marktpreis geschlossen.

## Indikatoren
- **DeMarker** – Erkennt Momentum-Umkehrungen; der Längenparameter entspricht dem ursprünglichen Mittelungsperiod.
- **Stochastik-Oszillator** – Verwendet den ursprünglichen %K-Zeitraum (`StochasticLength`), %K-Glättung (`StochasticK`) und %D-Glättung (`StochasticD`).

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `HiddenTakeProfit` | Versteckter Take-Profit-Abstand in Pips. | 60 |
| `HiddenStopLoss` | Versteckter Stop-Loss-Abstand in Pips. | 30 |
| `TargetTp1` | Erster Trailing-Aktivierungslevel (Pips). | 20 |
| `TargetTp2` | Zweiter Trailing-Aktivierungslevel (Pips). | 35 |
| `TargetTp3` | Dritter Trailing-Aktivierungslevel (Pips). | 50 |
| `DeMarkerLength` | DeMarker-Mittelungsperiode. | 14 |
| `StochasticLength` | Stochastik-%K-Lookback-Periode. | 5 |
| `StochasticK` | %K-Glättungslänge. | 3 |
| `StochasticD` | %D-Glättungslänge. | 3 |
| `CandleType` | Für Berechnungen verwendete Kerzenserie (Standard 5 Minuten). | 5-Minuten-Zeitrahmen |

## Hinweise
- Die Pip-Größe wird aus `Security.PriceStep` abgeleitet. Wenn der Schritt unbekannt ist, wird ein Fallback-Wert von `0.0001` verwendet, der die EA-Logik für 3- und 5-stellige Notierungen reproduziert.
- Alle Handelsentscheidungen basieren auf abgeschlossenen Kerzen; das Intrabar-Verhalten des ursprünglichen EAs kann leicht abweichen, da die MQL-Version auf jedem Tick lief.
- Die Strategie verlässt sich auf StockSharp's Standard-Ordervolumen-Handling — `Strategy.Volume` zum Steuern der Trade-Größe setzen.
