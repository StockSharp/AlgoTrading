# Diagramm der Strategie Bollinger Bands + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei klassische Werkzeuge beantworten hier zwei verschiedene Fragen. Die Bollinger Bands zeigen, wie weit sich der Kurs von seinem eigenen Durchschnitt entfernt hat, der Relative Strength Index zeigt, ob die Bewegung dahinter bereits erschöpft ist. Gehandelt wird nur, wenn beide zusammenpassen, und die Position wird aufgegeben, sobald der Kurs wieder am mittleren Band steht.

![schema](schema.svg)

## Strategieübersicht

- Bollinger Bands und Relative Strength Index werden auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet.
- Die Bänder liefern dem Diagramm gleich drei Zahlen: oberes Band, unteres Band und den mittleren gleitenden Durchschnitt.
- Ein Einstieg verlangt einen Schlusskurs außerhalb eines Bandes und einen RSI-Wert in der passenden Extremzone; eine Bedingung allein genügt nie.
- Das mittlere Band ist das Ziel: die Rückkehr dorthin schließt die Position, sodass das Diagramm keinen bereits zurückgelaufenen Trade weiterträgt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Kerze schließt unter dem unteren Bollinger-Band, der RSI liegt unter der überverkauften Marke und es besteht keine Position. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Die Kerze schließt über dem oberen Bollinger-Band, der RSI liegt über der überkauften Marke und es besteht keine Position. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, wenn der Schlusskurs wieder über das mittlere Band steigt, ein Short, wenn er darunter fällt. Beide Ausstiege nutzen Bausteine zur Positionsänderung im Schließmodus und greifen daher nur bei einer Position der passenden Seite; einen Schutzstopp gibt es nicht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Bollinger Length | 20 | Glättungsperiode der Bollinger Bands. |
| Bollinger Width | 2 | Multiplikator der Standardabweichung, der die Bandbreite bestimmt. |
| RSI Length | 14 | Glättungsperiode des Relative Strength Index. |
| RSI Oversold | 30 | Marke, unter der der RSI als überverkauft gilt. |
| RSI Overbought | 70 | Marke, über der der RSI als überkauft gilt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist drei Zweige: den Bollinger-Baustein, den RSI-Baustein und einen Konverter für den Schlusskurs.
- Drei Konverterbausteine zerlegen den Bollinger-Wert in oberes Band, unteres Band und mittleren gleitenden Durchschnitt.
- Sechs Vergleichsbausteine bilden die Bedingungen: Schlusskurs gegen jedes Band, RSI gegen jede Marke und die Position gegen eine Nullkonstante.
- Jedes logische UND verbindet eine Bandbedingung, eine RSI-Bedingung und die Positionsprüfung und löst einen Baustein zur Positionsänderung aus, dessen Volumen aus einer gemeinsamen Konstante stammt.
- Die ursprüngliche Strategie pausiert nach jedem Trade eine feste Zahl von Bars; einen Bar-Zähler gibt es als Baustein nicht, deshalb entfällt die Pause und allein das mittlere Band bestimmt das Ende eines Trades.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
