# ICT Bread and Butter Sell-Setup Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verfolgt die Hochs und Tiefs der London-, New York- und Asien-Sessions und handelt vordefinierte Setups rund um diese.

## Details

- **Einstiegskriterien**:
  - **NY Short**: Der Preis erreicht ein höheres Hoch als die London-Session, und die Kerze schließt bärisch während der NY-Session.
  - **London Close Kauf**: Zwischen 10:30 und 13:00 Uhr, wenn der Preis unter dem Tief der London-Session schließt.
  - **Asia Short**: Während der Asien-Session, wenn der Preis über dem Hoch der Asien-Session schließt.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Jeder Trade verwendet Stop-Loss und Take-Profit in Ticks.
- **Stops**: Ja.
- **Standardwerte**:
  - `ShortStopTicks` = 10
  - `ShortTakeTicks` = 20
  - `BuyStopTicks` = 10
  - `BuyTakeTicks` = 20
  - `AsiaStopTicks` = 10
  - `AsiaTakeTicks` = 15
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Price action
  - Richtung: Beide
  - Indikatoren: Price action
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
