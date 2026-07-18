# Charles 1.3.7 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert symmetrische Stop-Orders ober- und unterhalb des aktuellen Preises und verwendet Trailing-Exits, um Ausbrüche zu erfassen.

## Parameter

- **Anchor** – Abstand in Preisschritten zum Platzieren der Stop-Orders.
- **XFactor** – Multiplikator für das Ordervolumen.
- **Trailing Stop** – Trailing-Stop-Abstand in Preisschritten.
- **Trailing Profit** – Gewinnschwelle für den Positionsausstieg.
- **Stop Loss** – Fester Stop Loss in Preisschritten (0 deaktiviert ihn).
- **Volume** – Basis-Ordervolumen.
- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen.

## Handelslogik

1. Wenn keine Position offen ist, werden bestehende Orders storniert und sowohl ein Buy Stop als auch ein Sell Stop bei `Anchor` Schritten vom letzten Kerzenschlusskurs platziert.
2. Wenn eine Position eröffnet wird, wird die entgegengesetzte Stop-Order storniert. Der Einstiegspreis wird für Ausstiegsberechnungen gespeichert.
3. Bei einer Long-Position wird die Position geschlossen, wenn der Gewinn `Trailing Profit` erreicht oder der Preis um `Stop Loss` fällt. Bei einer Short-Position ist die Logik gespiegelt.

Die Strategie ist als Beispiel für Ausbruchshandel mit einfachem Risikomanagement konzipiert.
