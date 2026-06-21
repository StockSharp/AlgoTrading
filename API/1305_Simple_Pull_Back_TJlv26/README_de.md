# Einfache Pull-Back-Strategie TJlv26
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft, wenn der Preis über dem langen SMA, unter dem kurzen SMA und RSI(3) unter 30 liegt, innerhalb eines bestimmten Datumsbereichs. Sie schließt mit prozentualen Stop-Loss- und Take-Profit-Werten oder wenn der Preis über dem kurzen SMA aber unter dem Tief der vorherigen Kerze liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs > langer SMA, Schlusskurs < kurzer SMA, RSI(3) < 30, Zeit zwischen StartDate und EndDate.
- **Ausstiegskriterien**:
  - Stop Loss: Preis ≤ Einstiegspreis × (1 − StopLossPercent/100).
  - Take Profit: Preis ≥ Einstiegspreis × (1 + TakeProfitPercent/100).
  - Schließen, wenn Preis > kurzer SMA und Preis < Tief der vorherigen Kerze.
- **Indikatoren**: SMA, RSI.
- **Stops**: Ja.
- **Richtung**: Nur Long.
