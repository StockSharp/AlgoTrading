# EA Trix Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die EA Trix Strategie repliziert die Logik des MetaTrader 5 Expert Advisors, der den *TRIX ARROWS*-Indikator mit
grundlegenden Risikomanagement-Tools kombiniert. Das System wartet darauf, dass der dreifach exponentielle gleitende
Durchschnitt (TRIX) und seine Signallinie kreuzen, bevor neue Positionen eingegangen werden. Es kann entweder sofort auf
die Signalkerze reagieren oder die Ausführung bis zur nächsten Kerze verzögern und damit das ursprüngliche
"Trade-at-Close-Bar"-Verhalten emulieren.

## Handelslogik

1. Zwei dreifach geglättete exponentielle gleitende Durchschnitte aufbauen:
   - TRIX wird berechnet, indem drei EMAs mit der **TRIX EMA**-Länge auf den Kerzenschluss angewendet werden und die
     Einbalken-Änderungsrate der dritten Glättung genommen wird.
   - Die Signallinie wird auf die gleiche Weise berechnet, verwendet aber die **Signal EMA**-Länge.
2. Richtungsänderungen durch Kreuzungen erkennen:
   - Wenn die Signallinie **über** TRIX kreuzt, bereitet die Strategie einen Long-Einstieg vor.
   - Wenn die Signallinie **unter** TRIX kreuzt, bereitet sie einen Short-Einstieg vor.
3. Je nach **Trade On Close**-Einstellung wird die Strategie entweder:
   - Sofort beim Schlusskurs der Signalkerze ausführen; oder
   - Die Order in die Warteschlange stellen und beim Öffnungskurs der nächsten Kerze ausführen (entsprechend der MT5
     EA-Option für den Handel auf geschlossenen Kerzen).
4. Vor dem Öffnen einer neuen Position kehrt der Algorithmus automatisch jedes entgegengesetzte Engagement um, damit nur
   eine Nettoposition zu einem Zeitpunkt existiert.

## Positionsmanagement

- **Stop Loss** – optionaler fester Abstand vom Füllkurs. Deaktiviert wenn auf null gesetzt.
- **Take Profit** – optionales Gewinnziel. Deaktiviert wenn auf null gesetzt.
- **Break-even** – sobald der Preis um die ausgewählte Distanz zugunsten des Trades voranschreitet, wird der Stop zum
  Eintrittspreis verschoben.
- **Trailing Stop** – nachdem der Preis sich um die Trailing-Distanz bewegt hat, folgt der Stop dem Preis mit dem
  ausgewählten Mindestinkrement **Trailing Step**.
- Schutzausstiege werden bei jeder abgeschlossenen Kerze mit den Kerzen-Hoch/Tief-Werten ausgewertet. Wenn ein
  Schutzausstieg auslöst, wird die Position mit einer Marktorder geschlossen.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `CandleType` | Datentyp (Zeitrahmen) der von der Strategie verarbeiteten Kerzen. |
| `Volume` | Positionsgröße für neue Einstiege. Bestehende Positionen werden bei Bedarf automatisch umgekehrt. |
| `EmaPeriod` | Länge der exponentiellen gleitenden Durchschnitte zur Berechnung der TRIX-Kurve. |
| `SignalPeriod` | Länge der exponentiellen gleitenden Durchschnitte zur Berechnung der Signalkurve. |
| `TradeOnCloseBar` | Wenn `true`, werden Einstiege in die Warteschlange gestellt und beim nächsten Kerzenöffnungskurs ausgeführt. Wenn `false`, erfolgt die Ausführung sofort beim Signalkerzenschluss. |
| `StopLoss` | Abstand vom Eintrittspreis zum Schutz-Stop. Auf `0` setzen zum Deaktivieren. |
| `TakeProfit` | Abstand zum Gewinnziel. Auf `0` setzen zum Deaktivieren. |
| `TrailingStop` | Abstand zur Aktivierung des Trailing Stops. Auf `0` setzen zum Deaktivieren. |
| `TrailingStep` | Minimales Inkrement beim Aktualisieren des Trailing Stops. |
| `BreakEven` | Erforderlicher Abstand zum Verschieben des Stops zum Eintrittspreis. Auf `0` setzen zum Deaktivieren. |

## Verwendungshinweise

- Die Strategie abonniert einen einzelnen Kerzen-Feed und verlässt sich ausschließlich auf abgeschlossene Kerzen, wie von
  den StockSharp High-Level-API-Richtlinien gefordert.
- Standard-Risikomanagement-Abstände werden in Kurseinheiten ausgedrückt. Passen Sie sie entsprechend der Tick-Größe des
  gehandelten Instruments an.
- Da Orders über Marktbefehle gesendet werden, wird angenommen, dass der Füllkurs in Backtests der Kerzenschluss (oder
  Öffnungskurs für in der Warteschlange stehende Signale) ist.

## Konvertierungshinweise

- Der ursprüngliche MQL5-Expert verwendet den externen *TRIX ARROWS*-Indikator (Code 19056). Die Konvertierung
  rekonstruiert dieselben Berechnungen mit StockSharp `ExponentialMovingAverage`-Instanzen und Änderungsraten-Logik ohne
  Abhängigkeit von benutzerdefinierten Puffern.
- Das MT5-Risikomanagement basierte auf brokerseitigen Stop- und Limit-Orders. In StockSharp werden Schutzausstiege durch
  Überwachung der Kerzenextreme und Ausgabe von Marktorders repliziert.
- Alarmierung, Klangbenachrichtigungen und brokerspezifische Parameter wurden ausgelassen, da sie nicht Teil der zentralen
  Handelslogik sind.
