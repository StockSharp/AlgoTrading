# Markttrend-Level-Strategie ohne Neuzeichnung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Crossover-Strategie, die optional Trades mit RSI filtert. Long-Positionen werden eröffnet, wenn der schnelle EMA über den langsamen EMA kreuzt, während Short-Trades beim entgegengesetzten Crossover ausgelöst werden. Wenn `ApplyExitFilters` aktiviert ist und der RSI-Filter aktiv ist, werden Positionen geschlossen, wenn der RSI die erlaubte Zone verlässt.

## Details

- **Einstiegskriterien**:
  - **Long**: `Fast EMA` kreuzt über `Slow EMA` und `RSI > RsiLongThreshold` wenn aktiviert
  - **Short**: `Fast EMA` kreuzt unter `Slow EMA` und `RSI < RsiShortThreshold` wenn aktiviert
- **Ausstiegskriterien**: Entgegengesetzter Crossover oder RSI-Filter versagt wenn `ApplyExitFilters` wahr ist
- **Typ**: Trendfolge
- **Indikatoren**: EMA, RSI
- **Zeitrahmen**: 5 Minuten (Standard)
