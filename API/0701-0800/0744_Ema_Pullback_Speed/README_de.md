# EMA-Pullback-Geschwindigkeits-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die EMA-Pullback-Geschwindigkeits-Strategie verwendet eine dynamische EMA, die sich der Preisbeschleunigung anpasst. Eine Long-Position wird eröffnet, wenn der Kurs während eines Aufwärtstrends mit einer bullischen Umkehr und ausreichender Aufwärtsgeschwindigkeit zur dynamischen EMA zurückkehrt. Eine Short-Position wird unter umgekehrten Bedingungen eröffnet. Ausstiege erfolgen über einen ATR-basierten Stop-Loss und einen fixen prozentualen Take-Profit.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs über dynamischer EMA, bullische Umkehr, Kurs zur EMA zurückgekehrt, positive Geschwindigkeit, kurze EMA über langer EMA, Geschwindigkeit ≥ `LongSpeedMin`.
  - **Short**: Kurs unter dynamischer EMA, bärische Umkehr, Kurs zur EMA zurückgekehrt, negative Geschwindigkeit, kurze EMA unter langer EMA, Geschwindigkeit ≤ `ShortSpeedMax`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: ATR-Stop-Loss und fixer prozentualer Take-Profit.
- **Stops**: Stop-Loss `AtrMultiplier`×ATR, Take-Profit `FixedTpPct`%.
- **Standardwerte**:
  - `MaxLength` = 50
  - `AccelMultiplier` = 3
  - `ReturnThreshold` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 4
  - `FixedTpPct` = 1.5
  - `ShortEmaLength` = 21
  - `LongEmaLength` = 50
  - `LongSpeedMin` = 1000
  - `ShortSpeedMax` = -1000
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, ATR
  - Stops: ATR-Stop-Loss, fixer Take-Profit
  - Komplexität: Mittel
  - Zeitrahmen: 5m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
