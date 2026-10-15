# Einfache EMA-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet eine Kreuzung zweier exponentieller gleitender Durchschnitte mit integriertem Stop-Loss und Take-Profit.

Sie kauft, wenn die schnelle EMA die langsame EMA von unten kreuzt, und verkauft, wenn sie von oben kreuzt.

## Details

- **Einstiegskriterien**: Kreuzung der schnellen EMA mit der langsamen EMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Kreuzung oder Stop-Orders.
- **Stops**: Ja.
- **Standardwerte**:
  - `Periods` = 17
  - `StopLoss` = 31 (absolut)
  - `TakeProfit` = 69 (absolut)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
