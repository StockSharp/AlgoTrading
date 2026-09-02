# Diagramm der Gap-Fill-Reversal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm misst den Sprung zwischen dem Schluss einer Kerze und der Eröffnung der nächsten und wartet dann darauf, dass diese Kerze in die Gegenrichtung schließt. Ein Abwärts-Gap mit anschließend bullischer Kerze wird gekauft, ein Aufwärts-Gap mit bärischer Kerze verkauft; wann der Trade endet, entscheidet die SimpleMovingAverage.

![schema](schema.svg)

## Strategieübersicht

- Das Gap wird in Prozent des vorherigen Schlusskurses gemessen, damit dieselbe Schwelle auf jedem Kursniveau dieselbe Bedeutung behält.
- Ein Gap allein ist kein Signal: Die Kerze, die abseits des vorherigen Schlusskurses eröffnet, muss wieder dorthin zurückschließen - das ist der namensgebende Umkehrkörper.
- Die SimpleMovingAverage ist die einzige Ausstiegslinie für beide Seiten; Stop-Loss und Take-Profit gibt es nicht, genau wie im Originalcode.
- Das Diagramm läuft auf Minutenkerzen wie die Vorlage, das Gap ist hier also der kleine Bruch zwischen zwei benachbarten Minuten und keine Übernacht-Kurslücke.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Abstand zwischen Eröffnung und vorherigem Schluss beträgt mindestens Min Gap %, die Eröffnung liegt unter dem vorherigen Schluss, die Kerze schließt über ihrer eigenen Eröffnung und es besteht keine Position. Die Order kauft ein Lot zum Marktpreis.
- **Short-Einstieg**: Der Abstand zwischen Eröffnung und vorherigem Schluss beträgt mindestens Min Gap %, die Eröffnung liegt über dem vorherigen Schluss, die Kerze schließt unter ihrer eigenen Eröffnung und es besteht keine Position. Die Order verkauft ein Lot zum Marktpreis.
- **Ausstieg**: Ein Long wird auf der ersten Kerze geschlossen, die unter der SimpleMovingAverage schließt, ein Short auf der ersten Kerze darüber; beide Schließbausteine ermitteln das Volumen selbst aus der offenen Position.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Min Gap % | 0.02 | Mindestabstand zwischen dem vorherigen Schlusskurs und der neuen Eröffnung, in Prozent des vorherigen Schlusskurses. |
| SMA Length | 20 | Glättungsperiode der SimpleMovingAverage, die die Position schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:01:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Zwei Konverterbausteine lesen Eröffnung und Schluss der Kerze, ein Baustein für den Vorwert hält den Schluss der Kerze davor.
- Der Formelbaustein rechnet den Abstand zwischen Eröffnung und vorherigem Schluss in Prozent um, ein Vergleich stellt ihn der Schwellenkonstante gegenüber.
- Vier weitere Vergleiche liefern die Richtung des Gaps und die Richtung des Kerzenkörpers; jedes logische UND verbindet Gap-Bedingung, Körperbedingung und Nullpositionsprüfung vor dem Orderbaustein.
- Das Ausstiegspaar vergleicht den Schlusskurs mit dem gleitenden Durchschnitt und steuert zwei Bausteine zum Schließen der Position. Die Pause von 500 Bars zwischen zwei Trades aus dem Code hat keine Entsprechung unter den Bausteinen und entfällt, daher handelt das Diagramm häufiger.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
