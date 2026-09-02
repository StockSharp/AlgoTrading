# Diagramm der Keltner-Kanal-Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Keltner-Kanal ist ein exponentieller gleitender Durchschnitt, dessen Ränder um ein Vielfaches der Average True Range nach außen geschoben sind. Das Diagramm wartet auf einen Schlusskurs außerhalb eines Randes, innerhalb dessen der vorherige Schlusskurs noch lag, und dreht die gesamte Position in Richtung des Ausbruchs. Es gibt weder Stopp noch Ziel: Erst der Gegenausbruch nimmt den Trade wieder ab.

![schema](schema.svg)

## Strategieübersicht

- KeltnerChannels erzeugt den Kanal in einem Baustein, zwei Konverter holen den oberen und den unteren Rand aus seinem Wert.
- Bausteine für den vorherigen Wert halten beide Ränder und den Schlusskurs von einem Bar zuvor, sodass der Ausbruch gegen ein Niveau gemessen wird, das der Markt bereits gesehen hat, und nicht gegen einen Rand, der sich mit derselben Kerze verschoben hat.
- Jede Order trägt das gemeinsame Volumen plus den Betrag der Position, sodass eine einzige Order den Trade dreht statt ihn nur zu verkleinern.
- Das C#-Original arbeitet mit einem Kanal der Periode 500 und Multiplikator 10 auf Minutenkerzen; das Diagramm nutzt den in dessen README dokumentierten Kanal 20 / 2 auf Fünfminutenkerzen, damit ein Ausbruch auf gewöhnlichen Daten tatsächlich vorkommt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt über dem oberen Band der Vorkerze, während der vorherige Schlusskurs noch darauf oder darunter lag, und die Position ist nicht long. Die Order kauft das Volumen plus den offenen Short und dreht damit auf Long.
- **Short-Einstieg**: Der Schlusskurs liegt unter dem unteren Band der Vorkerze, während der vorherige Schlusskurs noch darauf oder darüber lag, und die Position ist nicht short. Die Order verkauft das Volumen plus den offenen Long und dreht damit auf Short.
- **Ausstieg**: Es gibt keinen Ausstiegsbaustein: Der Gegenausbruch dreht die Position, genau wie in der Originalstrategie, die weder Stop-Loss noch Take-Profit kennt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Channel period | 20 | Periode des Keltner-Kanals; sie bestimmt sowohl den gleitenden Durchschnitt als auch die Spanne, aus der die Breite gebildet wird. |
| ATR multiplier | 2 | Wie viele Spannen die Kanalränder von der Mittellinie entfernt liegen. |
| Volume | 1 | Ordervolumen in Lots, bevor die offene Position hinzuaddiert wird. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikator und einen Konverter, der den Schlusskurs aus der Kerze liest.
- Drei Bausteine für den vorherigen Wert verschieben oberes Band, unteres Band und Schlusskurs um einen Bar; der Indikator liefert erst im geformten Zustand, sodass die ersten Bars von selbst übersprungen werden.
- Vier Vergleichsbausteine bilden jede Seite des Ausbruchs: einer für die Kerze, die heraustritt, und einer für die Kerze, die noch drinnen war.
- Die Position wird mit einer Nullkonstante verglichen und geht in beide logischen UND ein, während ein Formelbaustein ihren Betrag zur gemeinsamen Volumenkonstante addiert und so die Umkehrorder bemisst.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
