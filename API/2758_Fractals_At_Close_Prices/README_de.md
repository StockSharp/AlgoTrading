# Strategie Fractals bei Schlusspreisen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MetaTrader 5-Expertenberaters **"Fractals at Close prices"** von Vladimir Karputov. Sie analysiert fünf aufeinanderfolgende Schlusspreise, um Fraktale im Stil von Bill Williams zu erkennen, die streng auf Schlusskursen statt auf Hochs oder Tiefs aufgebaut werden. Die beiden jüngsten bullischen und bärischen Fraktale werden verglichen, um den aktiven Trend zu bestimmen. Wenn das neueste bullische Fraktal über dem vorherigen erscheint, eröffnet die Strategie eine Long-Position. Wenn das neueste bärische Fraktal unter dem vorherigen entsteht, wird eine Short-Position eröffnet. Entgegengesetzte Positionen werden immer geschlossen, bevor ein neuer Trade eingegangen wird, sodass die Strategie jederzeit höchstens in einer Richtung aktiv ist.

Trades sind nur zwischen der konfigurierbaren Start- und Endstunde erlaubt. Wenn die aktuelle Stunde außerhalb dieses Fensters liegt, werden alle offenen Positionen sofort geschlossen, was das Verhalten des ursprünglichen EA repliziert. Der Zeitfilter unterstützt Intraday-Fenster (Start < Ende), nächtliche Sitzungen, die Mitternacht überschreiten (Start > Ende), und ganztägigen Handel (Start == Ende).

## Indikatorlogik
* Jede abgeschlossene Kerze wird einer rollierenden Fünf-Element-Warteschlange von Schlusspreisen hinzugefügt.
* Sobald fünf Werte verfügbar sind, wird der mittlere Schluss (zwei Kerzen zurück) bewertet:
  * Ein bullisches Fraktal wird registriert, wenn der mittlere Schluss streng größer als die beiden älteren Schlüsse und größer oder gleich den beiden neueren Schlüssen ist.
  * Ein bärisches Fraktal wird registriert, wenn der mittlere Schluss streng kleiner als die beiden älteren Schlüsse und kleiner oder gleich den beiden neueren Schlüssen ist.
* Die neuesten und vorherigen bullischen Fraktale sowie die neuesten und vorherigen bärischen Fraktale werden für den späteren Vergleich gespeichert.
* Ein bullischer Trend wird erkannt, wenn das neueste bullische Fraktal höher als das vorherige ist. Ein bärischer Trend wird erkannt, wenn das neueste bärische Fraktal niedriger als das vorherige ist.

## Handelsregeln
1. **Long-Einträge**
   * Alle aktiven Short-Positionen zum Marktpreis schließen.
   * Wenn keine Long-Position offen ist, `OrderVolume` zum Marktpreis am Schluss kaufen, der die bullische Fraktal-Sequenz bestätigt hat.
2. **Short-Einträge**
   * Alle aktiven Long-Positionen zum Marktpreis schließen.
   * Wenn keine Short-Position offen ist, `OrderVolume` zum Marktpreis verkaufen, wenn eine bärische Fraktal-Sequenz bestätigt wird.
3. **Sitzungssteuerung**
   * Vor der Anwendung von Signalen überprüft die Strategie, ob `candle.OpenTime.Hour` innerhalb des Handelsfensters liegt. Falls nicht, wird `CloseAllPositions` aufgerufen und die Bar ignoriert.

## Risikomanagement
* Stop-Loss- und Take-Profit-Abstände werden in Pips ausgedrückt. Die Implementierung reproduziert den MT5-Ansatz: Der Symbolpunkt wird mit zehn multipliziert, wenn das Instrument 3 oder 5 Dezimalstellen hat. Der resultierende Pip-Wert wird dann mit den konfigurierten Abständen multipliziert.
* Beim Eingehen einer Position werden die anfänglichen Stop-Loss- und Take-Profit-Levels intern gespeichert. Da StockSharp MT5-Schutzaufträge nicht automatisch verwaltet, überwacht die Strategie abgeschlossene Kerzen und schließt zum Marktpreis, wenn deren Preisspanne das gespeicherte Level berührt.
* Trailing Stops folgen den ursprünglichen EA-Regeln. Ein neuer Stop wird als `close ± TrailingStop` berechnet, sobald der Gewinn `TrailingStop + TrailingStep` übersteigt. Der Trailing Stop wird nur vorgerückt, wenn die Bewegung vom vorherigen Stop mindestens `TrailingStep` beträgt.
* Wenn die Handelszeiten enden, werden alle Positionen unabhängig vom Trailing-Status geschlossen. Dies repliziert das EA-Verhalten, das `CloseAllPositions` außerhalb der erlaubten Sitzung aufruft.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Volumen für jeden Marktauftrag. | `0.1` |
| `StartHour` | Stunde (0-23), ab der der Handel aktiv wird. Wenn gleich `EndHour`, läuft die Strategie den ganzen Tag. | `10` |
| `EndHour` | Stunde (0-23), ab der keine neuen Signale mehr akzeptiert werden. | `22` |
| `StopLossPips` | Stop-Loss-Abstand in Pips. `0` deaktiviert den Stop. | `30` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. `0` deaktiviert den Take. | `50` |
| `TrailingStopPips` | Basis-Trailing-Stop-Abstand in Pips. `0` deaktiviert das Trailing. | `15` |
| `TrailingStepPips` | Zusätzlicher Gewinn (in Pips), der erforderlich ist, bevor der Trailing Stop vorgerückt wird. | `5` |
| `CandleType` | Kerzen-Datentyp, der von der Strategie abonniert wird. Standard ist 1-Stunden-Zeitrahmen-Kerzen. | `1 hour TimeFrame` |

## Implementierungshinweise
* Die Strategie verwendet `SubscribeCandles` mit der High-Level-API und registriert keine Indikatoren manuell, gemäß den Projektrichtlinien.
* Schutzzausgänge (Stop, Take-Profit, Trailing Stop) werden durch das Senden von Marktaufträgen nach Beendigung einer Kerze ausgeführt, da StockSharp MT5-Schutzaufträge nicht automatisch verwaltet.
* Sitzungsfilterung, Fraktal-Erkennung und Trailing-Logik folgen streng der EA-Struktur, einschließlich des Schließens aller Positionen, wenn der Stundenfilter nicht erfüllt ist.
* Die Pip-Skalierungslogik spiegelt die MT5-Implementierung wider, indem der Symbolpunkt bei 3- oder 5-dezimalen Instrumenten mit zehn multipliziert wird, um äquivalente Preisabstände sicherzustellen.

## Verwendungstipps
1. Strategie einem Symbol zuordnen und `OrderVolume` auf die bevorzugte Lotgröße setzen.
2. Einen Kerzentyp wählen, der dem in MetaTrader 5 verwendeten Zeitrahmen entspricht (der ursprüngliche EA funktioniert auf jedem Zeitrahmen).
3. Das Handelsfenster an die Broker-Sitzung oder die gewünschten Stunden anpassen.
4. Die pip-basierten Abstände entsprechend der Instrument-Volatilität einstellen. Größere `TrailingStepPips` reduzieren die Trailing-Häufigkeit, während kleinere Werte den Stop näher am Preis halten.
5. Logs auf Ein- und Ausstiege überwachen; die Strategie zeichnet Trades im optionalen Diagrammbereich zur schnellen visuellen Überprüfung.
