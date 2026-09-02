# Diagramm der N-Tage-Ausbruchstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Turtle-Klassiker auf den Kern reduziert: Die beiden Indikatoren Highest und Lowest halten die Extreme der letzten N Bars, und eine Kerze, die eines davon überschreitet, gilt als Beginn einer Bewegung. Das Diagramm ist stets im Markt und dreht beim Gegenausbruch.

![schema](schema.svg)

## Strategieübersicht

- Highest liest das Hoch jeder abgeschlossenen Kerze, Lowest das Tief, sodass beide zusammen den Ausbruchskanal der Rückschauperiode bilden.
- Beide Werte werden um eine Kerze verschoben, weil der aktuelle Wert die geprüfte Kerze bereits enthält: Ohne Verschiebung könnte das Hoch den Kanal bestenfalls erreichen, aber nie übertreffen.
- Die aktuelle Position schaltet jeden Einstieg frei oder blockiert ihn, und zum Ordervolumen kommt der Betrag der Position, sodass eine Marktorder die Seite dreht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das Kerzenhoch steigt über den Highest-Wert der Vorkerze und die Position ist nicht long. Die Order kauft das Basisvolumen zuzüglich des Positionsbetrags: Ein Short dreht auf Long, aus der Neutralstellung entsteht ein Long.
- **Short-Einstieg**: Das Kerzentief fällt unter den Lowest-Wert der Vorkerze, der Long-Ausbruch hat auf derselben Kerze nicht ausgelöst und die Position ist nicht short. Die Order verkauft das Basisvolumen zuzüglich des Positionsbetrags.
- **Ausstieg**: Kein Stop, kein Ziel, kein eigener Ausstieg: Die Position bleibt bestehen, bis der Gegenausbruch sie dreht — so verhält sich auch der ursprüngliche Code.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Lookback period | 20 | Anzahl der Bars, über die der Ausbruchskanal gebildet wird; dieselbe Länge gilt für Highest und Lowest. |
| Volume | 1 | Basisordervolumen in Lots; beim Drehen kommt der Betrag der Position hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatoren sowie über zwei Konverter das Hoch und das Tief der aktuellen Kerze.
- Zwei Bausteine für den Vorwert verzögern die Werte von Highest und Lowest um eine Kerze — darin besteht der ganze Kniff dieser Strategie.
- Vergleichsbausteine erzeugen die beiden Ausbruchsflaggen, zwei weitere vergleichen die Position mit null; ein logisches NICHT gibt dem Long-Ausbruch Vorrang vor dem Short-Ausbruch, genau wie der else-if-Zweig des Originals.
- Ein Formelbaustein berechnet das Drehvolumen als Basisvolumen plus Positionsbetrag und speist beide Bausteine zur Positionsänderung.
- Das Original deklariert einen gleitenden Durchschnitt und einen Stop-Prozentsatz, die sein eigener Code nie verwendet, und nutzt standardmäßig einen Kanal über 1500 Minutenbars; das Diagramm lässt die toten Parameter weg und verwendet 20 Bars auf dem Fünf-Minuten-Chart, wie es die README der Strategie und ihr Optimierungsbereich nahelegen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
