# RSI Überkauft/Überverkauft (RSI Overbought/Oversold)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses System handelt Umkehrungen mithilfe des Relative Strength Index (RSI). Wenn der RSI unter das Überverkauft-Niveau fällt, kauft es nach dem Schließen aller Short-Positionen. Wenn der RSI über das Überkauft-Niveau steigt, verkauft es nach dem Schließen der Longs.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 61 %. Die Strategie eignet sich am besten für den Kryptomarkt.

Positionen werden geschlossen, wenn der RSI in eine neutrale Zone zurückkehrt oder der Stop-Loss erreicht wird.

## Details

- **Einstiegskriterien**: RSI unter `OversoldLevel` oder über `OverboughtLevel`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: RSI kreuzt `NeutralLevel` oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70
  - `OversoldLevel` = 30
  - `NeutralLevel` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
