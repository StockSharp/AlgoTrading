# Diagramm der Morning-Star-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Morning Star ist der klassische Dreikerzen-Boden: eine breite Abwärtskerze, eine kleine zögernde Kerze und eine breite Aufwärtskerze, die mehr als die Hälfte der ersten zurückholt. Sein Spiegelbild, der Evening Star, markiert ein Hoch. Dieses Diagramm erkennt beide Formen mit Kerzenmuster-Bausteinen, eröffnet nur aus der Neutralstellung und gibt den Trade zurück, sobald der Kurs auf der falschen Seite eines einfachen gleitenden Durchschnitts schließt.

![schema](schema.svg)

## Strategieübersicht

- Zwei Bausteine des Kerzenmuster-Indikators tragen eigene Dreikerzen-Ausdrücke: Die erste Kerze hat einen Körper und zeigt gegen den späteren Einstieg, der mittlere Körper ist kleiner als die Hälfte davon, und die dritte Kerze schließt jenseits der Mitte der ersten.
- Ein einfacher gleitender Durchschnitt der Schlusskurse ist die einzige Ausstiegsreferenz; das Diagramm kennt weder Stop-Loss noch Take-Profit, genau wie die Originalstrategie.
- Der Positionsbaustein wird mit null verglichen, sodass ein Muster nur aus der Neutralstellung gehandelt und niemals aufgestockt wird.
- Die Originalstrategie friert nach jeder Ausführung außerdem alle Signale für mehrere hundert Bars ein; einen Bar-Zähler gibt es hier als Baustein nicht, daher entfällt diese Pause und wird hier vermerkt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Morning-Star-Baustein meldet das Muster auf der eben abgeschlossenen Kerze und die Position ist null. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Evening-Star-Baustein meldet das Muster auf der eben abgeschlossenen Kerze und die Position ist null. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird von einem Baustein zur Positionsänderung im Schließmodus glattgestellt, sobald eine Kerze unter dem gleitenden Durchschnitt schließt; ein Short ebenso, sobald eine Kerze darüber schließt. Einen Schutzstop gibt es nicht, weil die Ursprungsstrategie ebenfalls keinen hat.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen für das gesamte Diagramm; das Original läuft auf Minutenkerzen und ist hier auf die mitgelieferte Fünf-Minuten-Historie herunterskaliert. |

## Diagrammdetails

- Der Kerzenbaustein speist vier Zweige: die beiden Musterindikatoren, den gleitenden Durchschnitt und einen Konverter für den Schlusskurs.
- Jeder Musterbaustein enthält einen Ausdruck aus drei Bedingungen, sodass die Formation ohne Kette von Formelbausteinen erkannt wird.
- Zwei Vergleichsbausteine stellen den Schlusskurs auf die eine oder andere Seite des Durchschnitts und lösen die beiden Schließbausteine direkt aus.
- Jedes logische UND verbindet ein Muster mit der Positionsprüfung und startet einen Einstieg; beide Einstiegsorders beziehen ihr Volumen aus einer gemeinsamen Konstante, die Schließbausteine berechnen es aus der offenen Position.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
