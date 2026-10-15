# Triple CCI MFI Bestätigte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht long, wenn der schnelle CCI über null kreuzt, während der mittlere und langsame CCI positiv bleiben, der Preis über dem EMA liegt und der MFI 50 überschreitet. Der Gewinn wird nach einer ATR-basierten Aktivierung durch den EMA verfolgt.

Tests zeigen moderate Performance; am besten in Trendmärkten.

## Details
- **Einstiegskriterien**:
  - **Long**: Schneller CCI kreuzt über 0, mittlerer CCI > 0, langsamer CCI > 0, MFI > 50, Schlusskurs über EMA
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - **Long**: Schlusskurs unter Trailing-EMA nach Aktivierung oder Tief erreicht ATR-Stop
- **Stops**: Ja.
- **Standardwerte**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 14
  - `MiddleCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `MfiLength` = 14
  - `EmaLength` = 50
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: CCI, MFI, EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
