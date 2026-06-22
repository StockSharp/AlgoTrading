# ASCtrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Williams %R-Indikator, um schnelle Umkehrungen ähnlich dem ASCtrend-Ansatz zu erkennen. Sie verkauft, wenn der Indikator von einem überverkauften Niveau auf ein überkauftes Niveau steigt, und kauft, wenn das Gegenteil eintritt.

## Details

- **Einstiegskriterien**:
  - Verkaufen, wenn Williams %R von überverkauft (unter `x2`) zu überkauft (über `x1`) kreuzt.
  - Kaufen, wenn Williams %R von überkauft (über `x1`) zu überverkauft (unter `x2`) kreuzt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Das umgekehrte Signal schließt die Position und dreht sie um.
- **Stops**: Nein.
- **Standardwerte**:
  - `Risk` = 4
  - `CandleType` = 1 Stunde
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Williams %R
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
