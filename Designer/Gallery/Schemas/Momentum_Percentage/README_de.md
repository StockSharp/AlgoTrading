# Diagramm der Momentum-Nulldurchgangs-Strategie mit SMA-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hier sind zwei Ideen übereinandergelegt. Das Momentum, die Differenz zwischen dem aktuellen Schlusskurs und dem Schlusskurs von vor zehn Kerzen, sagt, wohin der Markt den Kurs auf dieser Strecke geschoben hat, und der Vorzeichenwechsel dieser Differenz ist der Auslöser. Ein einfacher gleitender Durchschnitt spielt dann den Schiedsrichter: Der Durchgang wird nur in die Richtung gehandelt, der der Schlusskurs ohnehin zustimmt.

![schema](schema.svg)

## Strategieübersicht

- Der Nulldurchgang wird mit zwei Vergleichen ausgeschrieben, dem aktuellen Wert gegen null und dem Wert von einer Kerze zuvor gegen null - genau die Bedingung, die der Originalcode formuliert.
- Der Filter des gleitenden Durchschnitts trennt die Richtungen: Ein Durchgang nach oben kauft nur, solange der Schlusskurs über dem Durchschnitt liegt, ein Durchgang nach unten verkauft nur, solange er darunter liegt.
- Trotz des Ordnernamens ist der Indikator Momentum, eine absolute Kursdifferenz in Punkten, und keine prozentuale Änderungsrate.
- Jedes Signal dreht die Position: Das Ordervolumen ist das gemeinsame Volumen plus der Betrag der aktuellen Position, sodass eine einzige Ausführung die alte Seite schließt und die neue eröffnet.
- Das Original friert den Handel nach jeder Ausführung für 30 Kerzen ein; einen Balkenzähler gibt es als Baustein nicht, also entfällt diese Pause und das Diagramm reagiert auf jeden gültigen Durchgang.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das Momentum stand auf der Vorkerze auf oder unter null, steht jetzt darüber, der Schlusskurs liegt über der SMA und die Position ist nicht long. Die Order kauft das Umkehrvolumen zu Markt.
- **Short-Einstieg**: Das Momentum stand auf der Vorkerze auf oder über null, steht jetzt darunter, der Schlusskurs liegt unter der SMA und die Position ist nicht short. Die Order verkauft das Umkehrvolumen zu Markt.
- **Ausstieg**: Es gibt weder einen eigenen Ausstiegsbaustein noch einen Schutzstopp, genau wie im Original: Eine Position wird gehalten, bis der entgegengesetzte Durchgang sie mit einer Order dreht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Momentum Length | 10 | Anzahl der Kerzen, über die das Momentum zurückblickt; der Wert ist der aktuelle Schlusskurs minus der Schlusskurs von so vielen Kerzen zuvor. |
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der die Richtung des Durchgangs filtert. |
| Volume | 1 | Basisordervolumen in Lots; die Umkehrorder addiert den Betrag der offenen Position dazu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist drei Zweige: den Momentum-Indikator, den einfachen gleitenden Durchschnitt und einen Konverter, der den Schlusskurs entnimmt.
- Ein Vorwert-Baustein hält den Momentum-Stand der letzten Kerze, und vier Vergleichsbausteine legen den aktuellen und den vorherigen Stand auf je eine Seite einer gemeinsamen Nullkonstante.
- Zwei weitere Vergleichsbausteine stellen den Schlusskurs dem gleitenden Durchschnitt gegenüber, zwei vergleichen die Position mit derselben Nullkonstante.
- Jedes logische UND verbindet die vorherige Seite der Null, die aktuelle Seite, den Durchschnittsfilter und die Positionsprüfung und löst einen Baustein zur Positionsänderung aus, dessen Volumen aus einer Formel aus Volumen plus Betrag der Position stammt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
