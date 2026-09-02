# Diagramm der Reversion-Strategie mit VWMA und RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein volumengewichteter gleitender Durchschnitt zeigt, wo das Geld tatsächlich gehandelt hat, und der RSI sagt, ob die Entfernung davon übertrieben wurde. Das Diagramm kauft unter dem Durchschnitt nur bei überverkauftem RSI, verkauft darüber nur bei überkauftem RSI und hält den Trade, bis der Kurs auf die andere Seite des Durchschnitts zurückkehrt.

![schema](schema.svg)

## Strategieübersicht

- Der Durchschnitt ist ein gleitender VolumeWeightedMovingAverage über 32 Kerzen, kein Sitzungs-VWAP. Trotz des Namens ist es genau der Indikator der ursprünglichen Strategie: Er gewichtet jeden Schlusskurs mit dem Volumen seiner Kerze.
- Der Relative-Stärke-Index wird auf Schlusskursen berechnet und bestätigt den Einstieg nur; von sich aus eröffnet er nichts.
- Beide Indikatorbausteine geben ausschließlich fertig gebildete Werte aus, was verhindert, dass auf dem unvollständigen Durchschnitt der ersten Kerzen gehandelt wird.
- Das Original verarbeitet nach jedem Trade 100 Kerzen lang nichts, wodurch auch der Ausstieg einfriert und eine Position mindestens acht Stunden gehalten wird. Designer kennt keinen Sperrzähler, deshalb wird diese Pause nicht nachgebildet: Hier wird geschlossen, sobald der Kurs den Durchschnitt wieder kreuzt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt unter der VWMA, der RSI unter der überverkauften Marke und die Position ist neutral. Die Order kauft das eingestellte Volumen.
- **Short-Einstieg**: Der Schlusskurs liegt über der VWMA, der RSI über der überkauften Marke und die Position ist neutral. Die Order verkauft das eingestellte Volumen.
- **Ausstieg**: Ein Long wird geschlossen, sobald der Schlusskurs wieder über die VWMA steigt, ein Short, sobald er wieder darunter fällt. Stop-Loss und Take-Profit gibt es wie im Original nicht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| VWMA Length | 32 | Anzahl der Kerzen im volumengewichteten gleitenden Durchschnitt. |
| RSI Length | 14 | Glättungsperiode des Relative-Stärke-Index. |
| Oversold | 30 | Marke, unter der der Index als überverkauft gilt. |
| Overbought | 70 | Marke, über der der Index als überkauft gilt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den volumengewichteten Durchschnitt direkt, da dieser Indikator das Kerzenvolumen benötigt, und den RSI über einen Konverter des Schlusskurses.
- Zwei Vergleichsbausteine legen fest, auf welcher Seite des Durchschnitts der Schlusskurs steht; dieselben zwei Signale bedienen Ein- und Ausstiege.
- Zwei weitere Vergleiche prüfen den RSI gegen die Schwellenkonstanten.
- Der Positionsbaustein wird dreimal mit null verglichen und liefert die Merkmale neutral, long und short für die logischen UND.
- Jedes Einstiegs-UND verknüpft drei Bedingungen — Seite des Durchschnitts, RSI-Extrem und neutrale Position — und löst einen Baustein mit der Bedingung Position eröffnen aus; die Ausstiege nutzen Bausteine mit der Bedingung Position schließen, die kein Volumen brauchen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
