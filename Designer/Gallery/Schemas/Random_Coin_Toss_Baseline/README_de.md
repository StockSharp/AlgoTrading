# Diagramm der Random-Coin-Toss-Baseline-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt bewusst ohne Marktsignal. Immer wenn bei neutraler Position eine abgeschlossene Vier-Stunden-Kerze eintrifft, entscheidet ein Zufallswert zwischen einem Long- und einem Short-Einstieg. Die Position wird über zehn weitere abgeschlossene Kerzen gehalten, zum Markt geschlossen, und auf der folgenden Kerze kann der Zyklus erneut beginnen. Es ist eine lehrreiche Basislinie zum Vergleich regelbasierter Systeme und keine Strategie für den Live-Handel.

![schema](schema.svg)

## Strategieübersicht

- Ein einziger Strom ausschließlich abgeschlossener Vier-Stunden-Kerzen taktet sowohl die Zufallsentscheidungen als auch den Zähler der Haltedauer.
- Der Random-Baustein erzeugt einen Wert zwischen null und eins. Eine Schwelle von 0.5 teilt den Bereich in zwei sich gegenseitig ausschließende Richtungen.
- Die aktuelle Position wird beim Eintreffen jeder abgeschlossenen Kerze erfasst und mit null verglichen. Beide Einstiegsbausteine verwenden außerdem die Bedingung Open position, sodass ein Trade nur beginnen kann, wenn das Diagramm beim Eintreffen der Kerze keine Position hatte.
- Der Einstiegstrade aktiviert einen N-values-Baustein, der zehn nachfolgende abgeschlossene Kerzen zählt, bevor die Position geschlossen werden darf.
- Das Diagramm verwendet weder Indikatoren noch Stop-Loss oder Take-Profit. Seine Zufallsfolge wird innerhalb des Diagramms nicht initialisiert und kann sich zwischen den Läufen unterscheiden.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Zufallswert liegt unter der Münzwurfschwelle und die Position ist neutral. Das Diagramm kauft das eingestellte Volumen zum Marktpreis.
- **Short-Einstieg**: Der Zufallswert liegt auf oder über der Münzwurfschwelle und die Position ist neutral. Das Diagramm verkauft das eingestellte Volumen zum Marktpreis.
- **Ausstieg**: Nach der Ausführung eines Einstiegs zählt das Diagramm zehn nachfolgende abgeschlossene Vier-Stunden-Kerzen. Danach löst der N-values-Baustein das Schließen der gesamten Position zum Marktpreis aus. Auf der Ausstiegskerze wird kein neuer Trade eröffnet; die nächste abgeschlossene Kerze bietet die erste neue Einstiegsmöglichkeit.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Hold Bars | 10 | Anzahl der nach der Ausführung eines Einstiegs gezählten abgeschlossenen Kerzen bis zum Schließen der Position; der Wert muss größer als null sein. |
| Volume | 1 | Volumen der Ein- und Ausstiegsorders in Lots. Dasselbe eingestellte Volumen wird zum Öffnen und Reduzieren der Position verwendet. |
| Coin Threshold | 0.5 | Zufallswerte unterhalb dieser Schwelle wählen einen Long-Einstieg; Werte auf oder oberhalb der Schwelle wählen einen Short-Einstieg. |
| Candles | 04:00:00 | Vier-Stunden-Zeiteinheit für Einstiegsentscheidungen und das Zählen der Haltedauer; nur abgeschlossene Kerzen werden verarbeitet. |

## Diagrammdetails

- Der Ausgang des [Candles](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Bausteins speist den [Random](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/random.html)-Baustein, löst die Momentaufnahme der Position aus, speist den Eingang Input des [N values](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)-Bausteins und erreicht die Chartanzeige.
- Ein [Comparison](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Baustein prüft, ob der Zufallswert mindestens der Münzwurfschwelle entspricht. Dieses Signal wählt den Short-Zweig, während ein [Logical condition](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Baustein im NOT-Modus den Long-Zweig erzeugt.
- Der Ausgang des [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html)-Bausteins speist einen durch einen Trigger ausgelösten Variable-Baustein, der die Position beim Eintreffen der Kerze festhält. Die Momentaufnahme wird mit einer gemeinsamen Nullkonstante verglichen, und ihr Signal für die neutrale Position wird in jedem Einstiegs-AND mit dem Richtungssignal verbunden. Diese Momentaufnahme verhindert, dass die Ausstiegskerze unmittelbar nach der Ausführung des Ausstiegs eine neue Position eröffnet.
- Beide [Modify position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Einstiegsbausteine verwenden Marktorders mit der Bedingung Open position und beziehen ihr Volumen aus einer gemeinsamen Konstante.
- Die MyTrade-Ausgänge der Long- und Short-Einstiegsbausteine speisen den Eingang Trigger des N-values-Bausteins. Weitere Trigger werden ignoriert, solange sein Zehn-Kerzen-Zähler aktiv ist.
- Der Ausgang von N values löst zwei Modify-position-Bausteine im Modus Reduce only aus. Der Verkaufsbaustein kann nur eine Long-Position reduzieren und der Kaufbaustein nur eine Short-Position; beide erhalten das gemeinsame Volumen, sodass nur der jeweils zutreffende Zweig eine Ausstiegsorder registriert.
- Das [Chart panel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) erhält den Kerzenstrom und die von den beiden Einstiegs- und den beiden Ausstiegsbausteinen erzeugten Trades.

## Verwendung

Importieren Sie die `.json`-Datei in Designer und führen Sie sie im Backtester mit historischen Daten aus. Vergleichen Sie anschließend ihre Ergebnisse mit regelbasierten Diagrammen auf demselben Instrument und Zeitraum. Verwenden Sie dieses Beispiel als lehrreiche Basislinie und nicht als Live-Handelssystem.
