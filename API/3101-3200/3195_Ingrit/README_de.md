# Strategie Ingrit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Ingrit ist eine Konvertierung des MetaTrader 5 Expert Advisors `Ingrit.mq5`. Die Strategie überwacht Fünf-Minuten-Kerzen und reagiert, wenn auf eine starke gegenläufige Kerze ein breiter Ausbruch folgt, der gegen einen Swing von vierzehn Bars zuvor gemessen wird. Orders werden zum Marktpreis platziert, mit konfigurierbaren Stop-Loss-, Take-Profit- und Trailing-Stop-Abständen in Pips. Signale können optional umgekehrt oder dazu gebracht werden, die entgegengesetzte Exposition zu schließen, bevor ein neuer Trade eingegangen wird.

## Strategie-Logik
### Ausbrucherkennung
* Die Strategie verarbeitet nur abgeschlossene Kerzen des ausgewählten Zeitrahmens (Standard: 5 Minuten).
* Für ein **Long**-Setup muss die vorherige Kerze bärisch schließen und der Abstand zwischen dem Hoch der Kerze 14 Bars zurück und dem Tief der vorherigen Kerze muss `StepPips` überschreiten (nach Konvertierung von Pips in Preiseinheiten).
* Für ein **Short**-Setup muss die vorherige Kerze bullisch schließen und der Abstand zwischen dem Hoch der vorherigen Kerze und dem Tief der Kerze 14 Bars zurück muss `StepPips` überschreiten.
* Das Aktivieren von `ReverseSignals` tauscht die Long- und Short-Bedingungen aus und reproduziert den optionalen Umkehrmodus des ursprünglichen Roboters.

### Trade-Verwaltung
* Marktorders werden mit dem `Volume` der Strategie gesendet. Wenn `CloseOppositePositions` aktiviert ist, wird die angeforderte Größe um den absoluten Wert der aktuellen Position erhöht, sodass Umkehrungen die bestehende Exposition im selben Trade schließen.
* Ein fester Stop-Loss und Take-Profit (falls größer als null) werden unmittelbar nach dem Einstieg angehängt. Beide Abstände werden aus Pips unter Verwendung des Preisschritts des Instruments konvertiert und passen sich automatisch an drei- und fünfstellige FX-Kurse an.
* Der Trailing-Stop wird aktiv, sobald der nicht realisierte Gewinn `TrailingStopPips + TrailingStepPips` übersteigt. Bei Long-Positionen folgt der Stop unterhalb des Schlusskurses, bei Short-Positionen oberhalb. Jede Aktualisierung hält den Stop mindestens `TrailingStepPips` vom vorherigen Trailing-Niveau entfernt, um schnelle Modifikationen zu vermeiden.

### Zusätzliches Verhalten
* Das Trailing kann durch Setzen von `TrailingStopPips` auf null deaktiviert werden. Wenn Trailing aktiv ist, muss der Schritt positiv bleiben (die Strategie führt dieselbe Validierung wie die MQL-Version durch).
* Alle Berechnungen laufen auf abgeschlossenen Kerzen; keine Intrabar-Verarbeitung ist in StockSharp erforderlich.
* Die Strategie erstellt keine ausstehenden Orders – jedes Signal wird mit einer Marktorder ausgeführt und die Schutzebenen werden intern simuliert.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen für den Kerzenaufbau der Ausbruchslogik. Standard: 5-Minuten-Zeitrahmen. |
| `StopLossPips` | Stop-Loss-Abstand in Pips. Ein Wert von `0` deaktiviert den festen Stop. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Ein Wert von `0` deaktiviert das feste Ziel. |
| `TrailingStopPips` | Basis-Trailing-Stop-Abstand in Pips. Auf `0` setzen, um Trailing zu deaktivieren. |
| `TrailingStepPips` | Zusätzlicher Pip-Abstand, der gewonnen werden muss, bevor sich der Trailing-Stop wieder bewegt. Muss positiv sein, wenn Trailing aktiviert ist. |
| `StepPips` | Minimaler Swing-Abstand in Pips zwischen der Referenzkerze und der letzten Kerze, bevor ein Signal ausgelöst wird. |
| `ReverseSignals` | Bei `true` werden Long- und Short-Bedingungen getauscht (Umkehrmodus). |
| `CloseOppositePositions` | Bei `true` wird die Marktorder erweitert, um jede entgegengesetzte Exposition zu schließen, bevor die neue Position eröffnet wird. |
| `Volume` | Strategie-Eigenschaft, die die Basis-Ordergröße definiert. Mit `CloseOppositePositions` kombinieren, um das Umkehrverhalten zu steuern. |

## Hinweise
* Pip-Werte werden aus dem Preisschritt des Instruments abgeleitet. Wenn das Instrument drei oder fünf Dezimalstellen verwendet, multipliziert die Strategie den Schritt mit zehn, sodass ein Pip der Standard-FX-Definition entspricht.
* Es gibt keine Python-Version dieser Strategie im Repository.
