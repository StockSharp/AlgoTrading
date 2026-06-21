# Hull Suite Ohne SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hull Suite Ohne SL/TP ist eine Trendfolge-Strategie basierend auf Hull Moving Average Variationen. Die Position wird umgekehrt, wenn sich die Hull-Linie im Vergleich zu zwei Kerzen zuvor ändert.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Hull-Wert ist größer als vor zwei Kerzen.
  - **Short**: Hull-Wert ist kleiner als vor zwei Kerzen.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 55
  - `Mode` = `Hma`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: Hull Moving Average
  - Komplexität: Niedrig
  - Risikolevel: Niedrig
