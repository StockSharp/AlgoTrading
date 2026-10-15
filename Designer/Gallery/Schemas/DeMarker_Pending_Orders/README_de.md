# DeMarker-Strategiediagramm mit Pending-Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm wandelt DeMarker-Schwellenkreuzungen in Pullback-Limitorders um, statt sofort einzusteigen. Jede Pending-Order bleibt vier Kerzen aktiv; prozentualer Take-Profit und Stop-Loss werden erst nach ihrer tatsächlichen Ausführung eingeschaltet.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünfzehn-Minuten-Kerzen speisen einen DeMarker-Oszillator mit Länge 14 und einen Schlusskursstrom.
- Die untere und obere Schwelle liegen bei 0,3 und 0,7; Crossing-Blöcke erkennen das Unterschreiten der unteren und das Überschreiten der oberen Schwelle.
- Einstiege sind gemäß Kerzenzeit nur von 07:00:00 bis 20:59:59 und ausschließlich bei glatter Position erlaubt.
- Ein neues Signal ersetzt jede ältere Pending-Order; eine unausgeführte Order verfällt außerdem nach vier folgenden abgeschlossenen Kerzen.
- Das Diagramm zeigt Kerzen, Oszillator, beide Schwellen und alle Strategieausführungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Fällt DeMarker im Einstiegsfenster durch 0,3 und ist die Position glatt, wird eine Buy-Limitorder um den Pending-Abstand in Prozent unter dem aktuellen Schlusskurs registriert.
- **Short-Einstieg**: Steigt DeMarker im Einstiegsfenster durch 0,7 und ist die Position glatt, wird eine Sell-Limitorder um den Pending-Abstand in Prozent über dem aktuellen Schlusskurs registriert.
- **Ausstieg**: Ein unausgeführtes Limit wird bei Ersetzung durch ein neues Signal oder nach Ablauf seines Vier-Kerzen-Zählers storniert. Nach einer Ausführung schließt der Positionsschutz bei 1,2% Gewinn oder 0,6% Verlust.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| DeMarker Length | 14 | Mittelungslänge des DeMarker-Oszillators. |
| Lower level | 0.3 | Ein Abwärtskreuzen dieses Werts erzeugt ein Long-Signal. |
| Upper level | 0.7 | Ein Aufwärtskreuzen dieses Werts erzeugt ein Short-Signal. |
| Pending indent, % | 0.1 | Abstand des Pending-Preises vom Schlusskurs der Signalkerze. |
| Pending life, bars | 4 | Anzahl späterer abgeschlossener Kerzen bis zur Stornierung einer unausgeführten Order. |
| Entry window start | 07:00:00 | Erste vom Einstiegsfilter zugelassene Kerzenzeit. |
| Entry window end | 20:59:59 | Letzte vom Einstiegsfilter zugelassene Kerzenzeit. |
| Volume | 1 | Größe jeder Pending-Einstiegsorder. |
| Take profit, % | 1.2 | Günstige prozentuale Bewegung ab dem ausgeführten Einstiegspreis. |
| Stop loss, % | 0.6 | Ungünstige prozentuale Bewegung ab dem ausgeführten Einstiegspreis. |
| Candles | 00:15:00 | Zeitrahmen der abgeschlossenen Signalkerzen. |

## Diagrammdetails

- Für die untere Kreuzung liegt die Konstante 0,3 an Crossing Input Up und DeMarker an Input Down; für die obere Kreuzung liegt DeMarker an Input Up und 0,7 an Input Down.
- Zwei logische AND-Blöcke verbinden die jeweilige Kreuzung, das Ergebnis des Zeitfensters und die Prüfung auf glatte Position; kerzenweise Flags machen aus jedem gültigen Signal einen einzelnen Trigger.
- Formelblöcke berechnen `close × (1 − indent / 100)` für Buy und `close × (1 + indent / 100)` für Sell; zwei Registrierungsblöcke senden diese Preise anschließend als Limitorders.
- Jedes Signal startet einen eigenen N-values-Zähler. Nach vier späteren Kerzen löst dessen Ausgabe den passenden Stornoblock aus; vor jeder neuen Order erhalten beide Stornoblöcke außerdem den Auftrag, ältere Pending-Einstiege zu entfernen.
- Ein Trades-for-order-Block überwacht jedes registrierte Limit. Nur sein Ausführungsereignis erreicht Position protection, daher kann eine wartende oder stornierte Order weder Stop noch Take-Profit aktivieren.
- Aktuelle Designer-Versionen bieten am Registrierungsblock ebenfalls einen MyTrade-Ausgang; die separaten Trades-for-order-Blöcke bleiben hier erhalten, um die Kette von der Order zur Ausführung sichtbar zu machen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
