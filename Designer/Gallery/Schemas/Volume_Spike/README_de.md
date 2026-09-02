# Diagramm der Volumenspitzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Kerze, die deutlich mehr Volumen trägt als die vorige, bedeutet meist, dass gerade jemand in Größe gehandelt hat. Dieses Diagramm wartet auf diesen Sprung, lässt einen einfachen gleitenden Durchschnitt entscheiden, ob die Menge kauft oder verkauft, und geht mit, solange das Volumen weiter wächst. Sobald das Volumen unter das der vorigen Kerze fällt, ist der Trade beendet.

![schema](schema.svg)

## Strategieübersicht

- Das Volumen der Kerze wird mit dem Volumen der vorigen Kerze verglichen, nicht mit einem Durchschnitt vieler Kerzen, genau wie im Originalcode.
- Der Vergleich ist als Multiplikation statt als Division geschrieben, sodass eine Kerze ganz ohne Volumen das Diagramm nicht stören kann.
- Ein einfacher gleitender Durchschnitt des Schlusskurses über zwanzig Kerzen wählt die Seite: darüber wird die Spitze gekauft, darunter verkauft.
- Eingestiegen wird nur aus der Neutralstellung, und der Ausstieg braucht weder den Durchschnitt noch die Spitze, sondern nur ein Volumen, das nicht mehr wächst.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das Volumen der Kerze beträgt mindestens das Vielfache des Volumens der vorigen Kerze, die Kerze schloss über dem gleitenden Durchschnitt und die Position ist neutral. Die Order kauft ein Lot zum Markt.
- **Short-Einstieg**: Das Volumen der Kerze beträgt mindestens das Vielfache des Volumens der vorigen Kerze, die Kerze schloss unter dem gleitenden Durchschnitt und die Position ist neutral. Die Order verkauft ein Lot zum Markt.
- **Ausstieg**: Beide Seiten steigen auf der ersten Kerze aus, deren Volumen kleiner ist als das der Kerze davor, über Bausteine zur Positionsänderung im Schließmodus. Die Originalstrategie hat weder Stop Loss noch Take Profit, dieses Diagramm ebenso wenig.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Spike Multiplier | 2 | Wie viel Mal das Volumen der vorigen Kerze die aktuelle Kerze tragen muss, damit die Spitze zählt. |
| SMA Length | 20 | Periode des einfachen gleitenden Durchschnitts, der die Seite des Einstiegs wählt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist einen Konverter für das Volumen, einen für den Schlusskurs und den Durchschnittsbaustein; ein Baustein für den vorigen Wert mit Versatz von einer Kerze liefert das Volumen der früheren Kerze.
- Eine Formel multipliziert dieses frühere Volumen mit der Konstante des Spitzenfaktors, und ein Vergleichsbaustein prüft das aktuelle Volumen gegen das Ergebnis.
- Jedes logische UND verbindet die Spitze, die vom Durchschnitt gewählte Seite und die Prüfung auf Neutralstellung und löst einen Baustein zur Positionsänderung im Modus "nur eröffnen" aus.
- Der Vergleich des fallenden Volumens geht direkt auf beide Schließbausteine, die im Schließmodus stehen und deshalb untätig bleiben, solange keine Position offen ist. Das Original pausiert zusätzlich nach jedem Trade fünfhundert Kerzen lang und arbeitet auf Minutenkerzen; für eine solche Pause gibt es keinen Zählerbaustein und die mitgelieferte Historie ist gröber als eine Minute, also läuft das Diagramm auf Fünf-Minuten-Kerzen und handelt jede Spitze.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
