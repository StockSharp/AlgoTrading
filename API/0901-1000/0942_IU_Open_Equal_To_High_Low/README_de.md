# IU Eröffnung gleich Hoch/Tief-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eröffnet eine Long-Position bei der ersten Kerze des Tages, wenn deren Eröffnungspreis dem Tief entspricht, und eine Short-Position, wenn die Eröffnung dem Hoch entspricht. Der Stop-Loss verwendet die vorherige Kerze und der Take Profit basiert auf dem `RiskReward`-Verhältnis.

## Details

- **Einstiegskriterien**:
  - **Long**: Eröffnung der ersten Kerze entspricht ihrem Tief.
  - **Short**: Eröffnung der ersten Kerze entspricht ihrem Hoch.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Stop-Loss am vorherigen Kerzentief für Long, vorherigen Kerzenhoch für Short.
  - Take Profit berechnet vom Einstiegspreis mit `RiskReward`.
- **Stops**: Ja.
- **Standardwerte**:
  - `RiskReward` = 2.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Preisverhalten
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
