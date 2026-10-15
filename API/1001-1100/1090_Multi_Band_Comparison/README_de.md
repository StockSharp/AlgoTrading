# Multi-Band-Vergleich
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Multi-Band-Vergleich verwendet SMA, Standardabweichung und Preis-Quantil-Bänder. Die Strategie geht long, wenn der Preis über dem oberen Quantil minus Standardabweichung für eine definierte Anzahl von Bars schließt, und steigt aus, wenn der Preis für eine festgelegte Anzahl von Bars unter dieses Niveau fällt.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Schluss über (oberes Quantil - Standardabweichung) für `EntryConfirmBars` Bars.
- **Ausstiegskriterien**: Schluss unter dieser Linie für `ExitConfirmBars` Bars.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 20
  - `BollingerMultiplier` = 1
  - `UpperQuantile` = 0.95
  - `EntryConfirmBars` = 1
  - `ExitConfirmBars` = 1
- **Filter**:
  - Kategorie: Statistisch
  - Richtung: Long
  - Indikatoren: SMA, Standard Deviation
  - Komplexität: Moderat
  - Risikolevel: Mittel
