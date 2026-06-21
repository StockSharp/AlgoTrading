# MACD-Muster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den klassischen MetaTrader MACD Sample Expert.
Sie verwendet einen MACD-Kreuzung kombiniert mit einem EMA-Trendfilter, separate Take-Profit- und Stop-Loss-Niveaus für Long- und Short-Trades sowie einen optionalen Trailing Stop. Der Handel ist nur innerhalb eines konfigurierbaren Zeitfensters erlaubt.

## Details

- **Einstiegskriterien**:
  - **Long**: Die MACD-Linie liegt unter null und kreuzt die Signallinie nach oben, während die EMA steigt.
  - **Short**: Die MACD-Linie liegt über null und kreuzt die Signallinie nach unten, während die EMA fällt.
- **Ausstiegskriterien**:
  - Umgekehrte MACD-Kreuzung.
  - Erreichen der individuellen Take-Profit- oder Stop-Loss-Ziele.
  - Trailing Stop ausgelöst.
- **Long/Short**: Beide.
- **Standardwerte**:
  - `EMA Period` = 26
  - `MACD Open Level` = 3
  - `MACD Close Level` = 2
  - `Take Profit Long` = 50
  - `Take Profit Short` = 75
  - `Stop Loss Long` = 80
  - `Stop Loss Short` = 50
  - `Trailing Stop` = 30
  - Handelszeiten: 4 bis 19 UTC
- **Indikatoren**: MACD, EMA
- **Zeitrahmen**: Standardmäßig 1-Stunden-Kerzen
