# Diagramm der Doppelkreuzungsstrategie aus RSI und Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei Oszillatoren müssen sich auf derselben Kerze einig sein. Gekauft wird nur, wenn der RSI unter 30 fällt und der Williams %R zugleich unter -80 rutscht, verkauft nur, wenn der RSI über 70 steigt und der Williams %R zugleich über -20 klettert. Ein Wert, der bloß in der Zone liegt, genügt nicht: Auf der Vorkerze mussten beide noch außerhalb sein, deshalb wird jeder Oszillator zusätzlich eine Kerze zurück gehalten. Die Pause von 180 Balken aus dem Originalcode ist nicht übernommen, denn auf Fünf-Minuten-Kerzen würde sie die Strategie nach jedem Trade fünfzehn Stunden lang stilllegen.

![schema](schema.svg)

## Strategieübersicht

- RSI 14 und Williams %R 14 werden auf denselben Fünf-Minuten-Kerzen eines einzigen Instruments berechnet.
- Bausteine für den vorherigen Wert halten beide Oszillatoren eine Kerze zurück, sodass ein frischer Eintritt in die Zone von einem Wert unterschieden wird, der dort schon stundenlang liegt.
- Eingestiegen wird nur aus der Neutralstellung, und die RSI-Mittellinie bei 50 führt die Position wieder dorthin zurück.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der RSI liegt unter der überverkauften Marke und lag auf der Vorkerze noch auf oder über ihr, und der Williams %R liegt unter seiner überverkauften Marke und lag auf der Vorkerze noch auf oder über ihr; die Position ist neutral. Es wird ein Lot zum Marktpreis gekauft.
- **Short-Einstieg**: Der RSI liegt über der überkauften Marke und lag auf der Vorkerze noch auf oder unter ihr, und der Williams %R liegt über seiner überkauften Marke und lag auf der Vorkerze noch auf oder unter ihr; die Position ist neutral. Es wird ein Lot zum Marktpreis verkauft.
- **Ausstieg**: Ein Long wird geschlossen, sobald der RSI wieder über die Mittellinie 50 steigt, ein Short, sobald der RSI wieder darunter fällt; beide Ausstiege sind Bausteine zum Schließen der Position und rühren nur an die tatsächlich offene Seite.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| RSI Length | 14 | Glättungsperiode des Relative-Stärke-Index. |
| RSI Oversold | 30 | Marke, die der RSI nach unten durchbrechen muss, damit ein Kaufsignal entsteht. |
| RSI Overbought | 70 | Marke, die der RSI nach oben durchbrechen muss, damit ein Verkaufssignal entsteht. |
| Williams %R Length | 14 | Rückschauperiode des Williams %R. |
| Williams %R Oversold | -80 | Marke, die der Williams %R für einen Kauf nach unten durchbrechen muss; der Indikator läuft von -100 bis 0. |
| Williams %R Overbought | -20 | Marke, die der Williams %R für einen Verkauf nach oben durchbrechen muss. |
| RSI Midline | 50 | Neutrale RSI-Marke, bei der eine offene Position aufgegeben wird. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Jeder Oszillator speist ein Paar Vergleiche, einen mit dem aktuellen und einen mit dem vorherigen Wert; so wird der Durchbruch einer Marke ohne Kreuzungsbaustein beschrieben, der die beiden Durchbrüche von verschiedenen Kerzen zusammenführen könnte.
- Jedes logische UND sammelt fünf Flags: die beiden RSI-Vergleiche, die beiden Williams-%R-Vergleiche und die neutrale Position aus dem Vergleich des Positionsbausteins mit null.
- Beide Einstiegsbausteine eröffnen nur, wenn keine Position besteht, und beziehen ihr Volumen aus einer gemeinsamen Konstante.
- Zwei weitere Vergleiche beobachten den RSI an seiner Mittellinie und steuern die Bausteine zum Schließen der Position, den einzigen Ausstieg des Diagramms.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
