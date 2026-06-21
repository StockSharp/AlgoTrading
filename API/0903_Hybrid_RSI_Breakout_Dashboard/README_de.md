# Hybrid RSI Breakout Dashboard
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert RSI Mean Reversion mit Ausbruchs-Einstiegen, gefiltert durch ADX und eine 200 EMA.

Das System kauft, wenn der Markt seitwärts läuft und RSI unter `RsiBuy` fällt, im bullischen EMA-Trend. Es verkauft leer, wenn RSI über `RsiSell` steigt im bärischen Trend. Im Trendregime tritt es bei Ausbrüchen über/unter jüngsten Schlusskursen ein und verfolgt die Position mit ATR.

Enthält einen Startdatumsfilter und einfache Dashboard-Variablen für letzten Handelstyp und Richtung.

## Details

- **Einstiegskriterien**: RSI-Signale im Seitwärtsregime mit EMA-Tendenz oder Ausbrüche über/unter den letzten `BreakoutLength` Schlusskursen, wenn ADX > `AdxThreshold`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: RSI-Trades beenden bei `RsiExit`. Ausbruchs-Trades verwenden ATR Trailing Stop.
- **Stops**: ATR Trailing Stop für Ausbruchs-Trades.
- **Standardwerte**:
  - `AdxLength` = 14
  - `AdxThreshold` = 20m
  - `EmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuy` = 40m
  - `RsiSell` = 60m
  - `RsiExit` = 50m
  - `BreakoutLength` = 20
  - `AtrLength` = 14
  - `AtrMultiplier` = 2m
  - `StartDate` = 2017-01-01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend, Mean Reversion
  - Richtung: Beide
  - Indikatoren: ADX, EMA, RSI, ATR, Highest/Lowest
  - Stops: Trailing
  - Komplexität: Moderat
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
