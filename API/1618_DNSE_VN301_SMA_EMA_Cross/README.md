# DNSE VN301 SMA & EMA Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades the VN301 index using a crossover between a 15-period EMA and a 60-period SMA. It exits before the trading session ends and applies a simple percentage stop to cap losses.

Testing indicates an average annual return of about 20%. It works best on VN30 futures.

A long position is opened when EMA15 crosses above SMA60 and price is above the EMA. A short position opens on the opposite cross. Positions are closed on reverse signals, session cutoff, or when price moves against the entry beyond the configured loss limit.

## Details

- **Entry Criteria**:
  - **Long**: EMA15 crosses above SMA60 and price >= EMA15 before cutoff.
  - **Short**: EMA15 crosses below SMA60 and price <= EMA15 before cutoff.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover, max loss or session cutoff.
- **Stops**: Yes, percentage-based max loss.
- **Filters**:
  - Session cutoff time.
