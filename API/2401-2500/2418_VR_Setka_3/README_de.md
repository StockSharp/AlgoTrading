# VR Setka 3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **VR Setka 3-Strategie** implementiert einen gitterbasierten Handelsansatz. Die Strategie platziert symmetrische Buy- und Sell-Limit-Orders rund um den aktuellen Marktpreis. Nachdem eine Order ausgeführt wurde, wird das Take-Profit-Niveau anhand des durchschnittlichen Einstiegspreises aller Positionen in der aktiven Richtung neu berechnet. Neue Grid-Orders werden mit zunehmendem Abstand und optional mit zunehmendem Volumen (Martingal) platziert.

## Parameter
- **Start Offset** – anfänglicher Abstand vom aktuellen Kurs für das erste Paar von Limit-Orders.
- **Take Profit** – Abstand vom durchschnittlichen Einstiegspreis, bei dem alle Positionen mit Gewinn geschlossen werden.
- **Grid Distance** – Basisschritt zwischen Grid-Niveaus.
- **Step Distance** – zusätzlicher Abstand für jedes weitere Grid-Niveau.
- **Use Martingale** – wenn aktiviert, erhöht jede neue Grid-Order ihr Volumen mit dem Multiplikator.
- **Martingale Multiplier** – Faktor für die Volumenserhöhung bei aktivem Martingal.
- **Volume** – Basisordervolumen für das erste Niveau.
- **Candle Type** – Zeitrahmen zur Synchronisierung der Strategieoperationen.

## Algorithmus
1. Zu Beginn platziert die Strategie ein **Buy Limit** unterhalb und ein **Sell Limit** oberhalb des aktuellen Kurses.
2. Wenn eine Seite ausgeführt wird, wird die entgegengesetzte Order storniert.
3. Die Strategie berechnet ein gemeinsames Take-Profit-Niveau beim Durchschnittspreis ± *Take Profit*.
4. Wenn sich der Kurs gegen die Position bewegt, wird eine neue Limit-Order bei **Grid Distance + Step Distance × Niveau** vom Durchschnittspreis platziert. Das Volumen erhöht sich, wenn Martingal aktiviert ist.
5. Wenn der Kurs das Take-Profit-Niveau erreicht, werden alle Positionen in dieser Richtung geschlossen und das Grid wird zurückgesetzt.

## Hinweise
- Die Strategie öffnet keine Positionen in beiden Richtungen gleichzeitig.
- Angemessenes Risikomanagement ist erforderlich, da Martingal die Positionsgröße schnell erhöhen kann.
- Funktioniert mit jedem von StockSharp unterstützten Instrument, solange der gewählte Kerzentyp verfügbar ist.
