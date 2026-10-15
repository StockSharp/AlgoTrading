# VWAP Mean Magnet v9 (Einfacher Alert) Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese vereinfachte Version der VWAP Mean Magnet Strategie verwendet VWAP und RSI ohne Volumenfilter. Trades öffnen sich, wenn der Preis vom VWAP abweicht und der RSI extreme Niveaus erreicht. Positionen werden geschlossen, wenn der Preis zum VWAP zurückkehrt.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis < VWAP und RSI < überverkauft.
  - **Short**: Preis > VWAP und RSI > überkauft.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Position schließen, wenn der Preis zum VWAP zurückkehrt.
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Stop loss %` = 0.5
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Intraday
