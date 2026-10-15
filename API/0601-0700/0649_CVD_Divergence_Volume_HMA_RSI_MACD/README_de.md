# CVD Divergenz Volumen HMA RSI MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert Hull Moving Averages, RSI, MACD, Volumenfilter und die Divergenz des kumulierten Volumen-Deltas (CVD), um Trendchancen zu identifizieren.

Long-Positionen werden eröffnet, wenn HMA20 über HMA50 liegt, RSI bullisches Momentum zeigt, das MACD-Histogramm steigt, das Volumen seinen Durchschnitt übersteigt und CVD eine bullische Divergenz bildet oder zunimmt. Short-Positionen spiegeln diese Bedingungen umgekehrt.

## Details
- **Einstiegskriterien**:
  - **Long**: HMA20 > HMA50 & Kurs > HMA20; RSI zwischen 40 und `RsiOverbought`; MACD-Linie über Signal & Histogramm steigend; Volumen > SMA * `VolumeMultiplier`; bullische CVD-Divergenz oder steigendes CVD.
  - **Short**: HMA20 < HMA50 & Kurs < HMA20; RSI zwischen `RsiOversold` und 60; MACD-Linie unter Signal & Histogramm fallend; Volumen > SMA * `VolumeMultiplier`; bärische CVD-Divergenz oder fallendes CVD.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Kurs < HMA20 oder RSI > `RsiOverbought` oder MACD-Linie kreuzt unter Signal.
  - **Short**: Kurs > HMA20 oder RSI < `RsiOversold` oder MACD-Linie kreuzt über Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Hma20Length` = 20
  - `Hma50Length` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: HMA, RSI, MACD, Volumen, CVD
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
