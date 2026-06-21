# Charles Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsstrategie basierend auf täglichen Hoch- und Tiefniveaus. Sie sucht nach Preisbewegungen jenseits der Tagesspanne des Vortages mit einem RSI- und EMA-Trendfilter. Die Strategie berechnet das tägliche Hoch und Tief, verschiebt sie um ein konfigurierbares Delta und geht long oberhalb des oberen Niveaus oder short unterhalb des unteren Niveaus, wenn Trendbedingungen bestätigt werden.

## Details

- **Einstiegskriterien**:
  - Long: `Close > DailyHigh + Delta` und `RSI > 55` und `FastEMA > SlowEMA`
  - Short: `Close < DailyLow - Delta` und `RSI < 45` und `FastEMA < SlowEMA`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Schutz
- **Stops**: Konfigurierbarer Take-Profit und Stop-Loss in Prozent
- **Standardwerte**:
  - `Delta` = 0.0002m
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 1m
  - `StopLoss` = 0.5m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: EMA, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
