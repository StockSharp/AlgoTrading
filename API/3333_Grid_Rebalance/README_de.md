# Strategie zur Neuausrichtung des Netzes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Grid Rebalance Strategy ist eine High-Level-StockSharp-Portierung des Expertenberaters „Grid“ von Mission Automate. Die Strategie wechselt zwischen langen und kurzen Grid-Zyklen und behält immer eine Leiter von Limit-Orders in der aktiven Richtung bei. Sobald die Gesamtposition ein gemeinsames Take-Profit-Niveau erreicht, wird der Zyklus geschlossen, alle ausstehenden Aufträge werden entfernt und der nächste Zyklus beginnt in die entgegengesetzte Richtung.

## Wie es funktioniert
1. **Zyklusstart** – Wenn keine Positionen oder ausstehenden Aufträge vorhanden sind, eröffnet die Strategie eine Marktposition in der durch `FirstTradeSide` definierten Richtung unter Verwendung von `StartVolume`-Lots.
2. **Platzierung des Rasters** – Nach jeder ausgeführten Order in der aktiven Richtung platziert der Algorithmus eine neue Limit-Order im Abstand von `GridStepPoints` (vom Instrument in Preis umgewandelt `PriceStep`). Das Volumen der nächsten Bestellung entspricht dem Volumen der zuletzt ausgeführten Bestellung multipliziert mit `LotMultiplier`.
3. **Durchschnittsbasierter Take-Profit** – Für jede ausgeführte Order wird der gewichtete durchschnittliche Einstiegspreis neu berechnet. Der Take-Profit für den gesamten Korb wird auf den Durchschnittspreis plus/minus `TargetPoints` gesetzt (auch umgerechnet über `PriceStep`). Kerzenhochs und -tiefs werden zur Modellierung des maklerseitigen Triggerverhaltens verwendet.
4. **Zyklusabschluss** – Wenn das Take-Profit-Niveau erreicht ist, schließt die Strategie die gesamte Position mit einer Marktorder, storniert verbleibende ausstehende Orders, merkt sich die Richtung des abgeschlossenen Zyklus und kehrt die Richtung für den nächsten um.

## Parameter
- `FirstTradeSide` – Richtung des ersten Zyklus (`Buy` oder `Sell`). Bei jedem abgeschlossenen Zyklus wird die Richtung automatisch umgedreht.
- `StartVolume` – Losgröße der anfänglichen Marktorder in jedem Zyklus.
- `LotMultiplier` – Multiplikator, der bei der Vorbereitung der nächsten Rasterebene auf das zuletzt ausgeführte Auftragsvolumen angewendet wird. Werte größer als eins erzeugen einen Martingal-ähnlichen Verlauf.
- `GridStepPoints` – Abstand zwischen Rasterebenen, ausgedrückt in Punkten. Die Strategie multipliziert ihn mit `Security.PriceStep`, um die absolute Preisdifferenz zu erhalten.
- `TargetPoints` – Take-Profit-Distanz vom gewichteten durchschnittlichen Einstiegspreis, gemessen in Punkten.
- `CandleType` – Kerzenserie zur Überwachung von Preisextremen zur Auslösung von Ausstiegen.

## Risikomanagement und Verhalten
- Es wird kein expliziter Stop-Loss verwendet; Das Raster erhöht weiterhin das Engagement, während sich der Markt gegen die Position bewegt.
- Es ist jeweils nur eine ausstehende Bestellung aktiv. Sobald die Bestellung ausgeführt ist, wird die nächste Ebene sofort eingeplant.
- Der Zyklus kann erst beginnen, wenn sowohl die Position als auch die ausstehende Warteschlange leer sind und das Instrument über einen gültigen `PriceStep` verfügt.
- Durch die Konvertierung bleiben alle Berechnungen innerhalb der Strategie, ohne globale Sammlungen oder Indikatorpuffer zu berühren, und folgen den Projektregeln.
- Ausstehende Aufträge werden jedes Mal storniert, wenn ein Zyklus endet, wodurch verwaiste Limits aus früheren Zyklen verhindert werden.

## Notizen
- Alle punktbasierten Einstellungen werden mit `Security.PriceStep` in Preise umgewandelt. Wenn der Schritt Null ist, wartet die Strategie, bis das Instrument ihn bereitstellt.
- Die Implementierung basiert je nach Bedarf ausschließlich auf der übergeordneten Ebene API (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`, `BuyLimit`, `SellLimit`).
- Eine Python-Version ist in dieser Aufgabe absichtlich nicht enthalten.
