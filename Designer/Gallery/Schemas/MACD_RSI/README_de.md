# Diagramm der MACD-RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MACD gibt die Richtung vor, RSI den Zeitpunkt. Solange die MACD-Linie über ihrer Signallinie liegt, wartet das Diagramm darauf, dass der Relative-Stärke-Index in die überverkaufte Zone fällt, und kauft diesen Rücksetzer; spiegelbildlich wird ein überkaufter RSI verkauft, solange MACD unter seiner Signallinie liegt. Die Position wird zurückgegeben, sobald die beiden MACD-Linien die Seiten tauschen.

![schema](schema.svg)

## Strategieübersicht

- Der Trendtest ist ein Niveauvergleich und kein Kreuzen: Entscheidend ist, auf welcher Seite der Signallinie die MACD-Linie gerade steht, damit der Filter so lange greift, wie der Trend anhält.
- Der Einstieg innerhalb dieses Trends ist bewusst antizyklisch - der RSI muss dagegen gelaufen sein, sodass das Diagramm Rücksetzer kauft, statt Ausbrüchen hinterherzulaufen.
- Der Ausstieg nutzt dasselbe Linienpaar: Ein Long wird geschlossen, wenn MACD unter seine Signallinie fällt, ein Short, wenn er darüber steigt.
- Stop-Loss und Take-Profit gibt es im Diagramm nicht, genau wie in der Originalstrategie, in der nur der MACD-Wechsel aus der Position führt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die MACD-Linie liegt über ihrer Signallinie, der RSI unter der überverkauften Marke und es besteht keine Position. Die Order kauft ein Lot zum Marktpreis.
- **Short-Einstieg**: Die MACD-Linie liegt unter ihrer Signallinie, der RSI über der überkauften Marke und es besteht keine Position. Die Order verkauft ein Lot zum Marktpreis.
- **Ausstieg**: Ein Long wird auf der ersten Kerze geschlossen, auf der MACD unter seine Signallinie fällt, ein Short auf der ersten Kerze darüber; beide Schließbausteine lesen das Volumen aus der offenen Position.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| MACD Fast Length | 12 | Periode der schnellen EMA im MACD. |
| MACD Slow Length | 26 | Periode der langsamen EMA im MACD. |
| MACD Signal Length | 9 | Periode der EMA, die den MACD zur Signallinie glättet. |
| RSI Length | 14 | Glättungsperiode des Relative-Stärke-Index. |
| RSI Oversold | 30 | Marke, unterhalb derer der RSI als überverkauft gilt und ein Long erlaubt ist. |
| RSI Overbought | 70 | Marke, oberhalb derer der RSI als überkauft gilt und ein Short erlaubt ist. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Ein Indikatorbaustein enthält den MACD samt Signallinie; zwei Konverter holen die Werte Macd und Signal heraus, ein zweiter Indikatorbaustein berechnet den Relative-Stärke-Index auf denselben Kerzen.
- Zwei Vergleiche stellen die MACD-Linie der Signallinie gegenüber, zwei weitere den RSI den Schwellenkonstanten, und einer vergleicht die Position mit null.
- Jedes logische UND verbindet eine Trendbedingung, eine RSI-Bedingung und die Nullpositionsprüfung und löst dann einen Baustein aus, der nur aus der Neutralstellung eröffnet.
- Die Trendvergleiche dienen zugleich als Ausstiegsauslöser, sodass die beiden Schließbausteine ohne zusätzliche Logik auskommen. Die Pause von 150 Bars zwischen zwei Trades aus dem Original hat keine Entsprechung unter den Bausteinen und entfällt, wodurch Wiedereinstiege häufiger sind als im Code.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
