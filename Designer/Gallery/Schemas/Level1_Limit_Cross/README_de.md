# EMA-Cross-Limiteinstiege aus Level 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm übernimmt das EMA(14)/EMA(50)-Signal aus TwoDLimitsStrategy und macht die Orderverwaltung sichtbar. Jede fertige Fünfminutenkerze hält Level 1 fest, platziert ein Limit hinter dem besten Kurs und storniert es beim Gegenkreuz.

![schema](schema.svg)

## Strategieübersicht

- Fertige Fünfminutenkerzen speisen schnelle EMA(14) und langsame EMA(50); zwei Crossing-Blöcke erkennen beide Richtungen.
- BestBidPrice und BestAskPrice kommen asynchron aus Level 1 und werden vor der Preisberechnung je Kerze gehalten.
- Kauflimits liegen 0,02% unter dem besten Bid, Verkaufslimits 0,02% über dem besten Ask, damit Order cancellation sichtbar arbeitet.
- Jede Ausführung startet eine N-values-Pause von 100 Kerzen; bis zum Ende sind Einstieg und Schutzpreis gesperrt.
- Danach arbeitet Position protection mit 0,3% Stop und 0,6% Ziel.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: EMA(14) kreuzt über EMA(50), Position ist null oder short, ein positiver Bid liegt vor und die Pause ist beendet. Ein Kauflimit wird unter dem Bid platziert; abs(Position) plus Basisvolumen schließt Short und eröffnet Long in einer Ausführung.
- **Short-Einstieg**: EMA(14) kreuzt unter EMA(50), Position ist null oder long, ein positiver Ask liegt vor und die Pause ist beendet. Ein Verkaufslimit wird über dem Ask mit derselben Netto-Umkehrmenge platziert.
- **Ausstieg**: Das Gegenkreuz storniert das letzte noch aktive Gegenlimit. Nach Ausführung und 100 Kerzen beendet Position protection bei 0,3% Stop oder 0,6% Ziel; ein ausgeführtes Gegenlimit kann die Position ebenfalls drehen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 00:05:00 | Fertiger Kerzenzeitrahmen für EMAs, Quote-Abgriff, Pause und Schutzprüfung. |
| Fast EMA Length | 14 | Anzahl der Fünfminutenwerte in der schnellen exponentiellen Durchschnittslinie. |
| Slow EMA Length | 50 | Anzahl der Fünfminutenwerte in der langsamen exponentiellen Durchschnittslinie. |
| Quote Offset | 0.02% | Prozentualer Abstand vom besten Kurs: unter Bid für Kauf, über Ask für Verkauf. |
| Base Volume | 1 | Neue Positionsgröße nach Verrechnung einer bestehenden Gegenposition. |
| Cooldown, candles | 100 | Fertige Kerzen nach jeder Ausführung bis Einstieg und Schutz wieder aktiv sind. |
| Stop Loss | 0.3% | Prozentualer Abstand des Schutzstops vom Einstiegskurs. |
| Take Profit | 0.6% | Prozentualer Abstand des Gewinnziels vom Einstiegskurs. |

## Diagrammdetails

- Der C#-Code steigt zum Markt ein; das Diagramm nutzt bewusst Level-1-Limits, um Registrierung und Storno zu zeigen.
- Die ursprünglichen 200/400 Preisschritte werden beim BTCUSDT-Replay als ungefähr 0,3%/0,6% mit unverändertem Verhältnis 1:2 dargestellt.
- Der Code prüft Stop und Ziel in den ersten 100 Kerzen nach einer Ausführung nicht; das Sperren des Preises bildet diese Reihenfolge ab.
- Quote-Latches richten den asynchronen Level-1-Strom am Kerzentakt aus. Preisrundung ist deaktiviert, da im Replay ein Preisschritt fehlen kann.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
