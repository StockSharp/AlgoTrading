# Strategiediagramm für synchronisierte MA-Kreuzungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm synchronisiert eine Stundenkerze mit den vollständigen Werten eines schnellen und eines langsamen exponentiellen gleitenden Durchschnitts, bevor es deren Kreuzung auswertet. Beim ersten Signal eröffnet es eine Einheit; jedes Gegensignal nutzt das Doppelte des Basisvolumens und dreht die Position ohne Größenanstieg.

![schema](schema.svg)

## Strategieübersicht

- Eine gemeinsame Serie abgeschlossener Stundenkerzen speist EMA(20) und EMA(50), sodass beide Durchschnitte dieselbe Preisbasis haben.
- Der Sync-Block bildet eine Stundengruppe aus Kerze, schneller EMA und langsamer EMA; aus einer unvollständigen Gruppe entsteht keine Kreuzungsentscheidung.
- Crossing liefert `true`, wenn die schnelle EMA über die langsame steigt, und `false`, wenn sie darunter fällt; ein NOT-Block wandelt den zweiten Wert in den Short-Trigger um.
- Prüfungen des Positionsvorzeichens wählen entweder eine Eröffnung mit Basisvolumen aus glatter Position oder eine Umkehr mit doppeltem Basisvolumen von der Gegenseite.
- Das Diagramm erhält die synchronisierte Kerze, beide synchronisierten EMA-Werte und sämtliche Strategieausführungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Kreuzt die synchronisierte EMA(20) über EMA(50), wird aus glatter Position ein Basisvolumen und aus einer Short-Position zwei Basisvolumen gekauft; übrig bleibt ein Long von einem Basisvolumen.
- **Short-Einstieg**: Kreuzt die synchronisierte EMA(20) unter EMA(50), wird aus glatter Position ein Basisvolumen und aus einer Long-Position zwei Basisvolumen verkauft; übrig bleibt ein Short von einem Basisvolumen.
- **Ausstieg**: Es gibt keinen separaten Stop, kein Ziel und keinen Zeitausstieg. Die nächste Gegenkreuzung löst eine Marktumkehr aus, deren Volumen die aktuelle Einheit schließt und eine Einheit in der neuen Richtung eröffnet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast EMA Length | 20 | Länge des schnelleren exponentiellen gleitenden Durchschnitts. |
| Slow EMA Length | 50 | Länge des langsameren exponentiellen gleitenden Durchschnitts. |
| Candles | 01:00:00 | Zeitrahmen der abgeschlossenen Kerzen für beide Durchschnitte. |
| Sync interval | 01:00:00 | Zeitbereich, mit dem Sync Kerze und beide Indikatorwerte gruppiert. |
| Base volume | 1 | Aus glatter Position eröffnete Größe; eine Umkehr verwendet automatisch das Doppelte. |

## Diagrammdetails

- Die Kerzenausgabe aktualisiert zuerst Positionsabbild, Null- und Volumenkonstante und speist dann EMA(20) und EMA(50); ihre letzte Verbindung geht an den dritten Sync-Eingang und vervollständigt die Gruppe nach beiden Berechnungen.
- Sync Input 1 erhält EMA(20), Input 2 erhält EMA(50) und Input 3 die Kerze. Alle drei gekoppelten Ausgänge sind verbunden; nach jeder vollständigen Gruppe wird der Block geleert.
- Sync Output 1 und Output 2 speisen Crossing Input Up und Input Down. Output 3 durchläuft einen ClosePrice-Konverter und liefert zugleich die Kerzenserie für das Diagramm.
- Vier logische Routen unterscheiden glatte, Long- und Short-Positionen für beide Kreuzungsrichtungen. Ein gleichgerichtetes Signal kann eine bestehende Position nicht vergrößern.
- Vier Modify-position-Blöcke senden Marktorders: Zwei Eröffnungen verwenden das Basisvolumen, zwei Umkehrungen die Formel `2 × Basisvolumen`.
- Da jede Position mit dem Basisvolumen eröffnet wird, entspricht der Umkehrbetrag `abs(position) + Basisvolumen` und hält den Betrag der Exposition konstant.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
