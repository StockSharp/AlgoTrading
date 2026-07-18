# VWAP Mean Magnet v2 (Volumenfilter) Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert ein VWAP-Mean-Reversion-Konzept mit RSI und einem Volumenfilter. Trades werden eingegangen, wenn der Preis vom VWAP abweicht und der RSI extreme Niveaus erreicht, vorausgesetzt das aktuelle Volumen liegt über einem gleitenden Durchschnitt multipliziert mit einem Faktor.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis < VWAP, RSI < überverkauft, Volumenfilter bestanden.
  - **Short**: Preis > VWAP, RSI > überkauft, Volumenfilter bestanden.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Position schließen, wenn der Preis zum VWAP zurückkehrt.
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Volume lookback` = 20
  - `Volume multiplier` = 3
  - `Stop loss %` = 0.5
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
