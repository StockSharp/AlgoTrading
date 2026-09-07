# Strategiediagramm für Session-Sweeps des Vortags mit Meldung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt Fehlausbrüche aus der Handelsspanne des vorangegangenen UTC-Tages. Um Mitternacht fixiert es dessen Hoch und Tief, wartet darauf, dass eine Fünfzehn-Minuten-Kerze eine Grenze überschreitet und wieder innerhalb schließt, eröffnet eine Gegenposition und schreibt für jede akzeptierte Signalkerze eine Meldung in das Protokoll.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünfzehn-Minuten-Kerzen liefern Hoch, Tief und Schlusskurs für alle Entscheidungen.
- Highest(96) und Lowest(96) decken einen vollständigen Tag ab. Previous-value-Blöcke mit Verschiebung 1 schließen die neue Mitternachtskerze vor dem Erfassen der Spanne aus.
- Das erfasste Hoch und Tief bleiben von 00:00 UTC bis zum Ende dieses Kalendertages unverändert. Die Einstiegsprüfung beginnt um 00:15 UTC.
- Ein Fehlausbruch über das gehaltene Hoch erzeugt ein Short-Setup; ein Fehlausbruch unter das gehaltene Tief erzeugt ein Long-Setup.
- Einstiege sind nur bei neutraler Position erlaubt und werden mit Marktaufträgen über eine Einheit ausgeführt. Treten beide Sweep-Bedingungen in derselben Kerze auf, hat der Sweep des Hochs Vorrang.
- Jeder ausgeführte Einstieg erhält einen festen Take-Profit von 1% und einen Stop-Loss von 1%, die anhand abgeschlossener Kerzenschlüsse als Marktausstiege ausgelöst werden.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Zwischen 00:15 und 23:59:59 UTC muss das Kerzentief unter dem gehaltenen Tief des Vortages und der Schlusskurs darüber liegen, es darf kein gleichzeitiges High-Sweep-Setup vorliegen, und Position muss null sein. Eine Einheit wird zum Markt gekauft.
- **Short-Einstieg**: Im selben Zeitfenster muss das Kerzenhoch über dem gehaltenen Hoch des Vortages und der Schlusskurs darunter liegen, während Position null ist. Eine Einheit wird zum Markt verkauft.
- **Meldung**: Jedes akzeptierte Long- oder Short-Signal durchläuft einmal einen kerzenbezogenen Flag. String Formatter fügt den Schlusskurs der Signalkerze in eine Nachricht ein, die Notification an das Strategieprotokoll sendet.
- **Ausstieg**: Position protection schließt den ausgeführten Einstieg nach einer günstigen Bewegung von 1% oder einer ungünstigen Bewegung von 1%. Beide Schutzausstiege sind Marktaufträge.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Kerzen | 00:15:00 | Zeitrahmen der abgeschlossenen Kerzen für Spannenermittlung und Entscheidungen. |
| Highest-Länge | 96 | Anzahl der Fünfzehn-Minuten-Hochs in einer vollständigen Tagesspanne. |
| Highest-Quelle | Nicht gesetzt | Es ist kein alternatives Eingabefeld für den Indikator ausgewählt. |
| Lowest-Länge | 96 | Anzahl der Fünfzehn-Minuten-Tiefs in einer vollständigen Tagesspanne. |
| Lowest-Quelle | Nicht gesetzt | Es ist kein alternatives Eingabefeld für den Indikator ausgewählt. |
| Volumen | 1 | Größe jedes Markteinstiegs. |
| Take-Profit | 1% | Günstige Bewegung ab Ausführung, die den Schutz aktiviert. |
| Stop-Loss | 1% | Ungünstige Bewegung ab Ausführung, die den Schutz aktiviert. |
| Nachlaufender Stop-Loss | false | Hält die Stop-Grenze fest. |
| Marktaufträge verwenden | true | Sendet ausgelöste Schutzausstiege als Marktaufträge. |

## Diagrammdetails

- HighPrice- und LowPrice-Konverter speisen die beiden rollierenden Indikatoren mit 96 Werten; ClosePrice liefert die Rückkehrtests, den Meldungstext und die Schutzprüfung.
- Jedes rollierende Ergebnis läuft durch einen abgeschlossenen Previous-value-Block mit Verschiebung 1. Von 00:00 bis 00:14:59 UTC übernehmen Erfassungs-Variables den vorherigen Wert, und Halte-Variables veröffentlichen ihn mit jeder Kerze.
- Vier Vergleiche erkennen einen Durchstoß des Hochs mit Schluss unter dem gehaltenen Hoch oder einen Durchstoß des Tiefs mit Schluss über dem gehaltenen Tief. Working time begrenzt beide Kombinationen auf 00:15-23:59:59 UTC.
- Der Positionswert wird mit jeder Kerze abgetastet und mit null verglichen. Das Short-Gate verbindet den High-Sweep mit der Neutralitätsprüfung; das Long-Gate verlangt zusätzlich das invertierte High-Sweep-Signal, um den Vorrang zu erhalten.
- Die beiden akzeptierten Gates lösen Buy und Sell in Modify-position-Blöcken aus und werden für die Meldung zusammengeführt. Ein mit jeder Kerze zurückgesetzter Flag verhindert doppelte Meldungen innerhalb eines Signalereignisses, ohne spätere Signale am selben Tag zu unterdrücken.
- Einstiegsausführungen aktivieren den gemeinsamen Position-protection-Block. Sein Referenzpreis wird durch den abgeschlossenen Schluss aktualisiert, daher werden intrabar erreichte Grenzen mit Rückkehr vor Kerzenschluss nicht erfasst.
- Für die erste nutzbare Spanne sind 96 vorangegangene Fünfzehn-Minuten-Kerzen erforderlich. Die Niveaus werden nur beim Mitternachtsschnappschuss erneuert und bleiben bis zum nächsten UTC-Tag unverändert.
- Das Diagramm zeigt Kerzen, rollierende und gehaltene Tagesgrenzen, Kauf- und Verkaufsausführungen sowie sämtliche Strategieausführungen einschließlich der Schutzausstiege.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, führen Sie sie im Backtester mit genügend Historie für die erste Tagesspanne aus und passen Sie Zeitrahmen, Spannlängen, Schutzabstände und Volumen vor dem Live-Handel an das Instrument an.
