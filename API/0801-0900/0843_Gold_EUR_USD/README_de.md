# Gold & EUR/USD Liquiditätsgrab-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt Liquiditätsgrabs an Angebots- und Nachfragezonen bei Gold und EUR/USD unter Verwendung von RSI, SMA, Stochastischem Oszillator und ATR-basierten Fair-Value-Gaps.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs wickelt unter das letzte Tief, Marktstruktur dreht nach oben, Fair-Value-Gap entsteht, RSI überverkauft, Kurs über SMA, Stochastik überverkauft.
  - **Short**: Kurs wickelt über das letzte Hoch, Marktstruktur dreht nach unten, Fair-Value-Gap entsteht, RSI überkauft, Kurs unter SMA, Stochastik überkauft.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `RsiLength` = 14
  - `MaLength` = 50
  - `StochLength` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `StochOverbought` = 80
  - `StochOversold` = 20
- **Filter**:
  - Kategorie: Price action
  - Richtung: Beide
  - Indikatoren: RSI, SMA, Stochastic, ATR, Highest, Lowest
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
