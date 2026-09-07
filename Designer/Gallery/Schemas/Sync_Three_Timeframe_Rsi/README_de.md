# Strategiediagramm zur RSI-Übereinstimmung in drei Zeitrahmen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm wartet auf gebildete RSI-Werte aus Fünf-, Fünfzehn- und Dreißig-Minuten-Kerzen, nimmt alle drei bei jedem Schluss des langsamen Zeitrahmens auf und bewertet sie als eine synchronisierte Gruppe. Übereinstimmung unter 30 eröffnet oder dreht auf Long; Übereinstimmung über 70 eröffnet oder dreht auf Short.

![schema](schema.svg)

## Strategieübersicht

- Drei Ströme abgeschlossener Kerzen berechnen unabhängige RSI(14)-Werte in Zeitrahmen von 5, 15 und 30 Minuten.
- Das Dreißig-Minuten-RSI-Ereignis nimmt den letzten Wert jedes Stroms auf. Sync gruppiert die drei Stichproben in einem Intervall von 30 Minuten und leert sie nach der Ausgabe.
- Eine Entscheidung fällt einmal je gebildetem Dreißig-Minuten-RSI-Wert und erst nachdem alle drei synchronisierten Ausgänge ihre Vergleiche aktualisiert haben.
- Alle drei RSI-Werte unter der Kaufschwelle ergeben ein Long-Setup. Alle drei über der Verkaufsschwelle ergeben ein Short-Setup.
- Positions-Gates verhindern einen weiteren Auftrag in der bereits gehaltenen Richtung. Ein entgegenstehendes Setup sendet eine Marktumkehr über zwei Einheiten; ein Einstieg aus neutraler Position nutzt eine Einheit.
- Es gibt keinen unabhängigen Stop-Loss, Take-Profit, Zeitausstieg oder Abkühlzeitraum. Das nächste qualifizierte Gegensetup ist der einzige Ausstieg und stellt sofort die neue Richtung her.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Synchronisierte Werte von Fast RSI, Middle RSI und Slow RSI müssen strikt unter 30 liegen, und Position muss kleiner oder gleich null sein. `Base Volume + abs(sign(Position))` wird zum Markt gekauft: eine Einheit aus neutraler Position oder zwei Einheiten aus dem vom Diagramm erzeugten Short über eine Einheit.
- **Short-Einstieg**: Alle drei synchronisierten RSI-Werte müssen strikt über 70 liegen, und Position muss größer oder gleich null sein. Dieselbe berechnete Menge wird zum Markt verkauft: eine Einheit aus neutraler Position oder zwei Einheiten aus dem vom Diagramm erzeugten Long über eine Einheit.
- **Ausstieg**: Ein Long wird nur durch ein qualifiziertes Short-Setup geschlossen, ein Short nur durch ein qualifiziertes Long-Setup. Der Umkehrauftrag schließt zugleich die Ein-Einheiten-Exposition und eröffnet eine Einheit in der neuen Richtung.
- **Positionsumfang**: Die normalisierte Formel ist für Positionen bestimmt, die dieses Diagramm erzeugt. Hat eine externe Position einen Betrag von mehr als einer Basiseinheit, erreicht ein Umkehrauftrag über zwei Einheiten nicht zwingend das Ziel.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Schnelle Kerzenserie | 00:05:00 | Abgeschlossene Fünf-Minuten-Kerzen für Fast RSI. |
| Mittlere Kerzenserie | 00:15:00 | Abgeschlossene Fünfzehn-Minuten-Kerzen für Middle RSI. |
| Langsame Kerzenserie | 00:30:00 | Abgeschlossene Dreißig-Minuten-Kerzen, die synchronisierte Entscheidungen planen. |
| Fast-RSI-Länge | 14 | Mittelungslänge des RSI im schnellen Kerzenstrom. |
| Fast-RSI-Quelle | Nicht gesetzt | Es ist kein alternatives Eingabefeld für den Indikator ausgewählt. |
| Middle-RSI-Länge | 14 | Mittelungslänge des RSI im mittleren Kerzenstrom. |
| Middle-RSI-Quelle | Nicht gesetzt | Es ist kein alternatives Eingabefeld für den Indikator ausgewählt. |
| Slow-RSI-Länge | 14 | Mittelungslänge des RSI im langsamen Kerzenstrom. |
| Slow-RSI-Quelle | Nicht gesetzt | Es ist kein alternatives Eingabefeld für den Indikator ausgewählt. |
| Kaufschwelle | 30 | Strikte Obergrenze der Übereinstimmung, die einen Long erlaubt. |
| Verkaufsschwelle | 70 | Strikte Untergrenze der Übereinstimmung, die einen Short erlaubt. |
| Basisvolumen | 1 | Zielposition und Größe des neutralen Einstiegs; Umkehrungen nutzen den doppelten Standardwert. |

## Diagrammdetails

- Jeder Candles-Block gibt nur abgeschlossene Werte aus und kann seinen Zeitrahmen aus kleineren gespeicherten Kerzen aufbauen. Jeder RSI gibt erst nach seinem eigenen Aufwärmen mit 14 Werten aus.
- Fast RSI und Middle RSI werden früher bereit als Slow RSI. Drei Variables vom Typ indicator value werden durch jedes Slow-RSI-Ereignis ausgelöst, sodass unvollständige Aufwärmintervalle nicht am Anfang der Sync-Warteschlange verbleiben.
- Sync besitzt genau drei verbundene Ein-/Ausgangspaare, Interval `00:30:00` und aktivierte Clear Sockets. Seine Ausgänge tragen den letzten schnellen, den letzten mittleren und den aktuellen langsamen RSI-Wert mit einer gemeinsamen Entscheidungszeit.
- Sechs Comparison-Blöcke wenden strikte Prüfungen `< Buy Threshold` und `> Sell Threshold` an. Ein boolescher Freigabeimpuls erreicht beide AND-Gates mit fünf Eingängen erst, nachdem alle sechs Vergleiche die aktuelle Gruppe verarbeitet haben.
- Current Position wird für den Long-Zweig mit `<=` und für den Short-Zweig mit `>=` gegen null verglichen. Diese Prüfungen erlauben einen neutralen Einstieg oder eine Umkehr der Gegenposition und unterdrücken wiederholte Einstiege in dieselbe Richtung.
- Formula berechnet `Base Volume + abs(sign(Position))`. Bei der verwalteten Exposition ist das Ergebnis 1 in neutraler Position und 2 in einer Position; auch bei mehreren Datenabonnements bleibt es begrenzt.
- Die Buy- und Sell-Modify-position-Blöcke verwenden NoCondition-Marktaktionen, weil Richtung und Zulässigkeit bereits von den äußeren Gates bestimmt sind. Schutz- und Chart-Blöcke sind nicht vorhanden.
- Schwellen sind bewusst von RSI-Längen und Kerzenserien getrennt. Eine Änderung von Zeitrahmen oder Länge verändert das Aufwärmen; Sync wartet dennoch vor jeder Bewertung auf ein frisches Stichprobentripel.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, führen Sie sie im Backtester mit ausreichend Historie zur Bildung des Dreißig-Minuten-RSI aus und passen Sie die drei Kerzenserien, RSI-Längen, Schwellen und das Basisvolumen vor dem Live-Handel an das Instrument an.
