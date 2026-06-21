# VR Setka P2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein gitterbasierter Ansatz, der vom MetaTrader 4-Experten `VR---SETKAp2` übertragen wurde.
Sie handelt, wenn der Tagesschluss vom Tageshoch oder -tief um einen bestimmten Prozentsatz abweicht.
Die Strategie eröffnet Long-Positionen nach einem signifikanten Rückgang vom Tageshoch und
Short-Positionen nach einem signifikanten Anstieg vom Tagestief. Sobald eine Position gehalten wird,
wird sie bei einer festen Take-Profit-Distanz geschlossen. Das Volumen kann optional über ein einfaches Martingale-Schema erhöht werden.

## Parameter
- **TakeProfit** – Abstand zum Gewinnziel in Preisschritten.
- **Lot** – Basisvolumen für jeden Trade.
- **Percent** – prozentualer Schwellenwert, berechnet aus der Tagesspanne.
- **UseMartingale** – aktiviert die Volumenerhöhung beim Hinzufügen zu einer Verlustposition.
- **Slippage** – erlaubter Preisschlupf für Aufträge.
- **Correlation** – Versatz bei der Berechnung der Gitterlevel.
- **Candle Type** – Zeitrahmen für die Berechnungen (standardmäßig täglich).

## Logik
1. Tageskerzen abonnieren.
2. Für jede abgeschlossene Kerze die prozentualen Abweichungen vom Tageshoch und -tief berechnen.
3. Long oder Short eingehen, abhängig von der Abweichung und der Richtung der vorherigen Kerze.
4. Die Position schließen, wenn das Take-Profit-Level erreicht wird.

Diese Implementierung zeigt, wie ein klassischer MetaTrader-Gitterexperte auf die StockSharp-High-Level-API portiert werden kann.
