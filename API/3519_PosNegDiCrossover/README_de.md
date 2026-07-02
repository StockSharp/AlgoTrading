# Strategie PosNegDiCrossoverStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **PosNegDiCrossoverStrategy** ist ein StockSharp-Port des MetaTrader-Experten `_HPCS_PosNegDIsCrossOver_Mt4_EA_V01_WE`. Die
Das ursprüngliche System überwacht Übergänge zwischen den +DI- und -DI-Linien des durchschnittlichen Richtungsindex (ADX) und zwar sofort
eröffnet eine Position in Richtung des neuen Leiters. Jede Position ist durch symmetrischen Stop-Loss und Take-Profit geschützt
In Pips gemessene Schwellenwerte und verlorene Trades lösen eine Erholungsschleife im Martingal-Stil aus, die mit einem vervielfachten Volumen wieder eintritt
bis eine festgelegte Anzahl von Versuchen erreicht ist oder ein gewinnbringender Ausstieg erfolgt.

## Handelslogik
1. **Signalerkennung** – wenn die fertige Kerze neue ADX-Werte liefert, vergleicht die Strategie den aktuellen +DI und -DI
Lesungen mit den vorherigen. Ein bullisches Signal erscheint, wenn +DI über -DI kreuzt, während ein bärisches Signal erzeugt wird, wenn
+DI kreuzt unter -DI. Es ist nur ein erster Einstieg pro Bar erlaubt, um den MQL-Schutz widerzuspiegeln, der doppelte Trades verhindert hat
die gleiche Kerze.
2. **Zeitfilter** – Einträge sind nur innerhalb eines benutzerdefinierten Tagesfensters zulässig. Außerhalb des Fensters gelingt die Strategie weiterhin
aktive Positionen (virtuelle Stopps und Gewinnmitnahmen), eröffnet jedoch keine neuen Zyklen oder setzt eine Martingal-Sequenz fort.
3. **Auftragserteilung** – eine Marktorder wird in die erkannte Richtung mit dem konfigurierten Basisvolumen gesendet. Nach dem Befüllen
Die Strategie wandelt `TakeProfitPips` und `StopLossPips` mithilfe des Instrumentschritts in absolute Preise um (ein 10-facher Multiplikator ist).
wird für Instrumente angewendet, die mit 3 oder 5 Dezimalstellen notiert sind) und speichert diese Werte für manuelle Ausgangsprüfungen.
4. **Schutzbehandlung** – jede fertige Kerze wird überprüft: Eine Long-Position wird geschlossen, wenn das Tief den Stopp durchbricht oder wenn die
hoch erreicht das Ziel; Short-Positionen nutzen die symmetrischen Bedingungen. Exits werden mit Marktaufträgen ausgeführt, also im Zyklus
kann das Ergebnis bewerten, bevor er über den nächsten Schritt entscheidet.
5. **Martingale-Schleife** – nach einem Verlust multipliziert die Strategie das aktuelle Volumen mit `MartingaleMultiplier` und erhöht den Zyklus
Zähler und tritt sofort wieder in die gleiche Richtung ein (unter Berücksichtigung des Handelsfensters). Wenn ein profitabler Ausstieg erfolgt oder die
Wenn die Anzahl der Versuche `MartingaleCycleLimit` erreicht, wird der Zyklus auf das Basisvolumen zurückgesetzt und wartet auf den nächsten Crossover von ADX.

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CandleType` | 15-minütiger Zeitrahmen | Kerzenserien, die für ADX-Berechnungen und Stopp-/Zielüberwachung verwendet werden. |
| `AdxPeriod` | 14 | Länge des Average Directional Index-Indikators. |
| `UseTimeFilter` | `true` | Aktiviert das tägliche Handelsfenster. |
| `StartTime` | 00:00 | Beginn der Handelssitzung (Börsenzeit). |
| `StopTime` | 23:59 | Ende der Handelssitzung (Börsenzeit). |
| `OrderVolume` | 0,1 | Anfängliches Marktauftragsvolumen für jeden Zyklus. |
| `TakeProfitPips` | 10 | Abstand zum Gewinnziel in Pips (über den Instrumentenschritt in Preis umgerechnet). |
| `StopLossPips` | 10 | Abstand zum Schutzstopp in Pips. |
| `MartingaleMultiplier` | 2 | Der Volumenmultiplikator wird nach jedem verlorenen Trade während der Martingal-Schleife angewendet. |
| `MartingaleCycleLimit` | 5 | Maximale Anzahl zulässiger Martingal-Wiedereintritte für dasselbe Signal. |

## Notizen
- Die Strategie prüft `IsFormedAndOnlineAndAllowTrading()`, bevor Orders gesendet werden, um eine ordnungsgemäße Initialisierung und ein ordnungsgemäßes Risiko sicherzustellen
Steuerelemente aus dem Framework.
- Die virtuelle Stop-Loss- und Take-Profit-Abwicklung ahmt das MetaTrader-Verhalten nach, bei dem Schutzaufträge direkt angehängt werden
Position. Sie werden an fertigen Kerzen bewertet, um mit dem High-Level StockSharp API kompatibel zu bleiben.
- Wenn das Handelsfenster deaktiviert ist (entweder durch Parameter oder durch Festlegen identischer Start- und Stoppzeiten), verhält sich die Strategie wie folgt
ein 24/5-System, identisch mit dem ursprünglichen Experten, wobei `is_start` und `is_stop` den ganzen Tag abdecken.
