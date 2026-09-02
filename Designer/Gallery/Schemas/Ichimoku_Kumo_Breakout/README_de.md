# Diagramm der Ichimoku-Kumo-Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Name verweist auf die Ichimoku-Wolke, doch die Strategie hinter diesem Diagramm handelt tatsächlich das schnellste Linienpaar: Tenkan-sen gegen Kijun-sen. Beide sind der Mittelwert aus höchstem Hoch und tiefstem Tief ihrer Periode, sodass ihre Kreuzung bereits ein kompaktes Trendsignal ist; die Wolke bleibt bewusst außen vor.

![schema](schema.svg)

## Strategieübersicht

- Ein einziger Ichimoku-Baustein erzeugt alle fünf Linien; zwei Konverter holen nur Tenkan-sen und Kijun-sen heraus, die Wolkenlinien spielen in den Regeln keine Rolle.
- Der Kreuzungsbaustein löst nur auf der Kerze aus, auf der Tenkan-sen Kijun-sen tatsächlich schneidet, sodass ein anhaltender Trend keine wiederholten Orders erzeugt.
- Jeder Einstieg wird mit der aktuellen Position verknüpft - genau das hindert das Diagramm daran, weitere Lots auf eine bereits gehaltene Seite zu legen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Tenkan-sen kreuzt Kijun-sen von unten nach oben und die Position ist nicht long. Die Order kauft das feste Volumen: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Tenkan-sen kreuzt Kijun-sen von oben nach unten und die Position ist nicht short. Die Order verkauft das feste Volumen: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Es gibt weder einen eigenen Ausstiegsbaustein noch einen Schutzstopp: Da alle Orders dasselbe Volumen tragen, führt die Gegenkreuzung in die Neutralstellung zurück statt zu drehen, und die andere Seite wird erst bei der darauffolgenden Kreuzung eröffnet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Tenkan period | 9 | Periode von Tenkan-sen, dem Mittelwert aus höchstem Hoch und tiefstem Tief über so viele Kerzen. |
| Kijun period | 26 | Periode von Kijun-sen, gleich gebildet, aber über ein längeres Fenster. |
| Senkou Span B period | 52 | Periode von Senkou Span B; sie gehört nicht zu den Regeln und bestimmt nur, wie lange der Indikator bis zur vollständigen Ausbildung braucht. |
| Volume | 1 | Ordervolumen in Lots; für das Öffnen und das Schließen wird derselbe Wert verwendet. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist einen einzigen Ichimoku-Indikatorbaustein, und zwei Konverter lesen die Werte von Tenkan und Kijun aus dem Wert des komplexen Indikators.
- Beide Linien treffen sich im Kreuzungsbaustein, dessen Ausgang das Long-Signal ist; ein logisches NICHT davon ergibt das Short-Signal.
- Der Positionsbaustein wird zweimal mit einer Null-Konstante verglichen, woraus die Filter Position <= 0 und Position >= 0 entstehen.
- Jedes logische UND verbindet ein Kreuzungssignal mit einem Positionsfilter und löst einen Baustein zur Positionsänderung aus; beide senden Marktorders und beziehen ihr Volumen aus einer gemeinsamen Konstante.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
