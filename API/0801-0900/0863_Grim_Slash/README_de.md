# Strategie Grim Slash
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grim Slash ist eine einfache Price-Action-Strategie, die kauft, wenn das Tief der aktuellen Kerze den vorherigen Schlusskurs testet, und aussteigt, wenn das Hoch den vorherigen Höchstkurs erreicht. Das Risiko wird mit festem Prozentsatz für Take-Profit und Stop-Loss gesteuert.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Das aktuelle Tief berührt oder fällt unter den vorherigen Schlusskurs.
- **Ausstiegskriterien**: Das aktuelle Hoch berührt oder überschreitet das vorherige Hoch.
- **Stops**: 15% Take-Profit, 5% Stop-Loss.
- **Standardwerte**:
  - `TakeProfitPercent` = 15
  - `StopLossPercent` = 5
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Komplexität: Niedrig
  - Risikolevel: Mittel
