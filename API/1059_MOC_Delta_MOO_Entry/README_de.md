# MOC Delta MOO Entry-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet das Kauf- und Verkaufsvolumen-Delta während der 14:50–14:55-Sitzung und handelt um 08:30, wenn der Delta-Prozentsatz einen Schwellenwert relativ zum Tagesvolumen überschreitet. Sie verwendet SMA-Filter auf den Eröffnungspreis und setzt tick-basierten Stop-Loss und Take-Profit ein.

## Details

- **Einstiegskriterien:**
  - **Long:** 08:30, MOC-Delta % über dem Schwellenwert, Eröffnung über SMA15 und SMA30.
  - **Short:** 08:30, MOC-Delta % unter dem negativen Schwellenwert, Eröffnung unter SMA15 und SMA30.
- **Ausstiegskriterien:**
  - **Stops:** Take-Profit und Stop-Loss in Ticks.
  - **Zeitbasiert:** Schließung aller Positionen um 14:50.
- **Standardwerte:**
  - `DeltaThreshold` = 2
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 10
