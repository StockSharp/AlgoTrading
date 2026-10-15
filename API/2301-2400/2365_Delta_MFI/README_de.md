# Delta MFI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Vergleich von schnellen und langsamen Money Flow Index (MFI) Werten. Sie geht long, wenn der schnelle MFI über den langsamen MFI steigt, während der langsame MFI über dem Signalniveau liegt. Sie geht short, wenn der schnelle MFI unter den langsamen MFI fällt, während der langsame MFI unter 100 minus dem Signalniveau liegt.

## Details

- **Einstiegskriterien**: 
  - Kaufen wenn `slow MFI > Level` und `fast MFI > slow MFI`
  - Verkaufen wenn `slow MFI < 100 - Level` und `fast MFI < slow MFI`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenteiliges Signal
- **Stops**: Nein
- **Standardwerte**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 50
  - `Level` = 50
  - `CandleType` = 4-Stunden-Kerzen
- **Filter**:
  - Kategorie: Indikator
  - Richtung: Beide
  - Indikatoren: Money Flow Index
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
