# Nasdaq 100 Haupthandelszeiten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt den Nasdaq 100 nur in den ersten zwei und der letzten Stunde der Handelssitzung. Sie nutzt EMA-Trendbestätigung, RSI-, ATR- und VWAP-Filter mit ATR-basierten Trailing-Stops und Break-Even-Stops.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs über kurzer EMA, kurze EMA über langer EMA, beide EMAs steigend, RSI über 50 und Kurs über VWAP während der Haupthandelszeiten.
  - **Short**: Entgegengesetzte Bedingungen.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - ATR-basierter Trailing-Stop oder Break-Even-Stop.
  - Zeitbasierter Ausstieg nach konfigurierbarer Anzahl von Bars oder EMA-Trendumkehr.
- **Stops**: ATR-Trailing mit Break-Even.
- **Standardwerte**:
  - `Long EMA` = 21
  - `Short EMA` = 9
  - `RSI` = 14
  - `ATR` = 14
  - `Trail ATR Mult` = 1.5
  - `Initial SL Mult` = 0.5
  - `Break-even ATR Mult` = 1.5
  - `Time Exit Bars` = 20
- **Filter**:
  - Kategorie: Intraday
  - Richtung: Beide
  - Indikatoren: EMA, RSI, ATR, VWAP
  - Stops: Trailing
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
