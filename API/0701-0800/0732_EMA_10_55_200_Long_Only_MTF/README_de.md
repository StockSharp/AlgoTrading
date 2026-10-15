# EMA 10/55/200 Nur-Long-MTF-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Long-Positionen, wenn EMA-Kreuzungen im 4-Stunden-Chart mit bullischen Trends in den Tages- und Wochencharts übereinstimmen.

## Details

- **Einstiegskriterien**:
  - `EMA10` kreuzt `EMA55` nach oben, wobei das Kerzenhoch über `EMA55` liegt, oder `EMA55` kreuzt `EMA200` nach oben, oder `EMA10` kreuzt `EMA500` nach oben.
  - Der tägliche `EMA55` liegt über `EMA200` und der wöchentliche `EMA55` liegt über `EMA200`.
- **Ausstiegskriterien**:
  - `EMA10` kreuzt `EMA200` oder `EMA500` nach unten.
  - Der Preis fällt auf das Stop-Loss-Niveau.
- **Parameter**:
  - `EMA 10 Length` = 10
  - `EMA 55 Length` = 55
  - `EMA 200 Length` = 200
  - `EMA 500 Length` = 500
  - `Stop Loss %` = 5
