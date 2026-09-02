# Diagramm der Strategie Bollinger Bands + Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Mean-Reversion, die zwei voneinander unabhängige Zeichen einer erschöpften Bewegung verlangt: Der Schlusskurs muss ein Bollinger Band erreichen und die %K-Linie des Stochastic muss in der passenden Extremzone stehen. Die Position wird zurückgegeben, sobald der Kurs die Mittellinie derselben Bänder kreuzt, sodass der Trade genau so lange lebt wie die Abweichung.

![schema](schema.svg)

## Strategieübersicht

- Die Bollinger Bands liefern aus einem einzigen Indikatorbaustein drei Linien: oberes Band, unteres Band und den mittleren gleitenden Durchschnitt als Ausstiegsniveau.
- Vom Stochastic wird nur die %K-Linie genutzt; die %D-Linie bleibt bewusst unverbunden, genau wie in der Originalstrategie.
- Eingestiegen wird ausschließlich aus der Neutralstellung, sodass das Diagramm eine laufende Position nie verbilligt.
- Die Originalstrategie wartet zwischen zwei Trades zusätzlich eine feste Anzahl Kerzen; für diesen Zähler gibt es keinen Baustein, er entfällt, weshalb dieses Diagramm häufiger handelt als der Quellcode.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt auf oder unter dem unteren Bollinger Band, %K liegt unter der überverkauften Marke und die Position ist neutral. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Schlusskurs liegt auf oder über dem oberen Bollinger Band, %K liegt über der überkauften Marke und die Position ist neutral. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, wenn der Schlusskurs über die Mittellinie steigt, ein Short, wenn er darunter fällt. Beide Ausstiege nutzen Bausteine zur Positionsänderung im Schließen-Modus: Sie berechnen das Volumen aus der offenen Position und bleiben untätig, wenn nichts zu schließen ist. Stopps oder Ziele gibt es nicht, genau wie im Originalcode.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Bollinger Length | 20 | Glättungsperiode der Bollinger Bands, die zugleich die Mittellinie für den Ausstieg festlegt. |
| Bollinger Width | 2 | Multiplikator der Standardabweichung, der den Abstand der Bänder zur Mittellinie bestimmt. |
| %K Oversold | 20 | Marke, unter der die %K-Linie einen Kauf bestätigt. |
| %K Overbought | 80 | Marke, über der die %K-Linie einen Verkauf bestätigt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Ein Kerzenbaustein versorgt die Bollinger Bands, den Stochastic und einen Konverter, der den Schlusskurs herauszieht.
- Konverterbausteine zerlegen die Indikatoren in einzelne Linien: oberes Band, unteres Band, Mittellinie und %K.
- Jedes logische UND verbindet eine Bandbedingung, eine Stochastic-Bedingung und die Prüfung auf Neutralstellung und löst dann einen Baustein zur Positionsänderung im Eröffnungsmodus aus.
- Die beiden Ausstiegsbausteine werden direkt von den Vergleichen mit der Mittellinie ausgelöst; der Schließen-Modus des Bausteins entscheidet, ob wirklich eine Order nötig ist.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
