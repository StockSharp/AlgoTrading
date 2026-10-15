# Flat Channel Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Flat Channel Strategy** ist eine C#-Übersetzung des MetaTrader 5-Expertenberaters *Flat Channel (barabashkakvns Edition)*. Sie behält den ursprünglichen Workflow bei: eine geglättete Standardabweichung hebt Volatilitätskompressionen hervor, die höchsten und niedrigsten Kurse innerhalb der Kompression definieren einen horizontalen Kanal, und Pending-Stop-Orders werden knapp außerhalb dieses Bereichs platziert. Wenn der Markt ausbricht, folgt die Strategie der Bewegung mit vordefinierten Stop-Loss- und Take-Profit-Niveaus und kann optional den Stop nachziehen, wenn die Position Gewinn macht.

## Funktionsweise

1. **Volatilitätskompression-Erkennung** – Ein `StandardDeviation`-Indikator mit Länge `StdDevPeriod` wird durch einen kurzen `SimpleMovingAverage` von `SmoothingLength` geglättet. Wann immer die geglättete Serie `FlatBars` aufeinanderfolgende nicht-steigende Werte druckt, wird der Markt als flat behandelt und die Order-Flags werden neu bewaffnet.
2. **Kanalaufbau** – Sobald ein Flat bestätigt ist, fragt die Strategie den höchsten Hoch und niedrigsten Tief über die letzten `max(ChannelLookback, FlatBars + 1)` Kerzen ab, mit den eingebauten `Highest`/`Lowest`-Indikatoren. Die Kanalhöhe wird durch `ChannelMinPips`/`ChannelMaxPips` nach der Konvertierung von Pips in Preiseinheiten über `PipSize` gefiltert (oder die erkannte Tick-Größe, wenn der Parameter bei null belassen wird).
3. **Pending-Orders** – Wenn die aktuelle Position flat ist und Handel erlaubt ist, sendet die Strategie eine Kauf-Stop-Order bei `high + IndentPips` und eine Verkauf-Stop-Order bei `low − IndentPips`. Jede Order merkt sich die Schutz-Niveaus, die zum Zeitpunkt der Einreichung berechnet wurden.
4. **Breakout-Ausführung** – Wenn eine Pending-Order gefüllt wird, wird die entgegengesetzte Pending-Order automatisch storniert. Der gefüllte Preis wird zum Einstiegsanker für die Trailing-Stop-Logik und die gespeicherten Stop-Loss-/Take-Profit-Abstände werden aktiviert.
5. **Positionsmanagement** – Die aktive Position wird bei jeder abgeschlossenen Kerze überwacht. Wenn der Preis das Stop-Loss- oder Take-Profit-Niveau berührt, gibt die Strategie einen Marktausstieg aus. Wenn `TrailingStopPips` größer als null ist, wird der Stop vorwärts gezogen, sobald der Schlusskurs sich mindestens `TrailingStopPips + TrailingStepPips` vom Füllpreis entfernt.
6. **Sessionsfilter** – Wenn `UseTradingHours` aktiviert ist, läuft die Breakout-Logik nur zwischen `StartHour` (einschließlich) und `EndHour` (ausschließlich). Übernacht-Sessionen werden durch Erlaubnis von `StartHour > EndHour` unterstützt.

## Risikomanagement

- **Dynamischer oder fester Schutz** – Setzen Sie `StopLossPips` / `TakeProfitPips` auf positive Werte für feste Abstände (in Pips). Nullwerte schalten auf dynamisches Sizing basierend auf der Kanalhöhe und den `DynamicStopMultiplier` / `DynamicTakeMultiplier`-Koeffizienten um.
- **Trailing-Stop** – Aktivieren Sie `TrailingStopPips`, um der Bewegung zu folgen, sobald der Trade im Gewinn ist. Die Trailing-Logik respektiert `TrailingStepPips`, um Mikrojustierungen zu vermeiden.
- **Positions-Cap** – `MaxPositions` begrenzt die aggregierte Exposition auf `MaxPositions × TradeVolume`. Wenn dieser Schwellenwert erreicht wird, werden keine neuen Pending-Orders eingereicht, bis die Exposition abnimmt.
- **Richtungsfilter** – `UseBuy` und `UseSell` erlauben der Strategie, im Nur-Breakout-, Nur-Breakdown- oder bidirektionalen Modus zu operieren.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `TradeVolume` | `1` | Volumen für jede Pending-Order. |
| `PipSize` | `0.0001` | Manuelle Pip-Größen-Überschreibung. Bei null zur automatischen Tick-Größe lassen (mit 3/5-Stellen-Anpassung). |
| `StdDevPeriod` | `46` | Lookback für die Basis-`StandardDeviation`. |
| `SmoothingLength` | `3` | Gleitende Durchschnittslänge für die Volatilitätsserie. |
| `FlatBars` | `3` | Anzahl aufeinanderfolgender nicht-steigender geglätteter Volatilitätswerte zum Wiederaufladen von Breakout-Orders. |
| `ChannelLookback` | `5` | Kerzen zur Messung von Hoch und Tief nach Flat-Erkennung. Automatisch mit `FlatBars + 1` verglichen. |
| `ChannelMinPips` | `15` | Minimale Kanalhöhe (in Pips). Auf `0` setzen zum Deaktivieren der Untergrenze. |
| `ChannelMaxPips` | `105` | Maximale Kanalhöhe (in Pips). Auf `0` setzen zum Deaktivieren der Obergrenze. |
| `DynamicStopMultiplier` | `1` | Kanalhöhen-Multiplikator für dynamische Stop-Loss-Berechnung wenn `StopLossPips = 0`. |
| `DynamicTakeMultiplier` | `1` | Kanalhöhen-Multiplikator für dynamische Take-Profit-Berechnung wenn `TakeProfitPips = 0`. |
| `StopLossPips` | `0` | Fester Stop-Loss-Abstand in Pips. Überschreibt die dynamische Formel wenn positiv. |
| `TakeProfitPips` | `0` | Fester Take-Profit-Abstand in Pips. Überschreibt die dynamische Formel wenn positiv. |
| `IndentPips` | `0` | Zusätzlicher Versatz (in Pips) über die Kanalgrenzen für Pending-Orders. |
| `TrailingStopPips` | `5` | Trailing-Stop-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| `TrailingStepPips` | `5` | Minimaler Schritt (in Pips) zum Verschieben des Trailing-Stops. |
| `UseBuy` | `true` | Long (Kauf-Stop)-Breakouts aktivieren. |
| `UseSell` | `true` | Short (Verkauf-Stop)-Breakouts aktivieren. |
| `MaxPositions` | `5` | Maximale Anzahl von Basisvolumen in der aggregierten Position. |
| `UseTradingHours` | `true` | Sessionsfilter aktivieren. |
| `StartHour` | `0` | Sitzungsstartstunde (einschließlich). |
| `EndHour` | `23` | Sitzungsendstunde (ausschließlich). |
| `CandleType` | `H1` | Kerzenserie für Berechnungen (Standard: 1-Stunden-Zeitrahmen). |

## Hinweise

- Die Strategie operiert ausschließlich auf abgeschlossenen Kerzen über die High-Level-`SubscribeCandles().Bind(...)`-API und entspricht dem deterministischen Verhalten des ursprünglichen EA.
- Schutzpreise werden durch `Security.ShrinkPrice` normalisiert, um Börsen-Tick-Größen zu respektieren.
- Wenn beide Pending-Orders aktiv sind und eine davon gefüllt wird, wird die entgegengesetzte Order sofort storniert, sodass nur eine Breakout-Position gleichzeitig offen sein kann.
