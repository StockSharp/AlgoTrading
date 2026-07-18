# 3Commas Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vereinfachte Version der 3Commas Bot-Strategie. Es wird gehandelt, wenn ein schnelles EMA ein langsameres EMA kreuzt, und das Risiko wird mithilfe eines ATR-basierten Stops gesteuert. Ein festes Ertragsziel und ein optionaler ATR-Trailing-Stop werden unterstützt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelles EMA kreuzt über langsames EMA.
  - **Short**: Schnelles EMA kreuzt unter langsames EMA.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - ATR-Stop, optionaler Take-Profit, optionaler ATR-Trailing-Stop sobald ein Ertragsschwellenwert erreicht ist.
- **Stops**: ATR-basiert.
- **Standardwerte**:
  - `MaLength1` = 21
  - `MaLength2` = 50
  - `AtrLength` = 14
  - `RnR` = 1
  - `RiskM` = 1
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, ATR
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
