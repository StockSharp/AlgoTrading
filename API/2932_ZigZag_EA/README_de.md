# Strategie ZigZag EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert die ursprüngliche MT5 "ZigZag EA"-Logik, indem sie auf drei aufeinanderfolgende ZigZag-Swing-Punkte wartet und zwei Ausbruchs-Stop-Orders rund um den vorherigen Swing-Bereich platziert. Die Konvertierung verwendet die StockSharp-High-Level-API und arbeitet mit fertigen Kerzen. Die letzten zwei abgeschlossenen Swings definieren einen Handelskorridor, während der aktuellste Swing ("room 0" in der MQL-Version) innerhalb dieses Korridors bleiben muss, bevor die Strategie sich mit ausstehenden Orders bewaffnet. Der Ansatz ist symmetrisch: Er bereitet sowohl Buy-Stop- als auch Sell-Stop-Orders vor und lässt den Markt die Richtung des Ausbruchs entscheiden.

## Indikatoren und Marktdaten
* **Highest / Lowest:** StockSharp stellt den MT ZigZag-Indikator nicht direkt bereit, daher ahmt die Konvertierung das ZigZag-Verhalten nach, indem die rollierenden höchsten und niedrigsten Werte über die ausgewählte Tiefe verfolgt werden. Richtungsänderungen aktualisieren die internen Swing-Puffer genau wie der ursprüngliche EA beim Lesen des ZigZag-Puffers.
* **Kerzen:** Die Strategie abonniert einen konfigurierbaren Kerzentyp (Standard: 1-Minuten-Zeitrahmen) und arbeitet nur mit fertigen Kerzen, um die Kompatibilität mit Backtesting und Live-Trading zu gewährleisten.

## Handelslogik
1. Die letzten drei Swing-Werte sammeln. Die zwei vorherigen Werte bestimmen den Korridor (`high`/`low`), und der letzte Wert muss innerhalb des Korridors mit einem kleinen Puffer bleiben, der durch das Broker-Stop-Level definiert wird.
2. Korridor-Größenbeschränkungen einhalten (`MinCorridorPips` und `MaxCorridorPips`). Zu enge Korridore werden ignoriert, um Rauschen zu vermeiden, während übermäßig breite Korridore herausgefiltert werden, um enorme Stops zu vermeiden.
3. Sobald der Korridor gültig ist und keine Position offen ist, werden symmetrische ausstehende Orders platziert:
   * **Buy Stop** bei `high + EntryOffsetPips`.
   * **Sell Stop** bei `low - EntryOffsetPips`.
4. Stops und Ziele werden aus Fibonacci-Verhältnissen genau wie in der MQL-Implementierung berechnet: `FiboStopLoss` multipliziert die Korridor-Höhe und `FiboTakeProfit` subtrahiert den Korridor von der ausgewählten Fibonacci-Projektion. Preise werden auf die Instrument-Tick-Größe gerundet, um Ablehnungen zu vermeiden.
5. Wenn eine ausstehende Order ausgelöst wird, wird die verbleibende ausstehende Order storniert und der schützende Stop-Loss und Take-Profit werden sofort registriert. Die optionale Trailing-Logik zieht den Stop nach, wenn der Preis `TrailingStepPips` über die Trailing-Distanz hinaus vorrückt.
6. Die Strategie schließt sich und rüstet sich automatisch neu, wenn die Position auf null zurückkehrt.

## Risiko- und Orderverwaltung
* Schützende Stop- und Zielorders sind Live-Stop/Limit-Orders, sodass der Broker die Ausführung kontrolliert und Gaps natürlich behandelt werden.
* Die Trailing-Stop-Logik ist dem EA entnommen: Sie aktiviert sich, nachdem der Gewinn `TrailingStopPips + TrailingStepPips` überschreitet, und re-registriert dann den Stop jedes Mal, wenn die Distanz um mindestens einen Trailing-Schritt zunimmt.
* Die Strategie verwendet den Basis-`Volume`-Parameter der StockSharp-`Strategy`-Klasse. Money-Management-Blöcke aus der MQL-Version (festes Lot vs. Risikoprozentsatz) werden absichtlich weggelassen, da die Positionsgrößenbestimmung in StockSharp üblicherweise broker-spezifisch ist.

## Sitzungsfilter
* Handel ist nur zwischen `StartHour:StartMinute` und `StopHour:StopMinute` erlaubt. Wenn die Stop-Zeit früher als die Startzeit ist, behandelt die Strategie sie als Übernachtsitzung und erlaubt Handel über Mitternacht.
* Ausstehende Orders werden immer storniert, wenn die Sitzung geschlossen ist, was das MQL-Verhalten widerspiegelt, das Orders außerhalb des erlaubten Fensters entfernte.

## Parameter
| Name | Beschreibung | Standard |
|------|--------------|----------|
| `CandleType` | Für die Analyse verwendete Kerzenserie. | 1-Minuten-Kerzen |
| `ZigZagDepth` | Anzahl der Kerzen für die Swing-Erkennung. | 12 |
| `EntryOffsetPips` | Über/unter dem Korridor hinzugefügter Versatz. | 5 |
| `MinCorridorPips` | Minimale Korridor-Höhe zur Validierung eines Setups. | 20 |
| `MaxCorridorPips` | Maximal erlaubte Korridor-Höhe. | 100 |
| `FiboStopLoss` | Fibonacci-Level zur Berechnung der Stop-Loss-Distanz. | 61.8% |
| `FiboTakeProfit` | Fibonacci-Level für das Gewinnziel. | 161.8% |
| `StartHour` / `StartMinute` | Beginn des Handelsfensters. | 00:01 |
| `StopHour` / `StopMinute` | Ende des Handelsfensters. | 23:59 |
| `TrailingStopPips` | Vom Trailing-Stop verwendete Distanz. | 5 |
| `TrailingStepPips` | Mindestverbesserung erforderlich, um den Trailing-Stop zu bewegen. | 5 |
| `DrawCorridorLevels` | Wenn aktiviert, zeichnet die Strategie einen vertikalen Korridor-Marker im Chart als Referenz. | `false` |

## Implementierungshinweise
* Pip-Werte werden aus der Instrument-Tick-Größe berechnet. Instrumente mit 3 oder 5 Dezimalstellen multiplizieren den Tick automatisch mit 10, was die "adjusted point"-Logik des EA repliziert.
* Der Code verwendet High-Level-Hilfsmethoden wie `BuyStop`, `SellStop`, `SellLimit` und `BuyLimit`, in Übereinstimmung mit den Projektrichtlinien.
* Kommentare werden auf Englisch gehalten, um die Repository-Anforderungen zu erfüllen, während die detaillierte Beschreibung in mehreren Sprachen über die README-Dateien bereitgestellt wird.
* Es wird kein Python-Port erstellt; der Ordner enthält nur die C#-Implementierung wie angefordert.
