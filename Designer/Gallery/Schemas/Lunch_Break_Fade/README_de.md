# Strategiediagramm für Lunch Break Fade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt die Gegenbewegung einer kurzfristigen Kursbewegung im Mittagsfenster von 11:00:00 bis 14:59:59 anhand abgeschlossener Fünf-Minuten-Kerzen. Es steigt nur aus einer neutralen Position ein, beendet Positionen durch den Vergleich des Schlusskurses mit einem gebildeten SMA über 20 Perioden und sperrt nach einem Ordersignal alle Ein- und Ausstiegspfade für die nächsten 30 abgeschlossenen Kerzen.

![schema](schema.svg)

## Strategieübersicht

- Nur abgeschlossene Fünf-Minuten-Kerzen gelangen in die Entscheidungskette. Der SMA liefert Werte nach seiner Aufwärmphase über 20 Perioden, und zwei Previous-value-Bausteine stellen die beiden unmittelbar vorherigen Schlusskurse bereit.
- Der Baustein Working time liest die Eröffnungszeit jeder Kerze und erlaubt Einstiege von 11:00:00 bis einschließlich 14:59:59. Das Zeitfenster beschränkt die Ausstiege nicht.
- Zwei steigende vorherige Schlusskurse mit einer aktuellen bärischen Kerze erzeugen aus einer neutralen Position einen Short-Einstieg. Zwei fallende Schlusskurse mit einer bullischen Kerze erzeugen einen Long-Einstieg.
- Eine Long-Position wird beendet, wenn der Schlusskurs unter dem SMA liegt; eine Short-Position wird beendet, wenn der Schlusskurs über dem SMA liegt. Vier getrennte Pfade senden Market-Orders mit fester Menge für die beiden Einstiege und beiden Ausstiege.
- Jedes Ein- oder Ausstiegssignal aktiviert eine Abkühlphase, die beide Aktionsarten für die nächsten 30 abgeschlossenen Kerzen sperrt. Ein Positionsschutz-Baustein fehlt; der Chart zeigt Kerzen, SMA und Ausführungen aller vier Orderpfade.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Im Mittagsfenster sendet das Diagramm eine Market-Kauforder mit Volume 1, wenn `Close[-1] < Close[-2]` gilt, die aktuelle Kerze bullisch ist (`Close > Open`), die Positionsmomentaufnahme null ist und die Abkühlphase beendet ist.
- **Short-Einstieg**: Im Mittagsfenster sendet das Diagramm eine Market-Verkaufsorder mit Volume 1, wenn `Close[-1] > Close[-2]` gilt, die aktuelle Kerze bärisch ist (`Close < Open`), die Positionsmomentaufnahme null ist und die Abkühlphase beendet ist.
- **Ausstieg**: Nach Ende der Abkühlphase sendet eine Long-Position bei `Close < SMA` eine Market-Verkaufsorder, während eine Short-Position bei `Close > SMA` eine Market-Kauforder sendet. Diese Niveauprüfungen laufen innerhalb und außerhalb des Mittagsfensters. Stop-Loss, Gewinnziel oder anderer Schutz sind nicht angeschlossen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Fünf-Minuten-Zeitrahmen; nur abgeschlossene Kerzen treiben Indikator, Historie, Abkühlphase und Entscheidungen an. |
| SMA Period | 20 | Periode des SimpleMovingAverage für beide Ausstiegsprüfungen des Kursniveaus. |
| Cooldown Bars | 30 | Anzahl der nachfolgenden abgeschlossenen Kerzen, während der Ein- und Ausstiegssignale gesperrt sind. |
| Lunch Begin | 11:00:00 | Einschließliche Eröffnungszeitgrenze, ab der Einstiege im Mittagsfenster zulässig sind. |
| Lunch End | 14:59:59 | Einschließliche Eröffnungszeitgrenze, bis zu der Einstiege im Mittagsfenster zulässig bleiben. |
| Volume | 1 | Feste Menge für alle vier Market-Order-Bausteine mit NoCondition. |

## Diagrammdetails

- Der Baustein [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) gibt abgeschlossene Fünf-Minuten-Kerzen aus. Ein [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), der nur gebildete Werte ausgibt, berechnet den SimpleMovingAverage 20; eine [Formel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html) stellt seinen Zahlenwert bereit. Die Prüfung [Handel erlaubt](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) gibt die gespeicherte Kerze an die Entscheidungskette weiter.
- [Konverter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)-Bausteine lesen Close- und Open-Preis aus. Zwei [Vorheriger Wert](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)-Bausteine verwenden Verschiebung 1 und 2; eine Historienfreigabe verhindert Entscheidungen, bis beide vorherigen Schlusskurse verfügbar sind.
- Der Baustein [Arbeitszeit](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) empfängt den Kerzenstrom direkt und vergleicht die Metadaten der Eröffnungszeit mit den einschließlichen Grenzen des Mittagsfensters. Sein Ergebnis ist nur Teil der beiden Einstiegsbedingungen.
- Die aktuelle [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird in einer [Variablen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) gespeichert und einmal pro Entscheidungskerze ausgegeben. [Vergleich](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)- und [Logische Bedingung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Bausteine verbinden Sitzung, vorherige Richtung, Kerzenrichtung, Position, Historienfreigabe, SMA-Niveau und Abkühlstatus.
- Ein Baustein [Signal verzögern](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) zählt 30 spätere abgeschlossene Kerzen. Bereitschaftsvariablen unterdrücken während der Zählung alle vier Aktionsbedingungen und geben sie auf der folgenden Kerze wieder frei.
- Vier [Position ändern](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Bausteine platzieren Market-Orders mit `NoCondition` und dem gemeinsamen Volume 1: Kaufeinstieg, Verkaufseinstieg, Verkaufsausstieg aus Long und Kaufausstieg aus Short. Ein Schutzelement ist nicht vorhanden.
- Das [Chartpanel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) erhält abgeschlossene Kerzen, den gebildeten SMA-Strom und die MyTrade-Ausgabe jedes der vier Orderbausteine.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
