# Diagramm der Doji-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Doji ist eine Kerze, die fast auf demselben Kurs eröffnet und schließt: Käufer und Verkäufer haben sich die ganze Bar über gegenseitig aufgehoben. Das Diagramm misst diese Unentschlossenheit als Verhältnis von Körper zur gesamten Spanne und lässt die beiden Schlusskurse vor dem Doji über die Seite entscheiden, denn der Doji allein sagt nichts über die Richtung. Der einzige Ausstieg ist ein einfacher gleitender Durchschnitt.

![schema](schema.svg)

## Strategieübersicht

- Ein Formelbaustein rechnet Körper minus Spanne mal Schwelle: Ein negatives Ergebnis heißt, der Körper ist kleiner als der erlaubte Anteil der Kerze.
- Die Schreibweise als Multiplikation statt als Division bildet auch die Absicherung des Originalcodes ab: Bei einer Kerze, deren Hoch gleich dem Tief ist, wird null mit null verglichen und kein Doji gemeldet.
- Zwei Bausteine für den vorherigen Wert lesen die Schlusskurse eine und zwei Kerzen zurück: Ein Rückgang dazwischen gilt als Abwärtsschwung und wird gekauft, ein Anstieg als Aufwärtsschwung und wird verkauft.
- Die Originalstrategie sperrt nach einer Ausführung außerdem alle Signale für mehrere hundert Bars; einen Bar-Zähler gibt es hier als Baustein nicht, daher entfällt diese Pause und wird hier vermerkt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die eben abgeschlossene Kerze ist ein Doji, der Schlusskurs eine Kerze zurück liegt unter dem von zwei Kerzen zurück und die Position ist null. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Die eben abgeschlossene Kerze ist ein Doji, der Schlusskurs eine Kerze zurück liegt über dem von zwei Kerzen zurück und die Position ist null. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird von einem Baustein zur Positionsänderung im Schließmodus glattgestellt, sobald eine Kerze unter dem gleitenden Durchschnitt schließt; ein Short, sobald eine Kerze darüber schließt. Die Ursprungsstrategie hat weder Stop-Loss noch Take-Profit, und dieses Diagramm ebenfalls nicht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Doji Threshold | 0.1 | Größtes Verhältnis von Körper zu Gesamtspanne, bei dem eine Kerze noch als Doji gilt. |
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen für das gesamte Diagramm; das Original läuft auf Minutenkerzen und ist hier auf die mitgelieferte Fünf-Minuten-Historie herunterskaliert. |

## Diagrammdetails

- Der Kerzenbaustein speist vier Konverter für Eröffnung, Hoch, Tief und Schluss sowie den gleitenden Durchschnitt.
- Die vier Kurse und die Schwellenkonstante treffen sich in einem einzigen Formelbaustein, und ein Vergleich mit null macht aus dessen Ergebnis das Doji-Signal.
- Der Schlusskurs geht zusätzlich in zwei Bausteine für den vorherigen Wert, deren Ausgänge miteinander verglichen werden und die Richtung des letzten Schwungs liefern.
- Jedes logische UND verbindet das Doji-Signal, eine Richtungsbedingung und die Positionsprüfung und startet einen Einstieg; die beiden Schließbausteine werden direkt von den Vergleichen mit dem gleitenden Durchschnitt ausgelöst.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
