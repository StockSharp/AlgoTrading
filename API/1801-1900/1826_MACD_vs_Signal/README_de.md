# MACD vs Signal Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Kreuzung der MACD-Linie mit der Signallinie.

Einstieg Long, wenn die MACD-Linie die Signallinie von unten kreuzt.
Einstieg Short, wenn die MACD-Linie die Signallinie von oben kreuzt.
Optional werden Stop-Loss, Take-Profit und Trailing-Stop angewendet.

## Details

- **Einstiegskriterien**:
  - Long: `MACD kreuzt Signal von unten`
  - Short: `MACD kreuzt Signal von oben`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetzter MACD-Kreuzung
  - Risikomanagementregeln (Stop-Loss, Trailing-Stop, Take-Profit)
- **Stops**: Stop-Loss, Take-Profit, Trailing-Stop (optional)
- **Standardwerte**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLoss` = 50 Punkte
  - `TakeProfit` = 999 Punkte
  - `TrailingStop` = 0 Punkte (deaktiviert)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Stop-Loss / Take-Profit / Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
