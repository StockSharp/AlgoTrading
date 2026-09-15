# Strategiediagramm Delayed Reversal Session
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm schiebt zwei Dinge zwischen Signal und Order: eine Wartezeit und eine Uhr. Um einen gleitenden Durchschnitt wird ein prozentualer Korridor gezogen; verlässt der Kurs diesen Korridor, gilt das als Umkehr. Die Umkehr wird nicht in dem Moment gehandelt, in dem sie auftritt. Sie wird an einen Verzögerungs-Baustein übergeben, der eine feste Anzahl abgeschlossener Kerzen zählt, und nur der Impuls, der am anderen Ende herauskommt, darf an die Order-Bausteine heran – und das auch nur, solange der Arbeitszeit-Baustein meldet, dass die Uhr innerhalb der Sitzung steht.

![schema](schema.svg)

## Strategieübersicht

- Eine einzige Kerzenreihe, dreißig Minuten, ausschließlich abgeschlossene Kerzen, treibt jeden Zweig des Diagramms an.
- Ein gleitender Durchschnitt liefert die Mittellinie; eine Konstante hält die Sensitivität, und zwei Formeln machen aus diesem Paar eine obere und eine untere Kante – der Durchschnitt plus bzw. minus dem Durchschnitt mal der Sensitivität.
- Zwei Kreuzungs-Bausteine beobachten den Schlusskurs gegen diese Kanten. Der eine löst aus, wenn der Schlusskurs die obere Kante von unten kreuzt; der andere ist umgekehrt verdrahtet, mit der unteren Kante am Aufwärts-Eingang, sodass er auslöst, wenn der Schlusskurs durch die untere Kante fällt.
- Jede Kreuzung schärft ihren eigenen Verzögerungs-Baustein. Die Verzögerung zählt die nach dem Scharfschalten eintreffenden abgeschlossenen Kerzen und gibt einen einzelnen Impuls ab, wenn der Zähler abgelaufen ist; das Signal wird also später umgesetzt und nicht auf der Kerze, die es erzeugt hat.
- Der Arbeitszeit-Baustein liest die Zeit jeder Kerze und meldet, ob der Zeitpunkt innerhalb der Sitzung liegt; ein logisches NICHT macht aus demselben Flag ein Flag für „außerhalb der Sitzung“.
- Ein logisches UND je Seite verknüpft den freigegebenen Impuls mit dem Sitzungs-Flag, sodass ein Impuls, der außerhalb der Arbeitszeit ankommt, verworfen und nicht in eine Warteschlange gestellt wird.
- Einstiege sind Market-Orders über Position modify-Bausteine, die nur bei flacher Position eröffnen; ein verzögertes Signal öffnet damit genau eine Position, und wiederholte Impulse in dieselbe Richtung können nicht aufpyramidisieren.
- Ein dritter Position modify-Baustein schließt, was offen ist, und wird von drei Quellen angesteuert: dem verzögerten Signal beider Seiten und dem Flag für „außerhalb der Sitzung“; das Chart-Panel zeichnet die Kerzen, den Durchschnitt, beide Korridorkanten, die Orders und die Ausführungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs kreuzt die obere Korridorkante von unten und schärft damit die Long-Verzögerung. Eine konfigurierte Anzahl abgeschlossener Kerzen später gibt die Verzögerung ihren Impuls frei; liegt die Uhr in diesem Moment innerhalb der Sitzung, kauft der Long-Baustein Position modify das Ordervolumen zum Marktpreis. Da der Baustein nur bei flacher Position eröffnet, entfällt der Kauf, wenn bereits eine Position offen ist – derselbe Impuls ist dann stattdessen schon an den schließenden Baustein gegangen.
- **Short-Einstieg**: Der Schlusskurs fällt durch die untere Korridorkante und schärft damit die Short-Verzögerung. Eine konfigurierte Anzahl abgeschlossener Kerzen später trifft der Impuls ein, und liegt die Uhr innerhalb der Sitzung, verkauft der Short-Baustein Position modify das Ordervolumen zum Marktpreis – erneut nur aus einer flachen Position heraus.
- **Ausstieg**: Zwei Dinge beenden einen Trade. Ein verzögertes Signal beider Seiten ist sowohl in den schließenden Baustein als auch in den eigenen Einstiegs-Baustein verdrahtet; eine zurückgehaltene Umkehr gegen eine offene Position stellt diese daher zum Marktpreis glatt, und der folgende Einstieg wartet auf das nächste Signal, weil die eröffnenden Bausteine nur bei flacher Position arbeiten. Das andere ist die Uhr: sobald das Arbeitszeit-Flag auf false geht, löst der NICHT-Baustein auf jeder Kerze aus, und der schließende Baustein stellt die Position glatt und hält sie flach, bis die Sitzung wieder öffnet. Bei flacher Position tut der schließende Baustein nichts. Es gibt hier weder einen Stop-Loss- noch einen Take-Profit-Baustein.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:30:00 | Zeitrahmen der einzigen Kerzenreihe, auf der alles im Diagramm läuft. |
| Average Length | 20 | Länge des gleitenden Durchschnitts, der die Mitte des Korridors zeichnet. |
| Corridor Sensitivity | 0.004 | Halbe Breite des Korridors als Anteil des Durchschnitts – der Vorgabewert sind vier Zehntel Prozent je Seite. Höhere Werte ergeben seltenere, weitere Umkehrungen; niedrigere Werte lassen die Kanten weit häufiger kreuzen. |
| Long Signal Delay | 2 | Abgeschlossene Kerzen, die zwischen der Aufwärtsumkehr und dem kaufberechtigten Impuls gezählt werden. Eins bedeutet die nächste Kerze; größere Werte halten das Signal länger zurück. |
| Short Signal Delay | 2 | Abgeschlossene Kerzen, die zwischen der Abwärtsumkehr und dem verkaufsberechtigten Impuls gezählt werden. |
| Session Start | 08:00:00 | Beginn der Arbeitssitzung. Vorher freigegebene Impulse werden verworfen, und das Diagramm bleibt flach. |
| Session End | 20:00:00 | Ende der Arbeitssitzung. Ab diesem Moment stellt das Flag „außerhalb der Sitzung“ auf jeder Kerze eine offene Position glatt, bis die Sitzung wieder öffnet. |
| Order Volume | 1 | Ordergröße in Lots, die beim Einstieg gesendet wird; der schließende Baustein stellt stets glatt, was offen ist. |

## Diagrammdetails

- Ein Kreuzungs-Baustein ist ein Ereignis, kein Zustand. Er meldet sich nur auf der Kerze, auf der die beiden Reihen die Plätze tauschen; jede Verzögerung wird deshalb einmal pro Umkehr geschärft und nicht auf jeder Kerze neu, die der Kurs außerhalb des Korridors verbringt.
- Der Kreuzungs-Baustein meldet die Gegenrichtung als Wert false, und sowohl der Scharfschalt-Eingang der Verzögerung als auch der Order-Auslöser ignorieren false; die Abwärtshälfte eines Kreuzungs-Bausteins kann den Aufwärtszweig daher weder schärfen noch auslösen.
- Solange eine Verzögerung zählt, wird ein zweites Scharfschalten ignoriert. Eine Serie von Umkehrungen erzeugt somit einen Impuls und keine Warteschlange davon, und der Zähler ist eine echte Pause und keine Strichliste.
- Der Kerzen-Baustein gibt ausschließlich abgeschlossene Kerzen aus. Ein Update einer sich bildenden Kerze trägt die Eröffnungszeit der Kerze, und eine daraus gebaute Order ist gegenüber der Uhr des Emulators rückdatiert und wird abgelehnt.
- Die Sitzung wird aus der Zeit der Kerze selbst gelesen und nicht von der Systemuhr; eine Wiedergabe verhält sich damit genau wie ein Live-Lauf, und dasselbe Diagramm lässt sich ohne jede Änderung im Backtest prüfen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
