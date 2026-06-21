# Backtest UT Bot + RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert einen UT Bot-Trenddetektor mit RSI-Levels. Geht long bei einer bullischen UT Bot-Umkehr, wenn der RSI überverkauft ist, und short bei einer bärischen Umkehr, wenn der RSI überkauft ist.

## Details

- **Einstiegskriterien**:
  - **Long**: UT Bot dreht nach oben und RSI < `RSI Oversold`.
  - **Short**: UT Bot dreht nach unten und RSI > `RSI Overbought`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Take-Profit- oder Stop-Loss-Prozentsätze.
- **Stops**: Take Profit & Stop Loss.
- **Standardwerte**:
  - `RSI Length` = 14
  - `RSI Overbought` = 60
  - `RSI Oversold` = 40
  - `ATR Length` = 10
  - `UT Bot Factor` = 1.0
  - `Take Profit %` = 3.0
  - `Stop Loss %` = 1.5
- **Filter**:
  - Kategorie: Trend Following
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
