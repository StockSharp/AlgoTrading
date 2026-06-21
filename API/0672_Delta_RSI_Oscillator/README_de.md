# Delta-RSI-Oszillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Delta-RSI-Oszillator, definiert als die mit einer EMA geglättete Änderung des RSI. Signale werden ausgelöst, wenn das Delta die Null kreuzt, seine Signallinie kreuzt oder die Richtung ändert. Ausstiege spiegeln die gewählte Bedingung wider.

## Details

- **Einstiegskriterien**: Basierend auf `BuyCondition` (Nulldurchgang, Signalliniendurchgang oder Richtungsänderung) am Delta-RSI.
- **Long/Short**: Beide, gesteuert durch `UseLong` und `UseShort`.
- **Ausstiegskriterien**: Basierend auf `ExitCondition` am Delta-RSI.
- **Stops**: Keine.
- **Standardwerte**:
  - `RsiLength` = 21
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI, EMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
