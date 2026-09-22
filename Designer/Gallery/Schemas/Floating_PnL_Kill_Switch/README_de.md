# Strategiediagramm für einen Floating-PnL-Kill-Switch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm ergänzt ein CCI-Rückkehrsignal um eine geldbasierte Notfallstufe. Einstiege erscheinen als Limitorders zum Stundenschluss; erreicht der unrealisierte Gewinn eine Grenze, werden Arbeitsorders storniert und die Position zum Markt geschlossen.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Stundenkerzen speisen einen CommodityChannelIndex mit Länge 30 und liefern den Limitpreis über ihren Schlusskurs.
- Long verlangt einen vorherigen CCI unter −100 und einen aktuellen Wert zurück bei mindestens −100; Short spiegelt die Rückkehr von über +100.
- Käufe benötigen Position <= 0, Verkäufe Position >= 0, sodass Gegensignale bestehende Exponierung abbauen.
- Jede Ausführung setzt eine Pause von vier Kerzen zurück; der gedeckelte Zähler muss sie erneut erreichen.
- Order registering stellt die Order zum Schlusskurs der fertigen Kerze ohne Preisrundung ein.
- P&L change vergleicht den unrealisierten Wert mit +300 und −200; jede Grenze startet Mass order cancellation und ClosePosition.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorherige CCI lag unter −100, der aktuelle liegt wieder bei mindestens −100, Position ist nicht long und vier Kerzen sind seit der letzten Ausführung vergangen. Ein Kauflimit wird zum Schlusskurs eingestellt.
- **Short-Einstieg**: Der vorherige CCI lag über +100, der aktuelle liegt wieder bei höchstens +100, Position ist nicht short und die Pause ist abgelaufen. Ein Verkaufslimit wird eingestellt.
- **Ausstieg**: Es gibt keinen preisbezogenen Take Profit oder Stop. Bei +300 Gewinn oder −200 Verlust storniert der Kill-Switch alle Arbeitsorders und sendet gleichzeitig ein marktmäßiges ClosePosition.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 01:00:00 | Zeitrahmen der fertigen Kerzen für CCI, Pause und Limitpreise. |
| CCI Length | 30 | Anzahl der Stundenwerte im CommodityChannelIndex. |
| CCI Level | 100 | Absolute CCI-Schwelle, symmetrisch als +Level und −Level verwendet. |
| Signal Cooldown, candles | 4 | Fertige Kerzen, die seit der letzten Ausführung verstreichen müssen. |
| Order Volume | 1 | Menge jeder Limit-Einstiegsorder. |
| Target Profit, money | 300 | Unrealisierter Gewinn in Kontowährung für die Notfallliquidation. |
| Cut Loss, money | -200 | Unrealisierte Verlustgrenze in Kontowährung, gewöhnlich negativ. |

## Diagrammdetails

- Previous value hält den vorherigen CCI; der Einstieg ist daher eine Rückkehr durch die Schwelle und kein dauerhaftes Extremzonensignal.
- Die Pause beginnt bei einer echten Ausführung, nicht bei Signal oder Registrierungsversuch, und ist bei vier gedeckelt.
- Limit-Einstiege zum Schluss zeigen bewusst den Pending-Order-Lebenszyklus und geben der Massenstornierung eine reale Aufgabe.
- Ziel und Verlustgrenze sind absolute unrealisierte Kontowährungsbeträge, eine Notfallstufe und kein prozentualer Preisschutz.
- Ohne Security- oder Portfoliofilter storniert der Block alle Strategieorders; Modify position schließt anschließend die verbleibende Seite.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
