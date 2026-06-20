# Bollinger Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf Bollinger Bands Mean Reversion

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 118%. Sie funktioniert am besten am Aktienmarkt.

Bollinger Reversion handelt gegen Bewegungen außerhalb der Bollinger Bands. Trades öffnen gegen Schlusskurse jenseits der Bänder und schließen, sobald der Preis wieder ins Innere zurückkehrt oder einen Stop trifft.

Standardabweichungsbänder bieten eine statistische Sicht auf Überextensionen. Der Einstieg nach extremen Schlusskursen zielt darauf ab, vom Rückprall in Richtung des mittleren Bands zu profitieren.


## Details

- **Einstiegskriterien**: Signale basierend auf RSI, ATR, Bollinger.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI, ATR, Bollinger
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

