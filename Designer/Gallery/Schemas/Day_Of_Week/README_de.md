# Diagramm der Strategie zum Wochentagseffekt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Kalender bestimmt die Richtung, der gleitende Durchschnitt den Zeitpunkt. Zu Wochenbeginn darf das Diagramm kaufen, zum Wochenende verkaufen, und in beiden Fällen wartet es, bis der Schlusskurs auf der passenden Seite eines einfachen gleitenden Durchschnitts liegt. Der Wochentag wird direkt aus der Kerze gelesen, es muss also kein Zustand von Kerze zu Kerze mitgeführt werden.

![schema](schema.svg)

## Strategieübersicht

- Ein Konverter liest den Wochentag als Zahl aus der Eröffnungszeit der Kerze, wobei Sonntag null und Samstag sechs ist.
- Jedes Kalenderfenster bilden zwei Vergleiche: Montag bis Dienstag für die Long-Seite, Donnerstag bis Freitag für die Short-Seite; die Grenzen sind Parameter, das Fenster lässt sich also verschieben oder verbreitern.
- Ein einfacher gleitender Durchschnitt des Schlusskurses bestätigt die Richtung; der Kalender allein eröffnet nie eine Position.
- Die aktuelle Position geht in beide Einstiege ein, sodass das Diagramm eine bestehende Position nie vergrößert.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Kerze fällt in das Fenster des Wochenbeginns, ihr Schluss liegt über dem einfachen gleitenden Durchschnitt und die Position ist neutral. Die Order kauft das gemeinsame Volumen zum Marktpreis.
- **Short-Einstieg**: Die Kerze fällt in das Fenster des Wochenendes, ihr Schluss liegt unter dem einfachen gleitenden Durchschnitt und die Position ist neutral. Die Order verkauft das gemeinsame Volumen zum Marktpreis.
- **Ausstieg**: Ein Schluss zurück unter den Durchschnitt schließt einen Long, ein Schluss zurück darüber einen Short, beides über Bausteine zur Positionsänderung im Schließmodus. Da ein Schließbaustein bei neutraler Position nichts tut, bildet das den Kreuzungstest des Originals ohne zusätzliche Bausteine nach. Das Original kennt zwei Zähler, die das Diagramm über Kerzen hinweg nicht halten kann, und beide entfielen: die Pause von dreihundert Balken nach jedem Trade und die Regel, die einen zweiten Einstieg am selben Wochentag verbietet. Ohne sie steigt das Diagramm wieder ein, sobald der Kurs innerhalb desselben Fensters auf die richtige Seite des Durchschnitts zurückkehrt, und handelt daher deutlich häufiger als das Original.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| MA Period | 20 | Länge des einfachen gleitenden Durchschnitts, der die Richtung bestätigt und die Trades schließt. |
| Long day from | 1 | Erster Wochentag des Long-Fensters als Zahl, Sonntag ist null. Eins ist Montag. |
| Long day to | 2 | Letzter Wochentag des Long-Fensters. Zwei ist Dienstag. |
| Short day from | 4 | Erster Wochentag des Short-Fensters. Vier ist Donnerstag. |
| Short day to | 5 | Letzter Wochentag des Short-Fensters. Fünf ist Freitag. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den gleitenden Durchschnitt und zwei Konverter, einen für den Schlusskurs und einen für den Wochentag der Eröffnungszeit.
- Vier Vergleiche legen den Wochentag innerhalb oder außerhalb der beiden Kalenderfenster fest, zwei weitere setzen den Schluss auf die eine oder andere Seite des Durchschnitts.
- Jedes logische UND verbindet beide Enden eines Fensters, die Seite des Durchschnitts und die Neutralprüfung, bevor es einen Einstiegsbaustein auslöst.
- Die beiden Schließbausteine hängen direkt an den Vergleichen mit dem Durchschnitt und tragen die Bedingung Position schließen, sodass jeder nur seine eigene Seite glattstellt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
