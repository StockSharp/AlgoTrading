# Strategiediagramm „Two Symbol Vote Panel“
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Abstimmungspanel über zwei Instrumente. Zu jeder abgeschlossenen Stunde wird jedes Instrument an sieben Preiswerten gegen seine eigene Vorstunde gemessen – Eröffnung, Hoch, Tief, Schluss, Mittelpunkt, typischer Preis und gewichteter Schlusskurs – und jeder Wert, der gestiegen ist, gibt eine Stimme nach oben ab, jeder gefallene eine Stimme nach unten. Die beiden Punktzahlen werden zu einem einzigen Votum addiert, und das gehandelte Instrument wird allein auf dieses Votum hin gekauft oder verkauft. Im gesamten Diagramm kommt kein Indikator vor: Die vollständige Entscheidung entsteht aus Kerzenpreisen, Arithmetik und Vergleichen.

![schema](schema.svg)

## Strategieübersicht

- Zwei Kerzenblöcke speisen das Diagramm. Der eine läuft auf dem eigenen Instrument der Strategie und ist dasjenige, das gehandelt wird; der andere wird über eine Security-Variable auf ein benanntes Instrument gerichtet, sodass das zweite Symbol eine Einstellung ist und keine Verdrahtungsentscheidung.
- Beide Reihen nehmen ausschließlich abgeschlossene Kerzen. Eine Stimme, die aus einer noch entstehenden Kerze gezählt wird, würde sich innerhalb der Stunde mehrfach ändern und die daraus folgende Order auf die Eröffnung dieser Stunde datieren.
- In jeder Reihe hält Previous value die gesamte Kerze einen Schritt zurück, und Konverter lesen Eröffnung, Hoch, Tief und Schluss aus der aktuellen und aus der gehaltenen Kerze. Zuerst wird die Kerze gehalten und danach werden ihre Felder gelesen – das ist die Reihenfolge, die auf jeder Kerze einen Wert liefert.
- Diese acht Zahlen gehen in je einen Formula-Block pro Instrument. Sieben sign()-Terme, einer je Preiswert, die jeweils +1, 0 oder -1 zurückgeben, werden addiert: Das Ergebnis sind die Stimmen nach oben minus die Stimmen nach unten und liegt zwischen -7 und +7. sign ist das, was einen Vergleich innerhalb einer Formel überhaupt möglich macht, denn eine Formel kann von sich aus kein Wahr oder Falsch zurückgeben.
- Die Punktzahl des Referenzinstruments wird durch eine Variable, bei der input-as-trigger abgeschaltet ist, auf die gehandelte Kerze abgetastet: Die Punktzahl trifft am Eingang ein und wartet dort, die gehandelte Kerze trifft am Trigger ein und lässt sie heraus. Zwei Datenströme takten nie im selben Augenblick, und genau das setzt das zweite Instrument auf den Takt des gehandelten Instruments.
- Ein zweiter Formula-Block addiert die beiden Punktzahlen zum Votum des Panels, zwischen -14 und +14, und das Votum wird zusammen mit beiden Punktzahlen, aus denen es besteht, im Chart unter den Kerzen gezeichnet.
- Ein einziger Schwellenwert regiert beide Seiten. Ein Vergleich prüft das Votum gegen ihn, eine einzeilige Formel dreht dessen Vorzeichen um, und ein zweiter Vergleich prüft das Votum dagegen – wer die Einstellung erhöht, verschärft den Long- und den Short-Fall um denselben Betrag.
- Die Position wird über eine Variable auf die Kerze aufgesetzt und auf drei Arten mit null verglichen: flach lässt einen Einstieg zu, long oder short einen Ausstieg. Vier AND-Gatter steuern vier Position modify-Blöcke – zwei öffnen, zwei schließen – und ein zweites Chart-Panel zeichnet das Referenzinstrument neben dem gehandelten.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Auf einer abgeschlossenen Kerze liegt das Votum des Panels über dem Schwellenwert – die beiden Instrumente geben zusammen eine klare Mehrheit ihrer vierzehn Stimmen nach oben ab – und die Position ist flach. Das Long-Gatter feuert, und Position modify kauft das Ordervolumen zum Marktpreis unter der Bedingung Open position, sodass eine folgende Kerze, die das Votum wiederholt, der Position nichts hinzufügt.
- **Short-Einstieg**: Der Spiegelfall: Das Votum liegt unter dem negierten Schwellenwert, die Mehrheit der vierzehn Stimmen zeigt nach unten, und die Position ist flach. Das Short-Gatter feuert, und der zweite Position modify-Block verkauft das Ordervolumen zum Marktpreis, wiederum unter der Bedingung Open position.
- **Ausstieg**: Es gibt weder Stop-Loss noch Take-Profit. Was die Position geöffnet hat, gibt sie auch wieder zurück: Ein Votum unter dem negierten Schwellenwert bei long stehender Position feuert das Ausstiegsgatter, und Close position gibt die gesamte Position zum Marktpreis zurück; ein Votum über dem Schwellenwert bei short stehender Position tut dasselbe auf der anderen Seite. Da die schließenden Blöcke schließen, was offen ist, statt eine feste Größe zu verkaufen, kann ein Ausstieg die Position niemals drehen – ein Umschwenken des Panels lässt das Diagramm zuerst flach zurück, und die nächste Kerze, die weiterhin in dieselbe Richtung liest, eröffnet die neue Seite.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Reference Instrument | TONUSDT@BNBFT | Das zweite Instrument, dessen Kerze die andere Hälfte der Stimmen abgibt; es muss über die Verbindung zusammen mit dem gehandelten verfügbar sein. |
| Traded Candles | 01:00:00 | Zeitrahmen der gehandelten Kerzen: der Takt, in dem das Votum gelesen und Orders gesendet werden. |
| Reference Candles | 01:00:00 | Zeitrahmen der Referenzkerzen. Halten Sie ihn gleich dem der gehandelten, sonst werden die beiden Punktzahlen über verschieden lange Zeiträume gezählt und die Summe bedeutet nicht mehr das, was sie besagt. |
| Vote Threshold | 2 | Wie groß die Mehrheit sein muss, die die beiden Instrumente abgeben, bevor eine Position eröffnet wird, und wie weit das Votum in die andere Richtung ausschlagen muss, bevor sie zurückgegeben wird. Gezählt wird in Stimmen, von den vierzehn, die das Panel abgibt. |
| Order Volume | 1 | Ordergröße, in Lots. |

## Diagrammdetails

- Jeder Order-Block ist so eingestellt, dass er keine Online-Verbindung verlangt, damit sich das Diagramm auf Historie genauso verhält wie auf einem Live-Datenstrom; in der Voreinstellung würde der Block auf wiedergegebenen Daten jede Transaktion zurückhalten.
- Die Einstiege tragen die Bedingung Open position und die Ausstiege die Bedingung Close position mit der entgegengesetzten Seite. Ohne Bedingung würde ein Block auf jede Positionsänderung hin handeln, durch die er ausgelöst wird, und aus einem Votum würde ein Strom von Orders.
- Jede Konstante – die Null, der Schwellenwert und das Ordervolumen – wird von der gehandelten Kerze getriggert. Eine Variable gibt auf ihren Trigger hin aus und nicht dann, wenn ihr Wert gesetzt wird; eine ungetriggerte Konstante würde den Vergleich daneben also über den gesamten Lauf stumm lassen.
- Ein Preiswert, der sich exakt wiederholt, gibt überhaupt keine Stimme ab: sign gibt bei unverändertem Wert null zurück, sodass nur echte Bewegung gezählt wird und eine unbewegte Stunde auf dem einen Instrument die Entscheidung schlicht dem anderen überlässt.
- Der Schwellenwert ist das, was das Panel von einem überempfindlichen Auslöser zu einem Filter macht. Bei null handelt das Diagramm auf die geringste Mehrheit hin und dreht fast auf jeder Kerze; erhöht wartet es, bis die beiden Instrumente deutlich genug übereinstimmen, und die Position wird durch die Uneinigkeiten dazwischen hindurch gehalten.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
