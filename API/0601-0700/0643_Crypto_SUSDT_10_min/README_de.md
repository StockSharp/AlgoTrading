# Crypto-Strategie SUSDT 10 min
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine einfache EMA-Kreuzungsstrategie, die long einsteigt, wenn der Kurs über der EMA schließt und unter ihr öffnet, und short bei der entgegengesetzten Bedingung. Stop-Loss und Take-Profit werden als Prozentsätze vom Einstiegspreis definiert.

## Details

- **Einstiegskriterien**:
  - **Long**: `close > EMA` und `open < EMA`
  - **Short**: `close < EMA` und `open > EMA`
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss.
- **Stops**: Ja, sowohl Take-Profit als auch Stop-Loss.
- **Standardwerte**:
  - `CandleType` = 10 Minuten
  - `EmaLength` = 24
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
