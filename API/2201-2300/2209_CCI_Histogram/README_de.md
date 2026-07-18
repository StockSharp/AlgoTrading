# CCI-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Commodity Channel Index (CCI), um Umkehrungen zu erkennen, wenn der Indikator extreme Zonen verlässt. Eine Long-Position wird eröffnet, wenn der CCI nach einem Aufenthalt über dem oberen Niveau wieder darunter fällt. Eine Short-Position wird eröffnet, wenn der CCI nach einem Aufenthalt unter dem unteren Niveau wieder darüber steigt. Optionale Stop-Loss- und Take-Profit-Niveaus in Punkten können offene Positionen schützen.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorheriger CCI > `UpperLevel` und aktueller CCI ≤ `UpperLevel`.
  - **Short**: Vorheriger CCI < `LowerLevel` und aktueller CCI ≥ `LowerLevel`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Das entgegengesetzte Signal schließt die bestehende Position und eröffnet eine neue.
- **Stops**: Optionaler fester Stop-Loss und Take-Profit in Punkten.
- **Standardwerte**:
  - `CCI Period` = 14
  - `Upper Level` = 100
  - `Lower Level` = -100
  - `Stop Loss` = 100 Punkte
  - `Take Profit` = 200 Punkte
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: CCI
  - Stops: Optional
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig (Standard 4H)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

