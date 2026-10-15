# Strategiediagramm für RSI-Serien mit Pending-Limits
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm wartet auf eine anhaltende RSI-Extremzone und platziert danach eine Pullback-Limitorder. Es prüft den aktuellen RSI sowie die beiden vorherigen abgeschlossenen Werte, erlaubt je ununterbrochenem Aufenthalt in der Zone eine Pending-Order, storniert sie beim Verlassen der Zone und schützt jede Ausführung durch prozentuale Ausstiege.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen einen RSI mit Länge 14 und liefern den Schlusskurs zur Berechnung der Pending-Orders.
- Die untere Zone liegt unter 30, die obere über 70. Ein N-values-Block plant die Auswertung nach drei abgeschlossenen RSI-Aktualisierungen.
- Bei der Auswertung prüfen Formula-Blöcke gemeinsam den aktuellen RSI, die beiden vorherigen Werte und die Positionsbedingung. Eine unterbrochene Serie erzeugt keinen Einstieg.
- Das Buy-Limit liegt 0,2% unter dem Schlusskurs des Signals, das Sell-Limit 0,2% darüber.
- Getrennte Flag-Blöcke erlauben während jedes ununterbrochenen Aufenthalts in einer Extremzone nur eine Order.
- Ausgeführte Einstiege erhalten 1,5% Take-Profit und 1% Stop-Loss; bei Aktivierung werden beide als Market-Order gesendet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Nach der Drei-Aktualisierungs-Auswertung müssen alle drei RSI-Werte unter 30 liegen und Position muss kleiner oder gleich null sein. Ein Buy-Limit wird bei `Close × (1 − Pending Offset / 100)` registriert.
- **Short-Einstieg**: Nach der Drei-Aktualisierungs-Auswertung müssen alle drei RSI-Werte über 70 liegen und Position muss größer oder gleich null sein. Ein Sell-Limit wird bei `Close × (1 + Pending Offset / 100)` registriert.
- **Pending-Order**: Ein unausgeführtes Buy-Limit wird storniert, sobald der RSI über 30 steigt; ein unausgeführtes Sell-Limit, sobald der RSI unter 70 fällt. Dasselbe Ereignis setzt das Flag dieser Seite für einen späteren Zonenbesuch zurück.
- **Ausstieg**: Nach Ausführung einer Pending-Order schließt Position protection deren Exposure bei einer günstigen Bewegung von 1,5% oder einer ungünstigen Bewegung von 1% mit einer Market-Order.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Kerzen | 00:05:00 | Zeitrahmen der abgeschlossenen Kerzen für Signale und Preisberechnung. |
| RSI-Länge | 14 | Mittelungslänge des Relative-Stärke-Index. |
| RSI-Eingabefeld | Nicht gesetzt | Es ist kein alternatives Eingabefeld für den Indikator ausgewählt. |
| Überverkauft | 30 | Strikte Obergrenze einer Long-Serie aus drei Werten. |
| Überkauft | 70 | Strikte Untergrenze einer Short-Serie aus drei Werten. |
| Trefferzahl (N) | 3 | Anzahl abgeschlossener RSI-Aktualisierungen im Bestätigungsintervall. |
| Pending-Abstand, % | 0.2 | Entfernung des Limits vom Schlusskurs der Signalkerze. |
| Volumen | 1 | Größe jeder Pending-Einstiegsorder. |
| Take-Profit, % | 1.5 | Günstige Bewegung ab dem ausgeführten Einstieg, die den Schutz aktiviert. |
| Stop-Loss, % | 1 | Ungünstige Bewegung ab dem ausgeführten Einstieg, die den Schutz aktiviert. |
| Trailing-Stop | false | Hält die Stop-Grenze fest. |
| Market-Orders verwenden | true | Sendet aktivierte Schutzausstiege als Market-Orders. |

## Diagrammdetails

- Der aktuelle RSI speist zwei Previous-value-Blöcke mit den Verschiebungen 1 und 2. Alle drei Werte stammen ausschließlich aus abgeschlossenen Kerzen.
- Ein gemeinsamer N-values-Block wird von jeder Extremzone aktiviert und zählt drei RSI-Aktualisierungen. Sein Ausgang liest beide Einstiegsscores am selben Auswertungspunkt ein.
- Der Long-Score ist nur negativ, wenn alle drei RSI-Werte unter 30 liegen und Position nicht positiv ist. Der Short-Score ist nur negativ, wenn alle drei Werte über 70 liegen und Position nicht negativ ist.
- Verlässt der RSI die Zone während des Bestätigungsintervalls, ist der zugehörige Score nicht negativ und es entsteht kein Registrierungstrigger. Ein späterer Extrembesuch kann ein neues Intervall starten.
- Formula-Blöcke berechnen beide Limitpreise aus dem Schlusskurs; Order-registering-Blöcke senden ungerundete Limitorders mit einer Einheit.
- Order cancellation hält die jeweils letzte Order einer Seite und reagiert, sobald der RSI die Grenze dieser Zone zurückkreuzt.
- Eine Einheitsorder gegen eine bestehende Gegenposition von einer Einheit stellt das Exposure zunächst glatt; für Exposure in der neuen Richtung ist ein weiterer bestätigter Zonenbesuch erforderlich.
- Das Diagramm zeigt Fünf-Minuten-Kerzen, RSI mit beiden Schwellen, beide Pending-Order-Ströme sowie sämtliche Ausführungen und Ausstiege der Strategie.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie mit historischen Daten im Backtester und passen Sie RSI-Grenzen, Pending-Abstand, Schutzdistanzen und Volumen vor dem Live-Handel an das Instrument an.
