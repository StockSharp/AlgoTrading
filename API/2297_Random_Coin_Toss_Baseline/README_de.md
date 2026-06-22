# Zufälliger Münzwurf Basisstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das klassische GuruTrader-Beispiel, bei dem die Handelsrichtung durch einen Münzwurf bestimmt wird.
Bei jeder abgeschlossenen Kerze wird, wenn keine Position offen ist, eine Pseudozufallszahl generiert und als Münzwurf behandelt.
Kopf öffnet eine Long-Position, während Zahl eine Short-Position öffnet.
Jeder Trade wendet feste Take-Profit- und Stop-Loss-Abstände in absoluten Preiseinheiten an.

## Parameter
- **Take Profit** – Abstand vom Einstiegspreis zur Take-Profit-Order.
- **Stop Loss** – Abstand vom Einstiegspreis zur Stop-Loss-Order.
- **Use Time Seed** – initialisiert den Zufallsgenerator mit der aktuellen Zeit für unterschiedliche Ergebnisse bei jedem Lauf. Wenn deaktiviert, wird ein fester Seed verwendet.
- **Candle Type** – Kerzentyp, der von der Strategie verarbeitet wird.

## Handelslogik
1. Warten auf eine abgeschlossene Kerze.
2. Sicherstellen, dass die Strategie handeln darf und keine Position offen ist.
3. Einen Zufallswert generieren und die Richtung basierend auf dem Münzwurf wählen.
4. Die Position mit den vordefinierten Stop-Loss- und Take-Profit-Abständen schützen.

**Warnung:** Diese Strategie dient ausschließlich Bildungszwecken und sollte niemals auf echten Konten verwendet werden.
