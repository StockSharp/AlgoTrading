# ETH/USDT EMA-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt ETH/USDT unter Verwendung eines EMA-Crossovers mit zusätzlichen Filtern.

Eine Long-Position wird eröffnet, wenn der 20-Perioden-EMA den 50-Perioden-EMA nach oben kreuzt, während der Preis über dem 200-Perioden-EMA liegt, der RSI über 30 ist, die durch ATR gemessene Volatilität über ihrem gleitenden Durchschnitt liegt und das Volumen größer als sein Durchschnitt ist. Eine Short-Position wird bei entgegengesetzten Bedingungen eröffnet.

Positionen kehren sich um, wenn das entgegengesetzte Signal erscheint. Es wird kein expliziter Stop Loss oder Take Profit verwendet.

## Details

- **Einstiegskriterien**:
  - **Long**: `EMA20 kreuzt EMA50 nach oben` && `Close > EMA200` && `RSI > 30` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
  - **Short**: `EMA20 kreuzt EMA50 nach unten` && `Close < EMA200` && `RSI < 70` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
- **Long/Short**: Beide Seiten
- **Ausstiegskriterien**:
  - Umgekehrtes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `EMA200 Length` = 200
  - `EMA20 Length` = 20
  - `EMA50 Length` = 50
  - `RSI Length` = 14
  - `ATR Length` = 14

- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, RSI, ATR
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
