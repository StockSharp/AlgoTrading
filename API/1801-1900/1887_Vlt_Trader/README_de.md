# Vlt Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt Perioden sehr geringer Volatilität und bereitet Ausbruchsorders vor. Wenn die Spanne der aktuellen Kerze die kleinste über den angegebenen Rückblickzeitraum wird, platziert die Strategie Buy-Stop- und Sell-Stop-Orders um die vorherige Kerze.

## Parameter
- **Period** – Rückblickzeitraum für die Berechnung der minimalen Spanne.
- **Pending level** – Abstand in Ticks vom vorherigen Hoch/Tief zum Platzieren der Stop-Orders.
- **Stop loss** – Schutz-Stop in Ticks.
- **Take profit** – Gewinnziel in Ticks.
- **Candle type** – Zeitrahmen für die Analyse.

## Logik
1. Für jede abgeschlossene Kerze wird die Spanne (`High - Low`) berechnet.
2. Die kleinste Spanne über die letzten *Period* Kerzen wird verfolgt.
3. Wenn die aktuelle Spanne ein neues Minimum erreicht, werden bestehende Orders storniert und Stop-Orders oberhalb und unterhalb der vorherigen Kerze mit dem angegebenen Versatz platziert.
4. `StartProtection` verwaltet Stop-Loss und Take-Profit, sobald eine Position eröffnet wird.
