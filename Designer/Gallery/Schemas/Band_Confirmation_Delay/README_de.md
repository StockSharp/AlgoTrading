# Diagramm der Strategie Band Confirmation Delay
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Signal muss nicht in der Sekunde gehandelt werden, in der es erscheint. Dieses Diagramm legt ein Volatilitätsband über einen gleitenden Durchschnitt, und wenn der Schlusskurs darüber hinausschiebt, kauft das Diagramm nicht. Es schaltet stattdessen einen Delay-Block scharf, wartet eine feste Anzahl von Kerzen ab und stellt erst dann dieselbe Frage erneut: Liegt der Kurs immer noch über dem Band? Die Antwort in diesem Augenblick entscheidet über den Handel, nicht jene, die das Warten ausgelöst hat.

![schema](schema.svg)

## Strategieübersicht

- Eine einzige Kerzenserie von fünf Minuten, ausschließlich abgeschlossene Kerzen, speist alles im Diagramm.
- Ein gleitender Durchschnitt liefert die Mittellinie und eine Standardabweichung die Breite; eine Formel setzt beides zum oberen Band zusammen: Mittellinie plus Multiplikator mal Abweichung.
- Zwei Vergleiche reduzieren das gesamte Bild auf zwei Fragen: Liegt der Schlusskurs über dem Band, und liegt der Schlusskurs unter der Mittellinie?
- Eine logische Bedingung verknüpft den Ausbruch mit einer Position, die nicht long ist, und schaltet den Delay-Block für den Einstieg scharf. Während der Block zählt, werden weitere Ausbrüche ignoriert, sodass ein Warten nie durch die nächste Kerze neu gestartet wird.
- Wenn der Delay-Block zu Ende gezählt hat, gibt er einen einzelnen Impuls aus, und eine zweite logische Bedingung verbindet diesen Impuls mit einem frisch neu berechneten Ausbruch, einer abgelaufenen Abkühlphase und einer weiterhin flachen Position. Ohne diese zweite Bedingung würde das Diagramm blind kaufen, im Vertrauen auf ein Signal, das längst verschwunden sein könnte.
- Der Ausstieg ist spiegelbildlich genauso aufgebaut: Ein Schlusskurs unter der Mittellinie bei bestehender Long-Position schaltet den zweiten Delay-Block scharf, und dessen Impuls schließt den Handel, sobald er mit einer weiterhin gültigen Rückkehr unter die Mittellinie verknüpft wird.
- Parallel läuft ein Abkühlzähler: Jede eigene Ausführung setzt ihn auf null, jede Kerze erhöht ihn bis zu seiner Obergrenze, und ein Vergleich hält neue Einstiege fern, bis genügend Kerzen vergangen sind.
- Ein- und Ausstiege sind Market-Orders über Position modify-Blöcke, die aus der flachen Position eröffnen und die gesamte Position schließen; das Chart-Panel zeichnet die Kerzen, beide Indikatoren, die Orders und die Ausführungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs schließt über dem oberen Band, während die Position nicht long ist. Das schaltet den Einstiegs-Delay scharf. Eine konfigurierte Anzahl von Kerzen später gibt der Delay seinen Impuls frei, und wenn der Schlusskurs in diesem Moment immer noch über dem Band liegt, die Abkühlphase seit der letzten Ausführung abgelaufen ist und die Position weiterhin flach ist, kauft der Position modify-Block das Ordervolumen zum Marktpreis.
- **Short-Einstieg**: Short-Einstiege gibt es nicht. Unterhalb des Bandes hält sich das Diagramm einfach heraus; die einzige Order, die es jemals gegen eine Long-Position sendet, ist diejenige, die sie schließt.
- **Ausstieg**: Der Schlusskurs schließt unter der Mittellinie, während eine Long-Position offen ist, was den Ausstiegs-Delay scharf schaltet. Eine konfigurierte Anzahl von Kerzen später trifft der Impuls ein, und wenn der Schlusskurs weiterhin unter der Mittellinie liegt und die Position weiterhin long ist, verkauft der schließende Position modify-Block die gesamte Position zum Marktpreis. Es gibt weder einen Stop-Loss- noch einen Take-Profit-Block: In diesem Diagramm geht es darum, eine Bestätigung abzuwarten, und die Mittellinie ist das Einzige, was einen Handel beendet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der einzigen Kerzenserie, auf der das gesamte Diagramm läuft. |
| Middle Line Length | 40 | Länge des gleitenden Durchschnitts, der die Mittellinie und damit eine Hälfte des Bandes zeichnet. |
| Deviation Length | 40 | Länge der Standardabweichung, die die Bandbreite bestimmt; üblicherweise gleich der Länge der Mittellinie gehalten. |
| Band Multiplier | 1.1 | Wie viele Abweichungen über der Mittellinie das obere Band liegt. Höher setzen für seltenere, weiter gestreckte Ausbrüche. |
| Entry Confirm Candles | 3 | Kerzen, die der Einstiegs-Delay zwischen dem Ausbruch und der erneuten Prüfung zählt, die kaufen darf. |
| Exit Confirm Candles | 3 | Kerzen, die der Ausstiegs-Delay zwischen der Rückkehr unter die Mittellinie und der erneuten Prüfung zählt, die schließen darf. |
| Cooldown Candles | 8 | Kerzen, die nach einer eigenen Ausführung vergehen müssen, bevor ein neuer Einstieg erlaubt ist. |
| Order Volume | 1 | Ordergröße in Lots, die beim Einstieg gesendet wird; der Ausstieg schließt immer alles, was offen ist. |

## Diagrammdetails

- Der Delay-Block zählt Werte, die ihn nach dem Scharfschalten erreichen, nicht aufeinanderfolgende wahre Kerzen. Ein erneutes Scharfschalten wird ignoriert, solange er zählt, und der Block setzt sich selbst zurück, sobald er ausgelöst hat; das Warten ist damit eine echte Pause und keine laufende Zählung guter Bars.
- Genau deshalb wird der freigegebene Impuls mit einem frischen Vergleich kombiniert, statt direkt zur Order zu gehen. Der Impuls sagt, dass das Warten vorbei ist; der Vergleich sagt, ob der Grund dafür noch besteht.
- Die Bestätigungslänge ist hier bewusst länger als eine Kerze, damit der Delay-Block sichtbar etwas zu tun hat. Setzt man beide Verzögerungen auf eine Kerze, handelt das Diagramm auf der Ausbruchskerze selbst, also dieselbe Struktur ohne die Pause.
- Der Kerzenblock gibt ausschließlich abgeschlossene Kerzen aus. Ein Update einer sich bildenden Kerze trägt die Zeit, zu der der Bar eröffnet wurde, und eine daraus gebaute Order ist hinter der Uhr datiert und wird abgelehnt.
- Die Abkühlphase ist ein Zähler, kein Timer: eine Variable, die ihren Wert zwischen den Kerzen hält, von eigenen Ausführungen zurückgesetzt und von einer auf die Abkühllänge begrenzten Formel erhöht wird, sodass sie nicht davonlaufen und Einstiege niemals dauerhaft blockieren kann.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
