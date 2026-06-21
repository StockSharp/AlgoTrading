# Maximaler Gewinn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Max Gain vergleicht den prozentualen Abstand vom tiefsten Tief zum aktuellen Hoch und vom höchsten Hoch zum aktuellen Tief über einen Rückschauzeitraum. Es geht Long, wenn der potenzielle Gewinn den angepassten Verlust übersteigt, andernfalls geht es Short.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Max gain > adjusted max loss.
  - **Short**: Adjusted max loss > max gain.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `PeriodLength` = 30
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long & Short
  - Indikatoren: Highest, Lowest
  - Komplexität: Niedrig
  - Risikolevel: Mittel
