# Tick-basierte Marktüberwachungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Tick-basierte Marktüberwachungs-Strategie** repliziert das Verhalten des MetaTrader-Skripts `scOnTickMarketWatch.mq5`. Das ursprüngliche Skript scannt kontinuierlich die Market Watch-Liste und löst ein benutzerdefiniertes Ereignis aus, wenn ein neuer Tick für ein Symbol eintrifft, und gibt den Geldkurs und Spread-Informationen aus. Dieses C#-Port konvertiert dieses Verhalten in eine StockSharp-High-Level-Strategie, die auf Level1-Aktualisierungen hört und die Tick-Informationen über den Strategie-Logger protokolliert.

Die Strategie ist absichtlich nicht handelnder Natur. Ihr Zweck ist die Bereitstellung von Diagnosen oder die Überwachung eingehender Tick-Daten über mehrere an denselben Connector angeschlossene Instrumente. Da sie auf StockSharp-Datenabonnements basiert, ist die Lösung ereignisgesteuert und erfordert keine manuellen Verzögerungen oder Schleifen wie die MQL-Version.

## Hauptmerkmale
- Überwacht das primäre Strategie-Instrument und beliebige zusätzliche Instrumente, die in einer kommaseparierten Liste definiert sind.
- Abonniert Level1-Daten für jedes Instrument, um Geld-/Briefkurs-Aktualisierungen zu erfassen.
- Berechnet den Spread (Briefkurs minus Geldkurs), wenn beide Seiten verfügbar sind, und protokolliert detaillierte Informationen auf Englisch.
- Spiegelt den Market Watch-Index durch Beibehaltung einer internen Reihenfolge identisch zur benutzerspezifizierten Liste.
- Gibt benutzerfreundliche Warnungen aus, wenn ein Symbol nicht durch den konfigurierten `SecurityProvider` aufgelöst werden kann.

## Parameter
| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `SymbolsList` | `string` | `""` | Kommaseparierte Liste zusätzlicher Instrument-Identifikatoren (z.B. `AAPL@NASDAQ,MSFT@NASDAQ`), die zusätzlich zum Haupt-`Strategy.Security` beobachtet werden sollen. Jeder Identifikator muss im aktuellen `SecurityProvider` vorhanden sein. |

## Funktionsweise
1. Während `OnStarted` löst die Strategie alle Symbole auf. Das Haupt-`Strategy.Security` wird immer zuerst hinzugefügt, gefolgt von beliebigen zusätzlichen Symbolen aus `SymbolsList`.
2. Für jedes aufgelöste Instrument ruft die Strategie `SubscribeLevel1` auf und hängt einen Callback an, der `Level1ChangeMessage`-Aktualisierungen empfängt.
3. Jeder Callback überprüft, ob die Aktualisierung mindestens eines der relevanten Preisfelder enthält (`LastTradePrice`, `BestBidPrice` oder `BestAskPrice`).
4. Der Geldkurs wird aus `BestBidPrice` genommen (oder auf `LastTradePrice` zurückgegriffen, wenn der beste Geldkurs fehlt), der Briefkurs kommt aus `BestAskPrice`, und der Spread wird berechnet, wenn beide Werte vorhanden sind.
5. Der Logger gibt eine Nachricht aus, die dem Original-Skript entspricht: `New tick on the symbol <id> index in the list=<index> bid=<bid> spread=<spread>`. Wenn der Briefkurs nicht verfügbar ist, wird `spread` als `n/a` gemeldet.
6. Wenn StockSharp ein angefordertes Symbol im `SecurityProvider` nicht finden kann, wird eine Warnung ausgegeben und das Symbol übersprungen.

## Verwendungsanleitung
1. Weisen Sie das Hauptinstrument (`Strategy.Security`) über die Strategie-Konfigurationsoberfläche oder im Code zu.
2. Setzen Sie optional den Parameter `SymbolsList` mit zusätzlichen kommaseparierten Identifikatoren. Die Reihenfolge bestimmt den gemeldeten Index in der Log-Ausgabe.
3. Verbinden Sie die Strategie mit einer Datenquelle, die Level1-Informationen für die gewählten Instrumente liefern kann.
4. Starten Sie die Strategie. Sie abonniert sofort Level1-Daten und beginnt mit der Protokollierung von Tick-Nachrichten.
5. Überprüfen Sie das Strategie-Log, um eingehende Marktdaten und berechnete Spreads zu verifizieren.

## Hinweise und Unterschiede zur MQL-Version
- Die StockSharp-Version ist vollständig ereignisgesteuert. Es gibt keine manuelle Schleife oder `Sleep`-Aufruf; die Plattform ruft Callbacks auf, wenn Daten eintreffen.
- `SymbolsTotal(true)` aus MQL wird emuliert, indem die Reihenfolge beibehalten wird, in der Instrumente zur Beobachtungsliste hinzugefügt werden. Der gemeldete Index beginnt bei null für das Haupt-Strategie-Instrument.
- Spread-Werte in MetaTrader sind punktbasierte Ganzzahlen. In StockSharp wird der Spread als dezimale Preisdifferenz berechnet.
- Benutzerdefinierte Diagrammereignisse werden durch Log-Einträge ersetzt, da StockSharp-Strategien bereits ein flexibles Logging-Subsystem enthalten.
- Wenn einem Symbol in der aktuellen Aktualisierung ein Briefkurs fehlt, wird der Spread als `n/a` gemeldet, was Klarheit über unvollständige Level1-Informationen bietet.
- Die Strategie ist strikt für die Überwachung ausgelegt und sendet keine Orders.

## Beispiel-Log-Ausgabe
```
New tick on the symbol AAPL@NASDAQ index in the list=0 bid=171.25 spread=0.02
New tick on the symbol MSFT@NASDAQ index in the list=1 bid=324.10 spread=n/a
```
Diese Einträge zeigen, wie die Geldkurs- und Spread-Informationen für jedes überwachte Instrument in der Market Watch-Liste gemeldet werden.
