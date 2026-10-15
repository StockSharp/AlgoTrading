# Diagramm der Tape-Reader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Kerze sagt, was während eines Bars geschehen ist; das Tape sagt, wie es geschehen ist. Dieses Diagramm abonniert den Strom der ausgeführten Abschlüsse, misst jeden Abschluss an der durchschnittlichen Größe der letzten hundert Abschlüsse und behandelt einen Abschluss, der ein Mehrfaches dieses Durchschnitts beträgt, als Fußabdruck von jemandem, der es eilig hat. Die Ablesung erfolgt einmal je fertiggestellter Kerze, sodass ein Strom, der tausende Male am Tag feuert, dennoch eine Entscheidung pro Bar hervorbringt.

![schema](schema.svg)

## Strategieübersicht

- Der Tick-Strom trägt beide Hälften des Signals: ein Konverter liest die Größe jedes ausgeführten Abschlusses, ein zweiter dessen Preis.
- Ein gleitender Durchschnitt über die letzten hundert Abschlussgrößen gibt dem Diagramm eine laufende Vorstellung davon, wie ein gewöhnlicher Trade auf diesem Instrument aussieht, sodass nichts darin an ein bestimmtes Preisniveau oder eine bestimmte Kontraktgröße gebunden ist.
- Eine Formel teilt die Größe jedes Abschlusses durch diesen Durchschnitt und macht daraus ein schlichtes Vielfaches: eins ist ein gewöhnlicher Abschluss, fünf ein Abschluss vom Fünffachen der jüngsten Norm.
- Eine Variable hält dieses Vielfache, eine zweite den Preis des Abschlusses, bis die Kerze schließt. Die Kerze ist ihr Auslöser, und genau das bringt eine Messung im Tick-Takt und eine Entscheidung im Kerzen-Takt auf dieselbe Uhr.
- Ein Vergleich mit dem Size Factor beantwortet, ob der Abschluss ein großer war. Der Schlusskurs der vorherigen Kerze, über einen Previous value-Block und einen Konverter geholt, beantwortet, in welche Richtung er ging.
- Der Position-Block wird dreimal mit Null verglichen: ein Test auf flache Position für die Einstiege sowie je ein Long- und ein Short-Test für die Ausstiege.
- Strategy trades meldet jede eigene Ausführung und setzt einen Kerzenzähler zurück, der das Diagramm nach jeder Ausführung, ob Einstieg oder Ausstieg, für eine festgelegte Anzahl Kerzen vom Markt fernhält.
- Beide Einstiege sind Position modify-Blöcke im Modus „nur öffnen“, der Ausstieg ist ein dritter im Modus „schließen“, sodass immer nur eine Position gehalten wird und sie glattgestellt wird, bevor die Gegenseite eingegangen werden kann.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Abschluss, der zuletzt vor dem Schließen der Kerze durchging, betrug mindestens das Size Factor-fache der durchschnittlichen Abschlussgröße, sein Preis liegt über dem Schlusskurs der vorherigen Kerze, die Position ist flach und die Abkühlphase ist abgelaufen. Position modify kauft das Order Volume zum Marktpreis.
- **Short-Einstieg**: Derselbe große Abschluss, jedoch mit einem Preis unter dem Schlusskurs der vorherigen Kerze, wiederum aus flacher Position und mit abgelaufener Abkühlphase. Position modify verkauft das Order Volume zum Marktpreis.
- **Ausstieg**: Es gibt weder Take-Profit noch Stop-Loss. Eine Long-Position wird geschlossen, wenn ein großer Abschluss unterhalb des vorherigen Schlusskurses durchgeht, während die Position long ist; eine Short-Position schließt ein großer Abschluss oberhalb davon. Beide Ausstiegsbedingungen laufen in einer Combination zusammen, die den auf Schließen gestellten Position modify-Block auslöst; die Ausführung, die die Position glattstellt, startet auch die Abkühlphase neu, sodass der nächste Einstieg dieselbe Anzahl Kerzen abwartet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzen, auf denen das gesamte Diagramm arbeitet; jede Ablesung des Tapes erfolgt beim Schließen einer von ihnen. |
| Average Prints | 100 | Wie viele der jüngsten Abschlüsse die durchschnittliche Größe bilden, an der das Vielfache gemessen wird. Ein kürzeres Fenster folgt einem Wechsel der Aktivität schneller und macht den Durchschnitt selbst sprunghafter. |
| Size Factor | 5 | Das Wievielfache der durchschnittlichen Größe ein Abschluss erreichen muss, um als großer zu gelten. Erhöhen Sie den Wert für seltenere und selektivere Signale; senken Sie ihn, wenn das Tape ruhig ist und nichts infrage kommt. |
| Cooldown Bars | 3 | Wie viele fertiggestellte Kerzen nach einer Ausführung vergehen müssen, bevor der nächste Einstieg erlaubt ist. |
| Order Volume | 1 | Größe jeder Einstiegsorder, in Instrumenteneinheiten. Die schließende Order bemisst sich an der offenen Position und liest diesen Wert nicht. |

## Diagrammdetails

- Das Maß ist ein Verhältnis und keine Kontraktzahl, daher liest sich derselbe Size Factor auf einem Instrument, das in Bruchteilen einer Einheit notiert, genauso wie auf einem, das in ganzen Lots notiert.
- Der Durchschnitt schließt den Abschluss ein, an dem er gemessen wird, sodass ein einzelner sehr großer Abschluss seinen eigenen Maßstab ein wenig anhebt; bei hundert Abschlüssen im Fenster liegt ein Abschluss vom Fünffachen der Norm immer noch über vier.
- Gelesen wird der Abschluss, der als letzter vor dem Schließen der Kerze eintraf. Die Variable, die ihn hält, steht zwischen einem Strom, der tausende Male am Tag feuert, und einer Entscheidung, die einmal je Bar fallen soll; ohne sie würden Größentest und Preistest nach verschiedenen Uhren antworten und niemals übereinstimmen.
- Der Abkühlzähler wird vom Strategy trades-Block zurückgesetzt und nicht von den Einstiegsblöcken, sodass auch eine schließende Ausführung die Wartezeit startet. Ohne das lägen der Ausstieg des einen Trades und der Einstieg des nächsten auf benachbarten Kerzen.
- Das Chart-Panel zeichnet die Kerzen, die durchschnittliche Abschlussgröße, das gehaltene Vielfache und die Size Factor-Linie, die Einstiegs- und Ausstiegsorders sowie jede eigene Ausführung, sodass der Abschluss, der einen Trade ausgelöst hat, neben der zugehörigen Kerze zu finden ist.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
