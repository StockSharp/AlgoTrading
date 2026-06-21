# Entry-Fragger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verfolgt Sequenzen roter und grüner Kerzen relativ zum 50-Perioden-EMA. Nach einer Reihe roter Kerzen unterhalb des EMA löst eine grüne Kerze, die oberhalb einer Volatilitätswolke schließt, einen Long-Einstieg aus. Ein ähnliches Setup mit grünen Kerzen geht Short-Einstiegen voraus. Optionaler Umkehrhandel ermöglicht das Wenden von Positionen.

## Details

- **Einstiegskriterien**:
  - **Long**: `redCount >= Buy Signal Accuracy` && letzte rote unter EMA50 && grüne Kerze schließt über `EMA50 + stdev/4`.
  - **Short**: `greenCount >= Sell Signal Accuracy` && vorherige Kerze grün && rote Kerze schließt über `EMA50 + stdev/4`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Indikatoren**: EMA, StandardDeviation.
- **Standardwerte**:
  - `Buy Signal Accuracy` = 2
  - `Sell Signal Accuracy` = 2
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
