# Bbsr Extreme
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Bbsr Extreme**-Strategie kombiniert Bollinger-Bands-Ausbrüche mit einem Trendfilter auf Basis eines gleitenden Durchschnitts.
Eine Long-Position entsteht, wenn der Kurs von der unteren Band abprallt und der Durchschnitt steigt.
Eine Short-Position wird bei einem Rückzug von der oberen Band eröffnet, wenn der Durchschnitt fällt.
Der Ausstieg basiert auf ATR-basiertem Stop-Loss und Take-Profit.

## Details
- **Einstiegskriterien**: Kurs kreuzt die Bänder mit Trendbestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-Stop oder Take-Profit.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `BollingerPeriod = 20`
  - `BollingerMultiplier = 2`
  - `MaLength = 7`
  - `AtrLength = 14`
  - `AtrStopMultiplier = 2`
  - `AtrProfitMultiplier = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, EMA, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
