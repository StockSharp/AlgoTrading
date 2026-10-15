# Diagramm der Strategie Narrow Range Limit Bracket
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Markt, der sich nicht mehr bewegt, ist ein Markt, der sich auf eine Bewegung vorbereitet. Dieses Diagramm misst die Höhe jeder Kerze, findet den Moment, in dem diese Höhe auf den kleinsten Wert seit mehreren Kerzen zusammenfällt, und klammert diese Kerze mit zwei Niveaus ein: ihrem Hoch darüber und ihrem Tief darunter. Welches Niveau der Kurs zuerst per Schlusskurs überschreitet, entscheidet über die Richtung des Trades.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene 15-Minuten-Kerzen speisen drei Konverter, die Hoch, Tief und Schlusskurs jeder Kerze auslesen.
- Eine Formel zieht das Tief vom Hoch ab, sodass jede Kerze auf eine einzige Zahl reduziert wird: ihre Spanne.
- Der Indikator Lowest läuft über diese Zahl und nicht über den Kurs und meldet die kleinste Spanne der letzten sechs Kerzen; eine Formel multipliziert sie mit dem Squeeze Factor und ergibt so die Breite, in die eine Kerze passen muss, um als schmal zu gelten.
- Ein zweiter Zweig baut dieselbe Messung eine Kerze zuvor nach: Previous value gibt die vorherige Kerze aus, zwei Konverter und eine Formel liefern deren Spanne, und ein weiteres Previous value nimmt den Wert, den der Indikator Lowest zu diesem Zeitpunkt hatte.
- Eine logische Bedingung führt drei Antworten zusammen: Diese Kerze ist schmal, die Kerze davor war es nicht, und die Position ist flat. Das ist der Squeeze.
- Beim Squeeze werden Hoch und Tief dieser Kerze in zwei Variablen festgehalten, die diese beiden Kurse Kerze für Kerze weiter halten: die Klammer. Eine dritte Variable schaltet sie scharf.
- Ab der nächsten Kerze beobachten zwei Vergleiche den Schlusskurs gegenüber den beiden Niveaus, und eine logische Bedingung gibt einen Einstieg per Market-Order frei, wenn der Schlusskurs eines von ihnen überschreitet, die Klammer scharf ist und die Position flat ist.
- Zwei Chart-Panels zeichnen das Ergebnis: im einen der Kurs mit den Klammerniveaus, den Orders und den Ausführungen, im anderen die Kerzenspanne gegenüber ihrer Squeeze-Grenze.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs einer Kerze liegt über dem oberen Klammerniveau, die Klammer ist scharf und die Position ist flat. Position modify kauft das Ordervolumen per Market-Order.
- **Short-Einstieg**: Der Schlusskurs einer Kerze liegt unter dem unteren Klammerniveau, unter denselben Bedingungen aus scharfer Klammer und flacher Position. Position modify verkauft das Ordervolumen per Market-Order.
- **Ausstieg**: Das Diagramm hat kein eigenes Ausstiegssignal. Die beiden Einstiege werden von Combination zu einem einzigen Strom von Ausführungen zusammengeführt, der Position protection speist: Dieser Block übernimmt die Position und schließt sie 550 Kurseinheiten im Gewinn oder 550 Kurseinheiten im Verlust, gemessen ab dem Ausführungspreis. Derselbe Strom von Ausführungen entschärft die Klammer, sodass ein bereits gehandeltes Niveau nicht erneut gehandelt werden kann.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:15:00 | Zeitrahmen der Kerzen, mit denen das gesamte Diagramm arbeitet. |
| Period | 6 | Anzahl der Kerzen, über die der Indikator Lowest die kleinste Spanne misst. |
| Squeeze Factor | 1.08 | Um wie viel breiter als die kleinste Spanne der jüngsten Kerzen die aktuelle Kerze noch sein darf, um als schmal zu gelten. |
| Expansion Factor | 1.05 | Um wie viel breiter als ihre eigene kleinste jüngste Spanne die vorherige Kerze sein musste, damit der Squeeze als frisch gilt. |
| Order Volume | 1 | Ordergröße in Lots. |
| Take Profit | 550 | Take-Profit-Abstand vom Ausführungspreis, in Kurseinheiten. |
| Stop Loss | 550 | Stop-Loss-Abstand vom Ausführungspreis, in Kurseinheiten. |

## Diagrammdetails

- Der Squeeze ist ein Vergleich einer Kerze mit ihrer eigenen jüngsten Historie und braucht deshalb keine feste Vorstellung davon, was eine schmale Kerze ist: Der Squeeze Factor sagt nur, wie nah die aktuelle Spanne an die kleinste der jüngsten Spannen herankommen muss.
- Der Expansion Factor sichert die andere Seite derselben Idee ab. Ohne ihn würde eine Reihe gleich ruhiger Kerzen immer wieder neue Signale erzeugen; die Forderung, dass die vorherige Kerze breiter als ihre eigene Grenze war, sorgt dafür, dass das Signal den Moment markiert, in dem die Volatilität zusammenfällt, und nicht die gesamte ruhige Phase.
- Dem Indikator Lowest wird eine Zahl statt einer Kerze zugeführt, und genau das lässt einen gewöhnlichen Kursindikator als Volatilitätsmesser arbeiten.
- Die Klammer wird durch den Squeeze scharf geschaltet und durch eine Einstiegsausführung entschärft, und ihre Niveaus bleiben bestehen, bis der nächste Squeeze sie ersetzt. Eine Klammer lebt damit so lange, bis sie gehandelt wird oder ein neuer Squeeze auftritt, statt nach einer Anzahl von Bars zu verfallen.
- Der Squeeze setzt außerdem eine flache Position voraus, sodass die Niveaus nie überschrieben werden, während ein Trade läuft; und die Positionsbedingung an beiden Einstiegsblöcken verweigert eine zweite Order auf eine offene Position.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
