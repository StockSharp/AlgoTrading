# Diagramm der Strategie Parabolic SAR + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Parabolic SAR bestimmt, auf welcher Seite des Marktes man steht, und der Relative-Stärke-Index darf lediglich einen Einstieg untersagen, der in eine bereits ausgelaufene Bewegung hinein erfolgen würde. Dieselbe SAR-Linie, die die Position eröffnet, schließt sie auch, sodass der Ausstieg mit dem Trend wandert statt auf einem festen Kurs zu liegen.

![schema](schema.svg)

## Strategieübersicht

- Der Parabolic SAR läuft auf abgeschlossenen Kerzen und wird mit dem Schlusskurs jeder Kerze verglichen: ein Schluss über der Linie bedeutet Aufwärtstrend, darunter Abwärtstrend.
- Der Relative-Stärke-Index wirkt als weicher Filter, genau wie im Originalcode: Long verlangt einen RSI unterhalb der überkauften Marke, Short einen RSI oberhalb der überverkauften Marke, sodass nur Einstiege direkt ins Extrem verhindert werden.
- Positionen werden ausschließlich aus der Neutralstellung eröffnet, und der Seitenwechsel zum SAR ist der einzige Ausstieg — feste Stop-Loss- oder Take-Profit-Marken kennt das Diagramm nicht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Kerze schließt über dem Parabolic SAR, der RSI liegt noch unter der überkauften Marke und die Position ist neutral. Der Positionsbaustein kauft das gemeinsame Volumen zum Marktpreis.
- **Short-Einstieg**: Die Kerze schließt unter dem Parabolic SAR, der RSI liegt noch über der überverkauften Marke und die Position ist neutral. Der Positionsbaustein verkauft das gemeinsame Volumen zum Marktpreis.
- **Ausstieg**: Ein Long wird geschlossen, sobald eine Kerze unter der SAR-Linie schließt, ein Short, sobald sie darüber schließt; beide Schließbausteine arbeiten mit der aktuellen Positionsgröße.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| RSI Length | 14 | Glättungsperiode des Relative-Stärke-Index. |
| RSI Overbought | 70 | Marke, unter der der Index bleiben muss, damit ein Long-Einstieg erlaubt ist. |
| RSI Oversold | 30 | Marke, über der der Index bleiben muss, damit ein Short-Einstieg erlaubt ist. |
| SAR Acceleration | 0.02 | Anfangsbeschleunigungsfaktor des Parabolic SAR. |
| SAR Max acceleration | 0.2 | Obergrenze des SAR-Beschleunigungsfaktors. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein versorgt den Parabolic SAR, den Relative-Stärke-Index und einen Konverter, der den Schlusskurs ausliest.
- Zwei Vergleiche ordnen den Schlusskurs der SAR-Linie zu, zwei weitere prüfen den Index gegen seine Konstanten, drei vergleichen die Position mit null.
- Jedes logische UND sammelt eine Kursbedingung, eine Filterbedingung und eine Positionsbedingung, bevor es einen Positionsbaustein auslöst; die Schließbausteine laufen im Schließmodus und brauchen kein Volumen.
- Die Pause von 130 Kerzen, die die C#-Strategie nach jedem Trade einlegt, hat im Designer keinen entsprechenden Baustein, daher steigt dieses Diagramm früher wieder ein und handelt häufiger.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
