# Diagramm der instrumentenübergreifenden MACD-Vergleichsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm handelt ein Instrument und entscheidet auf zweien. Ein MACD wird auf dem gehandelten Instrument und auf einem Referenzinstrument gemessen, und verglichen werden nicht deren Kurse, sondern deren Momentum: Ist die gehandelte Seite in der schnellen Ablesung die schwächere von beiden, in der langsamen aber weiterhin die stärkere, behandelt das Diagramm das als eine noch nicht geschlossene Lücke und kauft. Das Spiegelbild verkauft.

![schema](schema.svg)

## Strategieübersicht

- Der Block Index baut aus einem Ausdruck ein synthetisches Instrument und übergibt es an einen zweiten Kerzenblock — so gelangt ein zweites Instrument überhaupt erst in ein Diagramm.
- Beide Beine werden als Kerzenserien desselben Zeitrahmens abonniert: eine auf dem Strategieinstrument, eine auf dem Instrument, das Index erzeugt hat.
- Sync hält je Bein eine Leitung und lässt beide gemeinsam heraus, sodass eine Ablesung des gehandelten Instruments und eine Ablesung des Referenzinstruments immer denselben Balken beschreiben und nicht die Serie, die zufällig zuerst eintraf.
- Jedes Bein erhält seinen eigenen MACD, und Konverter ziehen drei Zahlen daraus: die MACD-Linie, die Signallinie und den Schlusskurs der darunterliegenden Kerze.
- Zwei Formeln je Bein machen daraus Prozentwerte des jeweils eigenen Kurses — das Histogramm, MACD minus Signal, und die Signallinie selbst. Ein Vergleich der Rohwerte wäre sinnlos, wenn die beiden Instrumente um Größenordnungen unterschiedlich bepreist sind.
- Vier Variablen lesen die vier Prozentwerte auf der gehandelten Kerze, sodass sowohl der Vergleich als auch die darauf folgende Order vom gehandelten Instrument getaktet werden und nicht von dem Bein, das zuletzt geschlossen hat.
- Vergleiche stellen die Beine nebeneinander — Histogramm gegen Histogramm, Signal gegen Signal —, und vier logische Bedingungen ergänzen den Positionszustand, ein Gate je Aktion: Long eröffnen, Short eröffnen, Long schließen, Short schließen.
- Einstiege sind Market-Orders mit festem Volumen und nur aus einer flachen Position heraus; die entgegengesetzte Ablesung schließt, und Position protection führt einen Take-Profit und einen Stop-Loss in Prozent des Einstiegskurses mit.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Auf einer gehandelten Kerze liegt das Histogramm des gehandelten Instruments unter dem Referenzhistogramm, während seine Signallinie über der Referenzsignallinie liegt, und die Position ist flach. Die schnelle Ablesung sagt, die gehandelte Seite liegt zurück, die langsame sagt, sie liegt noch vorn, und das Diagramm liest das Paar als einen aufzuholenden Rückstand. Position modify kauft das Ordervolumen zum Marktpreis.
- **Short-Einstieg**: Auf einer gehandelten Kerze liegt das Histogramm des gehandelten Instruments über dem Referenzhistogramm, während seine Signallinie unter der Referenzsignallinie liegt, und die Position ist flach. Position modify verkauft das Ordervolumen zum Marktpreis.
- **Ausstieg**: Es gibt zwei Auswege, und jeder von beiden kann zuerst kommen. Die spiegelbildliche Ablesung schließt die Position: eine Long-Position geht, wenn das Histogramm vor die Referenz zieht und die Signallinie hinter sie zurückfällt, eine Short-Position beim umgekehrten Paar. Jeweils ist es ein auf Schließen gesetztes Position modify, sodass das Volumen aus der Position selbst stammt und innerhalb einer Order nichts gedreht wird — das Diagramm kehrt auf flach zurück und wartet auf ein neues Signal. Unabhängig davon folgt Position protection jeder Einstiegsausführung und schließt bei 1.6% Gewinn oder 0.8% Verlust gegenüber dem Einstiegskurs.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Reference Security | TONUSDT@BNBFT * 1 | Ausdruck, aus dem der Block Index das Referenzinstrument baut. Er benennt ein Instrument und multipliziert es mit eins, was den Kurs unverändert lässt und die Einstellung ein arithmetischer Ausdruck bleiben lässt. |
| Traded Candles | 00:15:00 | Zeitrahmen der gehandelten Kerzenserie und der Takt, auf dem jede Order gebaut wird. |
| Reference Candles | 00:15:00 | Zeitrahmen der Referenz-Kerzenserie. Er muss mit dem gehandelten übereinstimmen, sonst fallen die beiden Beine nie in denselben Balken. |
| Sync Interval | 00:15:00 | Länge des Fensters, in das Sync die beiden Beine gruppiert; dieselbe Länge wie die Kerzen. |
| Traded MACD Fast | 12 | Länge des schnellen gleitenden Durchschnitts des MACD auf dem gehandelten Instrument. |
| Traded MACD Slow | 26 | Länge des langsamen gleitenden Durchschnitts desselben MACD. Der Abstand zur schnellen Länge entscheidet, wie lange eine Bewegung anhalten muss, bevor das Histogramm reagiert. |
| Traded MACD Signal | 9 | Länge der Signallinie desselben MACD; sie ist die Linie, gegen die das Histogramm gemessen wird. |
| Reference MACD Fast | 12 | Länge des schnellen gleitenden Durchschnitts des MACD auf dem Referenzinstrument. Jedes Bein trägt seine eigenen drei Längen, sodass beide getrennt eingestellt werden können; ein Vergleich der beiden Histogramme sagt jedoch nur dann etwas aus, solange sie gleich gesetzt sind. |
| Reference MACD Slow | 26 | Länge des langsamen gleitenden Durchschnitts des Referenz-MACD. Gleich der des gehandelten MACD halten, sofern die beiden Instrumente nicht bewusst über unterschiedliche Horizonte gemessen werden sollen. |
| Reference MACD Signal | 9 | Länge der Signallinie des Referenz-MACD. Aus demselben Grund gleich der des gehandelten MACD halten. |
| Order Volume | 1 | Ordergröße, in Lots. |
| Take Profit, % | 1.6 | Take-Profit-Abstand, in Prozent des Einstiegskurses. |
| Stop Loss, % | 0.8 | Stop-Loss-Abstand, in Prozent des Einstiegskurses. |

## Diagrammdetails

- Beide Kerzenblöcke nehmen nur fertige Kerzen. Ein Update einer sich bildenden Kerze trägt die Zeit, zu der der Balken geöffnet wurde, und eine daraus gebaute Order wäre älter als der Moment, in dem sie gesendet wird.
- Sync gibt einen Satz unter der frühesten Zeit der darin enthaltenen Werte frei; deshalb erreicht nichts aus Sync direkt eine Order: Die vier Variablen lesen die Werte auf der gehandelten Kerze erneut, und auf deren Takt werden die Orders gebaut. Der Preis dafür ist, dass ein Vergleich das zuletzt vollständige Paar von Ablesungen verwendet, einen Balken hinter der Kerze, auf der er handelt.
- Jede Variable, die eine Konstante hält — die Null und das Ordervolumen —, wird von der gehandelten Kerze ausgelöst. Eine Variable ohne Trigger behält ihren Wert und gibt ihn nie aus, und eine Bedingung, die auf sie wartet, käme nie zustande.
- Beide MACD-Blöcke geben nur geformte und endgültige Werte aus, sodass der Vergleich beginnt, sobald jedes Bein einen vollständigen Indikator hinter sich hat, und sich innerhalb eines Balkens nicht bewegen kann.
- Beide Enden jeder Sync-Leitung sind verbunden. Ein Wert, der hineingeschickt und nie herausgenommen wird, lässt den Block darauf warten, und die Strategie startet nicht.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
