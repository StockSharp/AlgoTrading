# HelloSmart-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert einen einfachen Grid-Trading-Ansatz, der Positionen nur in eine Richtung eröffnet. Eine neue Order wird jedes Mal platziert, wenn sich der Markt eine konfigurierte Anzahl von Ticks gegen den letzten Einstieg bewegt. Wenn das kumulative Positionsvolumen einen Schwellenwert erreicht, wird die nächste Ordergröße multipliziert. Alle Positionen werden geschlossen, wenn der Gesamtgewinn oder -verlust vordefinierte Grenzen erreicht.

## Parameter
- **Trade Direction** – 1 wählen, um nur Long-Positionen zu eröffnen, oder 2 für nur Short-Positionen.
- **Step** – Anzahl der Preis-Ticks, die der Markt bewegen muss, bevor eine weitere Position hinzugefügt wird.
- **Initial Lot** – Basisvolumen für die erste Order.
- **Threshold Volume** – kumuliertes Positionsvolumen, das die Lot-Multiplikation auslöst.
- **Maximum Lot** – Obergrenze für das Volumen einer einzelnen Order.
- **Profit Target** – Gewinnbetrag in Währung, nach dem alle Positionen geschlossen werden.
- **Loss Limit** – Verlustbetrag in Währung, nach dem alle Positionen geschlossen werden.
- **Lot Multiplier** – Faktor, der auf die nächste Order angewendet wird, wenn das Schwellvolumen überschritten wird.
- **Candle Type** – Kerzenserie zur Messung der Kursbewegung.
