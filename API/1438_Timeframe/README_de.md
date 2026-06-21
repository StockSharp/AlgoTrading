# Zeitrahmen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Kreuzungsstrategie mit zeitrahmenbewusstem Risikomanagement.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 31%. Sie funktioniert am besten auf dem Kryptomarkt.

Die Strategie kauft, wenn eine schnelle EMA eine langsamere EMA von unten kreuzt und der langfristige Trend bullisch ist. Short-Einstiege erfolgen beim umgekehrten Kreuz. Handelszeiten und ein einfacher ADX-Filter helfen, Perioden mit geringem Momentum zu vermeiden. Das Risiko wird mit prozentualen Take-Profit- und Stop-Loss-Levels verwaltet.

## Details

- **Einstiegskriterien**:
  - **Long**: EMA9 kreuzt EMA20 von unten, während EMA50 über EMA200 liegt.
  - **Short**: EMA9 kreuzt EMA20 von oben, während EMA50 unter EMA200 liegt.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Stop-Loss oder Take-Profit.
  - **Short**: Stop-Loss oder Take-Profit.
- **Stops**: Ja, optionaler Trailing-Stop.
- **Standardwerte**:
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1.0
  - `TrailingPercent` = 0.5
  - `StartHour` = 15
  - `EndHour` = 20
  - `CooldownBars` = 5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, RSI, ADX
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
