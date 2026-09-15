# Diagramm der Zwei-Bein-Spread-Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei Instrumente, die sich normalerweise gemeinsam bewegen, legen manchmal unterschiedlich weite Strecken zurück, und dasjenige, das zurückgeblieben ist, holt in der Regel wieder auf. Das Diagramm misst, wie weit sich jedes der beiden über dieselbe Anzahl an Bars bewegt hat, zieht den einen Wert vom anderen ab, und sobald sich die Lücke weit genug öffnet, kauft es im selben Moment das zurückgebliebene Bein und verkauft das vorausgelaufene. Beide Beine werden zurückgegeben, sobald sich die Lücke wieder schließt.

![schema](schema.svg)

## Strategieübersicht

- Eine Variable vom Typ Instrument benennt das zweite Instrument, damit das Paar eine Einstellung ist und keine Verdrahtungsentscheidung; dieselbe Variable richtet auch die zweite Kerzenserie und jeden Orderblock des zweiten Beins darauf aus. Das erste Bein ist das Instrument, auf das die Strategie selbst eingestellt ist.
- Beide Kerzenserien sind auf ausschließlich abgeschlossene Kerzen eingestellt, damit ein unfertiger Bar niemals eine Entscheidung beeinflussen oder eine Order datieren kann.
- Sync hält je eine Leitung pro Instrument zurück und lässt beide Kerzen gemeinsam im Fünf-Minuten-Takt heraus. Zwei Datenströme treffen unabhängig voneinander ein, und erst nach diesem Zurückhalten gehören die beiden Kerzen zum selben Bar - und genau das macht den Vergleich der beiden Instrumente überhaupt erst sinnvoll.
- Für jede freigegebene Kerze nimmt ein Konverter den Schlusskurs, ein Previous value-Block hält die Kerze von einer festen Anzahl Bars zuvor, und ein zweiter Konverter liest den Schlusskurs aus dieser älteren Kerze. Der Block hält die Kerze selbst, das Feld wird erst danach gelesen - diese Reihenfolge liefert auf jedem Bar einen Wert.
- Zwei Formeln machen aus jedem Kurspaar eine einzige Zahl: wie weit sich dieses Bein seit dem älteren Bar bewegt hat, in Prozent des älteren Kurses.
- Zwei Variablen klinken diese beiden Prozentwerte an die gehandelte Kerze: Jede hält den zuletzt vom Paar erzeugten Wert und gibt ihn frei, sobald ein Bar auf dem gehandelten Instrument abgeschlossen ist. Damit trägt jeder nachgelagerte Vergleich, jedes Gatter und jede Order den Takt des Instruments, an das die Orders gehen.
- Eine Formel zieht die Strecke des zweiten Beins von der des gehandelten ab - das ist die Divergenz -, eine weitere nimmt für den Ausstieg ihren Betrag ohne Vorzeichen. Vergleiche prüfen die Divergenz gegen ein symmetrisches Band, beide Bein-Werte gegen null und die Position gegen null; zwei Und-Verknüpfungen und ein Oder machen aus den beiden Vorzeichenablesungen ein einziges Korrelations-Flag.
- Drei logische Bedingungen setzen die Entscheidungen zusammen, sechs Positionsblöcke führen sie aus: je zwei eröffnen ein Paar in eine Richtung, eine Order je Instrument, und zwei schließen beide Beine. Jeder eröffnende Block ist so eingestellt, dass er nur aus einer flachen Position auf seinem eigenen Instrument heraus handelt; ein Signal, das sich bei laufendem Paar wiederholt, fügt ihm daher nichts hinzu.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Divergenz liegt unter der unteren Kante des Bands - das gehandelte Bein ist hinter dem zweiten zurückgeblieben -, beide Beine haben sich über den Versatz in dieselbe Richtung bewegt, und das gehandelte Bein ist flach. Auf dieses eine Signal hin lösen zwei Blöcke gemeinsam aus: Der eine kauft das Volumen des gehandelten Beins zum Marktpreis auf dem Strategie-Instrument, der andere verkauft das Volumen des zweiten Beins zum Marktpreis auf dem benannten Instrument.
- **Short-Einstieg**: Die Divergenz liegt über der oberen Kante des Bands - das gehandelte Bein ist dem zweiten vorausgelaufen -, beide Beine haben sich über den Versatz in dieselbe Richtung bewegt, und das gehandelte Bein ist flach. Das gespiegelte Blockpaar verkauft das Volumen des gehandelten Beins und kauft das Volumen des zweiten Beins, beides zum Marktpreis.
- **Ausstieg**: Beide Beine werden gemeinsam zurückgegeben, sobald sich die Lücke, auf die hin sie eröffnet wurden, geschlossen hat: Der Betrag der Divergenz, ohne Vorzeichen genommen, fällt bei offenem Paar unter die Ausstiegsschwelle. Auf dieses eine Signal hin lösen zwei schließende Blöcke aus, einer je Instrument, und jeder ermittelt seine Größe selbst aus dem, was er offen vorfindet - keiner braucht daher ein eigenes Volumen, und ein schließender Block, der auf einem flachen Instrument auslöst, tut schlicht nichts. Es gibt hier kein Geldziel, keinen Stop-Loss und keine Zeitbegrenzung; das Zusammenlaufen der beiden Beine ist der gesamte Ausstieg.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Second Instrument | TONUSDT@BNBFT | Das Instrument, auf dem das zweite Bein gehandelt wird. Es muss in den angebundenen Daten vorhanden und ein tatsächlich handelbares Instrument sein: An es werden Orders geschickt, es wird nicht nur ausgelesen. |
| Traded Candles | 00:05:00 | Länge der Kerzen auf dem eigenen Instrument der Strategie. Die Entscheidungen und die Orders laufen in diesem Takt. |
| Second Leg Candles | 00:05:00 | Länge der Kerzen auf dem zweiten Instrument. Gleich der gehandelten Serie halten, sonst werden die beiden Beine über unterschiedlich lange Zeiträume gemessen und die Differenz zwischen ihnen bedeutet nichts. |
| Sync Interval | 00:05:00 | Der Takt, in dem das Zurückhalten beide Instrumente freigibt. An die Kerzenlänge angleichen. |
| Traded Leg Shift | 12 | Wie viele Bars zurück die Strecke des gehandelten Beins gemessen wird. Zwölf Fünf-Minuten-Bars sind eine Stunde. |
| Second Leg Shift | 12 | Dieselbe Anzahl für das zweite Bein. Beide gleich halten: Zwei unterschiedliche Zeiträume machen die Subtraktion sinnlos. |
| Divergence Threshold, % | 0.3 | Wie weit die beiden Strecken auseinanderliegen müssen, in Prozent, bevor sich das Eröffnen des Paares lohnt. Das Band ist symmetrisch, dieser eine Wert setzt daher beide Kanten. |
| Exit Threshold, % | 0.1 | Wie nah die beiden Strecken wieder zusammenkommen müssen, in Prozent, bevor beide Beine zurückgegeben werden. Unterhalb der Einstiegsschwelle halten, sonst wird ein Paar auf demselben Bar geschlossen, auf dem es eröffnet wurde. |
| Traded Leg Volume | 1 | Ordergröße auf dem Strategie-Instrument, in Lots. |
| Second Leg Volume | 10 | Ordergröße auf dem zweiten Instrument, in Lots. Die beiden Größen werden unabhängig voneinander gesetzt, das Verhältnis zwischen den Beinen ist also eine Einstellung und nichts, was aus den Kursen berechnet wird - so wählen, dass es zu den Kursniveaus der beiden Instrumente passt, und gegen den Volumenschritt des zweiten prüfen, sonst wird die Order abgerundet. |

## Diagrammdetails

- Sync ist es, was die Bars paart, aber auf seiner Freigabe wird nichts geordert. Zwei Datenströme werden nicht im selben Augenblick fertig, und ein vom Zurückhalten freigegebener Satz trägt die frühere der beiden Zeiten; eine Order mit einem Zeitstempel vor der aktuellen Zeit wird abgelehnt. Erst die beiden Variablen, die die Werte an die gehandelte Kerze klinken, halten den gesamten nachgelagerten Teil auf einem einzigen Takt.
- Die Folge dieses Einklinkens sollte man kennen: Die Werte, auf denen eine Entscheidung beruht, sind das letzte vollständige Paar, das Positionspaar wird also auf dem Bar nach demjenigen eröffnet, der die Ablesung erzeugt hat, und nicht innerhalb davon.
- Jede Konstante - die Null, beide Schwellen und beide Volumina - wird von der gehandelten Kerze ausgelöst. Ein Vergleich braucht für jede Auswertung beide Werte erneut, und eine Konstante, die nie wieder gesendet wird, legt die von ihr gespeiste Bedingung still lahm, ohne dass etwas darauf hinweist.
- Die untere Kante des Bands ist eine Formel über die Schwelle und keine zweite Konstante; damit bleibt das Band symmetrisch, welchen Wert die Schwelle auch bekommt, und es ist eine Zahl zu ändern statt zweier, die auseinanderlaufen können.
- Der Positionsblock liest nur das gehandelte Instrument, und sein Wert wird genauso an den Bar geklinkt wie die Bein-Werte. Beide Beine werden von denselben Signalen eröffnet und geschlossen, deshalb steht der Zustand des gehandelten Beins für den Zustand des Paares.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
