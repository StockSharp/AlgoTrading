# Strategiediagramm für zeitgesteuerte virtuelle Ausbrüche mit Pending Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm nutzt abgeschlossene Fünf-Minuten-Kerzen, um einmal täglich zwei symmetrische virtuelle Ausbruchsniveaus zu aktivieren. Die mit 02:00 gestempelte Kerze liefert den Referenzschlusskurs; eine spätere Berührung eines der beiden Niveaus registriert einen einzigen Markteinstieg, der prozentuale Schutz verwaltet die Ausführung und die mit 22:00 gestempelte Kerze deaktiviert die Konfiguration und schließt jede verbleibende Position. Die virtuellen Niveaus sind gespeicherte Werte und keine ruhenden Börsenorders.

![schema](schema.svg)

## Strategieübersicht

- Nur abgeschlossene Fünf-Minuten-Kerzen steuern die Zeitprüfung, die Niveauberechnung, die Ausbruchsprüfung und die Aktualisierung des Schutzpreises. Das Öffnungsfenster `02:00:00–02:04:59` wählt genau die mit 02:00 gestempelte Kerze aus; sie wird bei ihrem Abschluss gegen 02:05 verarbeitet.
- Wenn die Position während dieses Öffnungsimpulses neutral ist, speichert das Diagramm den Kerzenschlusskurs und berechnet `Upper Level = Close × 1.0015` sowie `Lower Level = Close × 0.9985`. Beide gespeicherten Werte bleiben fest und werden beim nächsten qualifizierten Öffnungsimpuls ersetzt.
- Eine Variable für den Aktivstatus und eine geordnete Auslöserkette am Kerzenende stellen sicher, dass High, Low und beide gespeicherten Niveaus vor einer Entscheidung aktualisiert sind. Ein gemeinsam verwendeter einmaliger Flag-Baustein lässt nur den ersten Ausbruch der täglichen Konfiguration zu.
- Der obere Vergleich wird vor dem unteren ausgewertet. Wenn eine Kerze beide Niveaus überstreicht, wird nur der obere Ausbruch akzeptiert und das Diagramm registriert einen einzigen Marktkauf; andernfalls kann der untere Ausbruch einen einzigen Marktverkauf registrieren. Vor der Berührung eines Niveaus besteht keine Order.
- Die Ausführungen der Einstiegsorders initialisieren den Positionsschutz. Danach steuern die Schlusskurse abgeschlossener Kerzen dessen Take-Profit-Prüfung bei 2% und Stop-Loss-Prüfung bei 0.5%. Das Schließungsfenster `22:00:00–22:04:59` deaktiviert jede ungenutzte Konfiguration und sendet eine auf Order Volume 1 begrenzte ReduceOnly-Marktaktion; ihre Ausführung wird an den Schutzbaustein zurückgeführt, damit dessen Überwachung der Exposition zurückgesetzt wird.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Solange das virtuelle Paar aktiviert ist, passiert `High ≥ Upper Level` das einmalige tägliche Gate und registriert einen Marktkauf mit Order Volume 1. Dasselbe Ereignis deaktiviert beide Ausbruchspfade bis zum nächsten gültigen Öffnungsimpuls.
- **Short-Einstieg**: Wenn der obere Ausbruch nicht akzeptiert wurde, passiert eine aktive Bedingung `Low ≤ Lower Level` das einmalige Gate und registriert einen Marktverkauf mit Order Volume 1. Sie deaktiviert außerdem beide Ausbruchspfade für den Rest des Zyklus.
- **Ausstieg**: Der Positionsschutz sendet einen Marktausstieg, wenn sich der Schlusskurs einer abgeschlossenen Kerze vom tatsächlichen Einstiegsausführungspreis um 2% zugunsten der Position oder um 0.5% gegen sie bewegt. Unabhängig davon deaktiviert die mit 22:00 gestempelte Kerze das virtuelle Paar und fordert eine ReduceOnly-Marktschließung von höchstens Order Volume 1 an. Eine innerhalb der Kerze berührte Schutzschwelle, die beim Schluss der abgeschlossenen Kerze nicht mehr vorliegt, löst keine Aktion aus; nach einem Ausstieg wird die Konfiguration erst im nächsten Öffnungsfenster erneut aktiviert.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles Series | 00:05:00 | Abgeschlossene Fünf-Minuten-Kerzen für Zeitsteuerung, virtuelle Niveaus, Ausbruchsprüfungen und schlusskursbasierte Schutzprüfungen. |
| Opening Window | 02:00:00–02:04:59 | Inklusives Intervall für genau eine Kerze, das bei neutraler Position den Referenzschlusskurs erfasst. |
| Closing Window | 22:00:00–22:04:59 | Inklusives Intervall für genau eine Kerze, das eine ungenutzte Konfiguration deaktiviert und eine offene Position schließt. |
| Entry Distance | 0.15% | Symmetrischer prozentualer Abstand über und unter dem erfassten Schlusskurs. |
| Take Profit | 2% | Günstige Schlusskursbewegung ab dem tatsächlichen Einstiegsausführungspreis, die den Schutz auslöst. |
| Stop Loss | 0.5% | Ungünstige Schlusskursbewegung ab dem tatsächlichen Einstiegsausführungspreis, die den Schutz auslöst. |
| Order Volume | 1 | Menge für jeden der beiden Markteinstiege und maximale geplante Positionsreduzierung. |

## Diagrammdetails

- Der [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Baustein gibt abgeschlossene Fünf-Minuten-Kerzen aus. [Konverter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/converter.html)-Bausteine für Close, High und Low stellen explizite numerische Datenströme bereit.
- Zwei [Arbeitszeit](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)-Bausteine prüfen OpenTime der Kerze. Ihre Obergrenzen enden eine Sekunde vor dem nächsten Fünf-Minuten-Zeitstempel, weil beide konfigurierten Grenzen inklusive sind.
- [Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)-Bausteine speichern den Referenzschlusskurs, die berechneten Niveaus, den Aktivstatus, die gewählte Seite und die Konstanten. Zwei [Formel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html)-Bausteine berechnen die symmetrischen prozentualen Abstände nur während eines qualifizierten Öffnungsimpulses.
- [Vergleich](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-, [Logikbedingung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)- und [Flag](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/flag.html)-Bausteine erzwingen die Aktivierung nur bei neutraler Position, die Auswertung aktualisierter Werte, den Vorrang des Kaufs und genau einen akzeptierten Ausbruch pro Konfiguration.
- Die [Orderregistrierung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/orders/register.html)-Bausteine für Kauf und Verkauf sind Marktorderaktionen. Ihre MyTrade-Ausgänge initialisieren den gemeinsam verwendeten [Positionsschutz](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)-Baustein, dessen Price-Eingang die Schlusskurse abgeschlossener Kerzen erhält.
- Der geplante [Position ändern](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Baustein verwendet ReduceOnly mit Order Volume 1. Er leitet die Schließungsrichtung aus der aktuellen Exposition ab, erhöht die Position niemals und führt seinen MyTrade-Ausgang nach einem zeitgesteuerten Ausstieg an den Positionsschutz zurück.
- Das [Chart-Panel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) zeigt die abgeschlossenen Kerzen, beide gespeicherten virtuellen Niveaus, Einstiegs- und Schutzorders sowie die Ausführungen für Einstieg, Schutz und geplante Schließung.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
