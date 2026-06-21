# Range-Filter-Strategie mit ATR TP/SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einsteigt, wenn der Preis die Range-Filter-Bänder kreuzt, und mit ATR-basierten Take-Profit- und Stop-Loss-Niveaus aussteigt.

## Details

- **Einstiegskriterien**: Preis kreuzt das obere Band für Long, das untere Band für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-basierter Take Profit oder Stop Loss.
- **Stops**: ATR-basiert, fest beim Öffnen des Trades.
- **Standardwerte**:
  - `RangeFilterLength` = 20
  - `RangeFilterMultiplier` = 1.5
  - `AtrLength` = 14
  - `TakeProfitMultiplier` = 1.5
  - `StopLossMultiplier` = 1.5
- **Filter**: keine.
- **Komplexität**: moderat.
- **Zeitrahmen**: konfigurierbar.
