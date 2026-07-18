# E TurboFx-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum-Umkehr-Strategie, adaptiert vom MQL5-Experten "e-TurboFx". Das System beobachtet eine Reihe von Kerzen, deren Körper in dieselbe Richtung wachsen. Nach mehreren bärischen Kerzen mit sich ausdehnenden Körpern kauft die Strategie und erwartet eine Gegenbewegung. Nach mehreren bullischen Kerzen mit wachsenden Körpern verkauft sie. Optionaler Stop-Loss und Take-Profit werden in rohen Preispunkten gesetzt.

## Details

- **Einstiegskriterien**:
  - Long: `N` aufeinanderfolgende bärische Kerzen und jeder Körper größer als der vorherige
  - Short: `N` aufeinanderfolgende bullische Kerzen und jeder Körper größer als der vorherige
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Punkte über `StartProtection`
- **Standardwerte**:
  - `BarsCount` = 3
  - `StopLossPoints` = 700
  - `TakeProfitPoints` = 1200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Price Action
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
