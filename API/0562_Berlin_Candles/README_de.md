# Berlin Candles-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie mit benutzerdefinierten Berlin-Kerzen, die aus geglätteten Heikin-Ashi-Werten abgeleitet werden. Eine Long-Position wird eröffnet, wenn eine bullische Berlin-Kerze über der Donchian-Basislinie schließt. Eine Short-Position wird eröffnet, wenn eine bärische Berlin-Kerze unter der Basislinie schließt.

## Details

- **Einstiegskriterien**:
  - **Long**: Berlin-Schluss > Berlin-Eröffnung und Berlin-Schluss > Basislinie.
  - **Short**: Berlin-Schluss < Berlin-Eröffnung und Berlin-Schluss < Basislinie.
- **Long/Short**: Beide
- **Stops**: Standardmäßig keine
- **Standardwerte**:
  - `Smoothing` = 1
  - `BaselinePeriod` = 26
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
