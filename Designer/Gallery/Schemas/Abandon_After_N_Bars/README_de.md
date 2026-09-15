# Diagramm der Strategie „Abandon After N Bars“
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei kurze gleitende Durchschnitte und ein RSI mit zwei Perioden entscheiden, wann ein Trade eröffnet wird, doch der eigentliche Punkt dieses Diagramms ist die Regel, die ihn beendet. Ein Trade, der innerhalb einer festen Anzahl von Kerzen weder sein Ziel noch seinen Stop erreicht hat, wird aufgegeben und zum Marktpreis geschlossen. Der Countdown wird von einem N values-Block gemessen, und er beginnt mit der Ausführung, die die Position tatsächlich eröffnet hat, nicht mit dem Signal, das sie angefordert hat.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen einen schnellen und einen langsamen exponentiellen gleitenden Durchschnitt, einen RSI mit zwei Perioden sowie einen Konverter, der den Schlusskurs aus jeder Kerze herauszieht.
- Zwei Vergleiche lesen den Trend aus den Durchschnitten ab: schnell über langsam und schnell unter langsam. Zwei weitere lesen das Momentum gegen eine Schwellenwert-Variable: RSI darunter und RSI darüber.
- Der Position-Block wird zweimal mit null verglichen, einmal auf Gleichheit und einmal auf Ungleichheit, sodass das Diagramm sowohl einen Test auf flache Position für die Einstiege als auch einen Test auf offene Position für die Abbruchregel besitzt.
- Jeder Einstieg ist eine logische UND-Verknüpfung dreier Signale — Trend, Momentum und flache Position —, und beide Einstiegsblöcke sind auf reines Eröffnen gestellt, sodass das Diagramm jeweils nur eine Position hält und sie niemals aufstockt.
- Position protection überwacht die Einstiegsausführungen und verwaltet den Trade mit einem prozentualen Take-Profit und einem Trailing-Stop-Loss, der dem Kurs folgt, sobald er sich zugunsten der Position bewegt.
- Strategy trades meldet jede eigene Ausführung. Ein Flag-Block, der bei flacher Position zurückgesetzt wird, lässt nur die erste Ausführung eines Trades durch — also die, die ihn eröffnet hat.
- Dieser einzelne Impuls schärft den N values-Block, der daraufhin abgeschlossene Kerzen zählt und auslöst, sobald die Anzahl erreicht ist.
- Das Abbruchsignal durchläuft ein zweites UND mit dem Test auf offene Position, bevor es einen auf Schließen gestellten Position modify-Block erreicht; so kann der Countdown immer nur einen Trade beenden, der noch läuft.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der schnelle Durchschnitt liegt über dem langsamen, der RSI unter seinem Schwellenwert und die Position ist flach. Die Kombination kauft Schwäche innerhalb eines steigenden kurzfristigen Trends. Position modify kauft das Ordervolumen zum Marktpreis, ausschließlich eröffnend.
- **Short-Einstieg**: Der schnelle Durchschnitt liegt unter dem langsamen, der RSI über seinem Schwellenwert und die Position ist flach — Stärke innerhalb eines fallenden kurzfristigen Trends. Position modify verkauft das Ordervolumen zum Marktpreis, ausschließlich eröffnend.
- **Ausstieg**: Es gibt zwei voneinander unabhängige Ausstiege. Position protection kann den Trade zuerst schließen, bei 1.2% Gewinn oder über einen Trailing-Stop 0.6% hinter dem besten erreichten Kurs. Geschieht keines von beidem, übernimmt die Abbruchregel: zwölf abgeschlossene Kerzen nach der eröffnenden Ausführung löst der N values-Block aus, der Test auf offene Position bestätigt, dass es noch etwas zu schließen gibt, und ein auf Schließen gestellter Position modify-Block stellt die gehaltene Seite glatt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzen, mit denen das gesamte Diagramm arbeitet; der Abbruch-Countdown wird in diesen Kerzen gemessen. |
| Fast EMA Length | 3 | Länge des schnellen exponentiellen gleitenden Durchschnitts. |
| Slow EMA Length | 7 | Länge des langsamen exponentiellen gleitenden Durchschnitts; halten Sie sie größer als die des schnellen, sonst verliert der Trendtest seinen Sinn. |
| RSI Length | 2 | Länge des RSI. Eine sehr kurze Länge lässt ihn häufig über den Schwellenwert schwingen, und genau das erzeugt die häufigen Einstiege. |
| RSI Threshold | 50 | Niveau, an dem der RSI gemessen wird. Eine Long-Position kauft darunter und eine Short-Position verkauft darüber; ein höherer Wert macht Long-Einstiege also häufiger und Short-Einstiege seltener. |
| Order Volume | 1 | Größe jeder Einstiegsorder, in Instrumenteinheiten. |
| Take Profit, % | 1.2 | Take-Profit-Abstand, in Prozent des Einstiegskurses. |
| Trailing Stop, % | 0.6 | Trailing-Stop-Abstand, in Prozent: Der Stop startet in diesem Abstand zum Einstieg und folgt dem besten erreichten Kurs, ohne je zurückzuweichen. |
| Bars Before Abandon | 12 | Wie viele abgeschlossene Kerzen ein Trade laufen darf, bevor er aufgegeben und zum Marktpreis geschlossen wird. Niedriger einstellen, um schneller aufzugeben; höher, um den Ausstieg dem Take-Profit und dem Trailing-Stop zu überlassen. |

## Diagrammdetails

- Der Countdown wird von einer Ausführung gestartet und nicht von einem Signal, sodass die Uhr misst, wie lange der Trade tatsächlich besteht, und nicht, wie lange es her ist, dass das Diagramm ihn haben wollte.
- Der Strategy trades-Block meldet auch die Ausstiege, die den Countdown sonst bei jeder schließenden Ausführung neu starten würden. Der Flag-Block verhindert das: Er wird nur bei flacher Position zurückgesetzt, sodass bei einem laufenden Trade dessen spätere Ausführungen ignoriert werden.
- Der Test auf offene Position am zweiten UND ist es, der einen bei flachem Konto ablaufenden Countdown harmlos macht — der Schließen-Block wird nur ausgelöst, wenn es eine Position zu schließen gibt.
- Beide Einstiegsblöcke senden ihre Ausführungen in eine Combination, sodass Position protection unabhängig von der eröffneten Seite über einen einzigen Eingang geschärft wird; der Schlusskurs speist denselben Block und gibt Take-Profit und Trailing-Stop auf jeder abgeschlossenen Kerze einen Wert, mit dem sie arbeiten können.
- Das Chart-Panel zeichnet die Kerzen, beide Durchschnitte, den RSI, die Einstiegs- und Abbruch-Orders, die Schutzorders und jede Ausführung, sodass ein aufgegebener Trade leicht von einem zu unterscheiden ist, der sein Ziel erreicht hat.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
