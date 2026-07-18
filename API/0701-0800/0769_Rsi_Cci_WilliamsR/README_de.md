# Strategie RSI CCI Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert RSI, CCI und Williams %R, um Umkehrmöglichkeiten zu erfassen. Sie kauft, wenn alle drei Indikatoren überverkaufte Niveaus erreichen, und verkauft, wenn alle überkaufte Niveaus erreichen. Jeder Trade verwendet prozentbasierte Take Profit- und Stop Loss-Absicherung.

## Details

- **Einstiegsbedingungen**:
  - **Long**: `RSI < RSI überverkauft` && `CCI < CCI überverkauft` && `Williams %R < Williams überverkauft`
  - **Short**: `RSI > RSI überkauft` && `CCI > CCI überkauft` && `Williams %R > Williams überkauft`
- **Ausstiegsbedingungen**: Positionen werden über Take Profit oder Stop Loss geschlossen.
- **Typ**: Umkehr
- **Indikatoren**: RSI, CCI, Williams %R
- **Zeitrahmen**: 45 Minuten (Standard)
- **Stops**: Prozentbasierter Take Profit und Stop Loss
