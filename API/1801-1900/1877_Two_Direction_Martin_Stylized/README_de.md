# Strategie Two Direction Martin Stylized
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert einen vereinfachten bidirektionalen Martingale-Ansatz. Beim Start werden sowohl Long- als auch Short-Positionen eröffnet und Limitorders in einem konfigurierbaren Abstand platziert, um Gewinne zu sichern.

## Funktionsweise
1. Berechnet den Spread und legt den Take-Profit-Abstand als Prozentsatz des aktuellen Ask-Preises fest.
2. Sendet eine anfängliche Verkaufs-Marktorder mit einem Kauf-Limit-Ziel unterhalb des Bids und eine Kauf-Marktorder mit einem Verkauf-Limit-Ziel oberhalb des Asks.
3. Wenn eine der Limitorders fehlt oder der Preis den vordefinierten Bereich verlässt, berechnet der Algorithmus die Volumen anhand von `Same Side %` neu und ersetzt die ausstehenden Orders. Zusätzliche Marktorders werden bei Bedarf gesendet, um die Position auszugleichen.
4. Alle Orders werden in Teile aufgeteilt, die den Parameter `Volume Limit` nicht überschreiten.

## Parameter
- **Take Profit %** – Abstand vom aktuellen Preis für Gewinnziele.
- **Base Volume** – Mindestvolumen für jede anfängliche Order.
- **Volume Limit** – Maximalvolumen für einen einzelnen Order-Teil.
- **Same Side %** – Prozentsatz des Gesamtvolumens, das der dominanten Seite zugewiesen wird.
- **Candle Type** – Kerzentyp, der als Zeitgeber verwendet wird.
