# Omar MMR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum-basierte Methode, die RSI, drei exponentielle gleitende Durchschnitte und eine MACD-Kreuzung kombiniert. Long-Trades entstehen, wenn der Kurs über der langsamen EMA liegt, die schnelle EMA die mittlere EMA übersteigt, MACD bullisch kreuzt und RSI in einer neutralen Zone zwischen 29 und 70 liegt.

Take-Profit- und Stop-Loss-Prozentsätze werden über das Schutzmodul des Motors angewendet. Das Setup konzentriert sich auf die Ausrichtung von Momentum und Trend und vermeidet dabei überdehnte RSI-Werte.

## Details

- **Einstiegskriterien**:
  - **Long**: Schluss über EMA C, EMA A > EMA B, MACD-Linie kreuzt über das Signal, RSI zwischen 29 und 70.
- **Ausstiegskriterien**:
  - Verwaltung über Take-Profit oder Stop-Loss; kein expliziter Indikator-Ausstieg.
- **Indikatoren**:
  - RSI (Länge 14)
  - EMA A/B/C (Perioden 20/50/200)
  - MACD (12,26,9)
- **Stops**: Prozentbasierter Take-Profit 1,5% und Stop-Loss 2% standardmäßig.
- **Standardwerte**:
  - `RsiLength` = 14
  - `EmaALength` = 20
  - `EmaBLength` = 50
  - `EmaCLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 2.0
- **Filter**:
  - Trendfortsetzung
  - Einzelner Zeitrahmen
  - Indikatoren: RSI, EMA, MACD
  - Stops: Ja
  - Komplexität: Moderat
