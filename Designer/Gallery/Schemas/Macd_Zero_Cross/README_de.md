# Diagramm der MACD-Nulllinien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der MACD ist der Abstand zwischen einem schnellen und einem langsamen exponentiellen gleitenden Durchschnitt, daher sagt schon das Vorzeichen der MACD-Linie, welcher Durchschnitt oben liegt. Dieses Diagramm ignoriert die Signallinie und handelt genau den Moment des Vorzeichenwechsels: von unter null auf null oder darüber wird gekauft, von null oder darüber nach unten verkauft.

![schema](schema.svg)

## Strategieübersicht

- Der MACD wird mit der schnellen Periode 8, der langsamen Periode 17 und der Signalperiode 9 berechnet; an den Entscheidungen ist nur die MACD-Linie beteiligt, die Signallinie wird berechnet, aber nie gelesen.
- Ein Baustein für den Vorwert hält die MACD-Linie der vorangegangenen Kerze, sodass der Vorzeichenwechsel als echte Kreuzung erkannt wird und nicht als anhaltender Zustand.
- Die aktuelle Position geht in jede Bedingung ein, sodass ein Signal in die bereits gehaltene Richtung verworfen wird, statt die Position zu vergrößern.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die MACD-Linie lag auf der vorherigen Kerze unter null und liegt auf der aktuellen bei null oder darüber, und die Position ist nicht long. Die Order kauft das feste Volumen: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Die MACD-Linie lag auf der vorherigen Kerze bei null oder darüber und liegt auf der aktuellen darunter, und die Position ist nicht short. Die Order verkauft das feste Volumen: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Es gibt weder einen eigenen Ausstiegsbaustein noch einen Schutzstopp: Alle Orders tragen dasselbe Volumen, deshalb führt die Gegenkreuzung der Nulllinie in die Neutralstellung zurück statt zu drehen, und die nächste Position entsteht erst bei der folgenden Kreuzung.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast EMA length | 8 | Periode des schnellen exponentiellen gleitenden Durchschnitts im MACD. |
| Slow EMA length | 17 | Periode des langsamen exponentiellen gleitenden Durchschnitts im MACD. |
| Signal EMA length | 9 | Glättungsperiode der MACD-Signallinie; sie beeinflusst die Handelsentscheidungen nicht. |
| Volume | 1 | Ordervolumen in Lots; für das Öffnen und das Schließen wird derselbe Wert verwendet. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den MACD-Indikatorbaustein, und ein Konverter liest die MACD-Linie aus dem Wert des komplexen Indikators.
- Ein Baustein für den Vorwert verschiebt diese Linie um eine Kerze zurück, und vier Vergleichsbausteine prüfen den vorherigen und den aktuellen Wert gegen eine gemeinsame Null-Konstante.
- Dieselbe Null-Konstante wird mit dem Positionsbaustein verglichen, woraus die beiden Filter Position <= 0 und Position >= 0 entstehen.
- Jedes logische UND verbindet drei Bedingungen - Vorwert, aktuellen Wert und Position - und löst einen Baustein zur Positionsänderung aus, der eine Marktorder mit der gemeinsamen Volumenkonstante sendet.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
