# Verbesserte BarUpDn-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie sucht nach bullischen oder bearischen Bars in Kombination mit Bollinger Bands und Trendbestätigung. Sie geht bei bullischen Gaps in Aufwärtstrends Long und bei bearischen Gaps in Abwärtstrends Short. Ausstiege nutzen ATR-basierte Stop-Loss- und Take-Profit-Level.

## Details

- **Einstiegskriterien**:
  - Long: Bullische Kerze mit Gap nach oben, Schlusskurs über Trend-MA und über der unteren Bollinger Band.
  - Short: Bearische Kerze mit Gap nach unten, Schlusskurs unter Trend-MA und unter der oberen Bollinger Band.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Preis berührt ATR-basierten Stop-Loss oder Take-Profit (1,5× ATR).
- **Stops**: ATR-basierter Stop und Take-Profit.
- **Standardwerte**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `MaLength` = 50
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 2
  - `AtrMultiplierTp` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, SMA, ATR
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
