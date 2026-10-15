# Donchian WMA-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Überschreiten des Donchian-Tiefs über einen gewichteten gleitenden Durchschnitt löst Long-Einstiege nur im Kalenderjahr 2025 aus. Positionen werden geschlossen, wenn ein Take-Profit-Level erreicht wird, der Crossover sich bei fallendem WMA umkehrt oder das Datum außerhalb von 2025 liegt.

## Details

- **Einstiegskriterien**:
  - Long: `DonchianLow` kreuzt `WMA` nach oben und das Datum liegt in 2025
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - Take-Profit über `TakeProfitPercent`
  - Kreuzung von `DonchianLow` unter `WMA` bei fallendem `WMA`
  - Datum außerhalb 2025
- **Stops**: Nur Take-Profit
- **Standardwerte**:
  - `DonchianLength` = 7
  - `WmaLength` = 62
  - `TakeProfitPercent` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long
  - Indikatoren: Donchian-Kanal, Gewichteter Gleitender Durchschnitt
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nur Jahr 2025
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
