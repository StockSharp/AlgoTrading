# Long- und Short-Strategie mit mehreren Indikatoren
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet RSI, Rate of Change und einen wählbaren gleitenden Durchschnitt, um Long- und Short-Signale zu generieren. Für Ausstiege wird ein ATR-basierter Trailing-Stop eingesetzt.

## Details

- **Einstiegskriterien**:
  - Long: RSI zwischen überkauft und überverkauft, ROC > 0 und Kurs über der MA.
  - Short: Bestätigter Bärischer Trend, ROC < 0 und Kurs unter der MA.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - ATR-basierter Trailing-Stop oder indikatorbasierte Stop-Bedingungen.
- **Stops**: ATR-Trailing-Stop.
- **Standardwerte**:
  - `RsiLength` = 5
  - `RsiOverbought` = 70
  - `RsiOversold` = 44
  - `RocLength` = 4
  - `MaLength` = 24
  - `MaTypeParam` = TEMA
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `BearishMaLength` = 200
  - `BearishTrendDuration` = 5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: RSI, ROC, MA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
