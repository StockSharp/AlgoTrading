# Liquidex Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Liquidex Keltner**-Strategie handelt Ausbrüche aus Keltner-Kanälen mit einem gleitenden Durchschnitt als Trendfilter.
Trades sind nur während bestimmter Stunden erlaubt und können optional durch die RSI-Richtung bestätigt werden.
Stop-Loss und Take-Profit werden über feste Prozentsätze verwaltet.

## Details
- **Einstiegskriterien**:
  - Preis kreuzt das obere Keltner-Band nach oben und schließt über dem gleitenden Durchschnitt.
  - Preis kreuzt das untere Keltner-Band nach unten und schließt unter dem gleitenden Durchschnitt.
  - Der Kerzenkörper muss `RangeFilter` überschreiten.
  - Wenn `UseRsiFilter` aktiviert ist, muss der RSI für Longs über 50 und für Shorts unter 50 liegen.
  - Die aktuelle Uhrzeit muss zwischen `EntryHourFrom` und `EntryHourTo` liegen und freitags vor `FridayEndHour`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja, prozentbasiert über `StartProtection`.
- **Standardwerte**:
  - `MaPeriod = 7`
  - `RangeFilter = 10m`
  - `StopLoss = 1m`
  - `TakeProfit = 2m`
  - `UseKeltnerFilter = true`
  - `KeltnerPeriod = 6`
  - `KeltnerMultiplier = 1m`
  - `UseRsiFilter = false`
  - `RsiPeriod = 14`
  - `EntryHourFrom = 2`
  - `EntryHourTo = 24`
  - `FridayEndHour = 22`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: MA, Keltner, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
