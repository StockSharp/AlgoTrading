# Liquiditäts-Engulfment-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt bullische und bärische Engulfing-Muster, die auftreten, wenn der Kurs kürzliche Liquiditätshochs oder -tiefs berührt. Trades werden nach Modus gefiltert und beinhalten einen festen Stop-Loss sowie einen optionalen Take-Profit in Pips.

## Details

- **Einstiegskriterien**:
  - **Long**: Bullisches Engulfing nach Berührung der unteren Liquidität.
  - **Short**: Bärisches Engulfing nach Berührung der oberen Liquidität.
- **Ausstiegskriterien**: Gegensignal, Stop-Loss oder Take-Profit.
- **Long/Short**: Konfigurierbar (standardmäßig beide).
- **Indikatoren**: Highest, Lowest.
- **Stops**: `StopLossPips` und optionales `TakeProfitPips`.
- **Standardwerte**:
  - `CandleType` = 1 Minute
  - `UpperLookback` = 10
  - `LowerLookback` = 10
  - `StopLossPips` = 10
  - `TakeProfitPips` = 20
  - `Mode` = Both
