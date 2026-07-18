# Trendtyp-Indikator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend Type Indicator erkennt das Marktregime mithilfe von ATR und ADX.
Er geht Long in Aufwärtstrends, Short in Abwärtstrends und steigt aus, wenn die Bedingungen seitwärts drehen.

## Details

- **Einstiegskriterien**: +DI größer als -DI und kein Seitwärtsmarkt
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetzter Trend oder Seitwärtsmarkt
- **Stops**: Nein
- **Standardwerte**:
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMaLength` = 20
  - `UseAdx` = true
  - `AdxLength` = 14
  - `AdxLimit` = 25
  - `SmoothFactor` = 3
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, ADX
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
