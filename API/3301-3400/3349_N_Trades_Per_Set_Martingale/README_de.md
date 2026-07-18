# N Trades pro Satz Martingale Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine direkte Umsetzung des MetaTrader-Expertenberaters „N Trades pro Satz Martingal + Schließen und Zurücksetzen bei Eigenkapitalerhöhung“. Es hält die Marktrichtung einfach – es werden nur Long-Trades eingegangen –, verwaltet aber aktiv die Positionsgröße durch eine Martingal-Kaskade und einen aktienbasierten Reset. Ein neuer Trade wird unmittelbar nach dem Abschluss des vorherigen eröffnet, sodass die Strategie ständig am Markt aktiv bleibt.

## Handelslogik
1. **Sequentielle Eingaben** – Die Strategie eröffnet eine Long-Market-Order immer dann, wenn keine Position aktiv ist. Stop-Loss- und Take-Profit-Orders werden direkt nach der Ausführung angehängt.
2. **Gewinn-/Verlustrechnung** – nach dem Schließen einer Position wird der realisierte Preis mit dem Einstiegspreis verglichen. Ein gewinnbringender Abschluss erhöht den Gewinnzähler, andernfalls wird der Verlustzähler erhöht. Break-Even-Ergebnisse werden als Verluste behandelt und entsprechen dem ursprünglichen EA.
3. **Set-Abschluss** – die Anzahl der Trades im aktuellen Set wird ebenfalls verfolgt. Wenn der Zähler `Trades Per Set` erreicht, gilt der Zyklus als abgeschlossen und es kann eines von drei Ergebnissen eintreten:
   - **Alle gewinnen** – das Volumen wird aus dem aktuellen Eigenkapital mit `Equity Divisor` neu berechnet und die Zykluszähler werden zurückgesetzt.
   - **Alle Verluste** – das Volumen wird mit `Scale Factor` multipliziert und die Zykluszähler werden zurückgesetzt.
   - **Gemischte Ergebnisse** – wenn das Set sowohl Siege als auch Niederlagen enthält, werden die Zähler einfach zurückgesetzt und das aktuelle Volumen bleibt erhalten.
4. **Equity-Reset** – Immer wenn das Portfolio-Equity um mindestens `Equity Increase` wächst, führt die Strategie einen globalen Reset durch. Alle Zähler werden gelöscht, das Basisvolumen wird aus dem Eigenkapital neu berechnet und das Eigenkapitalziel wird um denselben Schritt nach vorne verschoben.

Dieses Verhalten spiegelt das ursprüngliche EA wider, bei dem Handelsblöcke durch fxDreema-Logikknoten verkettet wurden.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `Trades Per Set` | Anzahl aufeinanderfolgender Trades, die einen Martingalzyklus bilden. |
| `Stop Loss (pips)` | Stop-Loss-Distanz, gemessen in Preisschritten des Instruments. Zum Deaktivieren auf Null setzen. |
| `Take Profit (pips)` | Take-Profit-Distanz gemessen in Preisschritten. Zum Deaktivieren auf Null setzen. |
| `Scale Factor` | Multiplikator, der auf das Handelsvolumen nach einem vollständigen Verlustsatz angewendet wird. Werte unter 1 werden automatisch auf 1 begrenzt. |
| `Equity Divisor` | Teilt das Kontokapital, um die Basislosgröße nach einem vollständigen Gewinnsatz oder einem Eigenkapital-Reset abzuleiten. |
| `Equity Increase` | Betrag des Aktienwachstums, der den globalen Reset auslöst. Auf Null setzen, um den eigenkapitalbasierten Ausstieg zu deaktivieren. |

## Money-Management
- Die Lautstärke wird auf die gleiche Weise wie beim Original EA an die Instrumentenbeschränkungen (`VolumeStep`, `MinVolume`, `MaxVolume`) angepasst.
- Wenn keine Aktiendaten verfügbar sind, wird das vorherige Volumen wiederverwendet und auf `VolumeStep` zurückgesetzt, wenn es sich um den allerersten Handel handelt.
- Stop-Loss- und Take-Profit-Abstände werden über `PriceStep` in Preisschritte umgewandelt. Wenn das Instrument keinen Preisschritt vorgibt, wird der Rohwert auf die nächste ganze Zahl gerundet.

## Nutzungshinweise
- Die Strategie ist nur long, genau wie das MetaTrader-Skript. Wenn der Broker Shorting unterstützt, deaktivieren Sie es manuell, wenn Sie die Strategie ausführen.
- Da Stopp- und Zielaufträge nach jeder Ausführung neu erstellt werden, werden Teilfüllungen ordnungsgemäß gehandhabt – das verbleibende Volumen erbt dieselben Schutzaufträge.
- Der Equity Reset wird nach jeder geschlossenen Position ausgewertet. Stellen Sie sicher, dass die Portfolioanbindung aktuelle Aktienwerte liefert, damit die Reset-Schwelle erreicht werden kann.
