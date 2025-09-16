# CSV Margin Tracker Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy records portfolio balance, equity, and margin into a CSV file on a fixed time interval.
Optional alerts can be logged when the margin-to-equity ratio exceeds configurable levels.

## Details
- **Purpose**: monitor account risk and store minimum balance, minimum equity, and maximum margin per interval.
- **Data Output**: writes to `margintracker_<portfolio>.csv`.
- **Alerts**: two margin levels trigger log messages after a cooldown.

## Parameters
- `IntervalSeconds` – length of the aggregation interval.
- `MailAlert` – enable or disable margin level alerts.
- `MailAlertIntervalSeconds` – minimum delay between alerts.
- `MailAlertMarginLevel1` – first margin-to-equity threshold.
- `MailAlertMarginLevel2` – second margin-to-equity threshold.
