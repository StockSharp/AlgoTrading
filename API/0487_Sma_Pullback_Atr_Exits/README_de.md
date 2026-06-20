# SMA-Pullback + ATR-Ausstiegs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie steigt bei Pullbacks ein, wenn ein kurzfristiger gleitender Durchschnitt über oder unter einem langfristigen Trenddurchschnitt liegt. Long-Positionen werden eröffnet, wenn der Kurs unter die schnelle SMA fällt, während diese über der langsamen SMA liegt. Short-Positionen werden eröffnet, wenn der Kurs über die schnelle SMA steigt, während diese unter der langsamen SMA liegt. Ausstiege verwenden Average True Range-Vielfache vom Einstiegspreis.

## Details

- **Einstiegskriterien**:
  - Long: close < schnelle SMA und schnelle SMA > langsame SMA.
  - Short: close > schnelle SMA und schnelle SMA < langsame SMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Kurs erreicht ATR-basierten Stop-Loss oder Take-Profit.
- **Stops**: ATR-Vielfache für Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `FastSmaLength` = 8
  - `SlowSmaLength` = 30
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 1.2
  - `AtrMultiplierTp` = 2.0
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, ATR
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
