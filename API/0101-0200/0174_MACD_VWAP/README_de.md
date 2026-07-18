# Strategie Macd Vwap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf den Indikatoren MACD und VWAP. Geht long, wenn MACD > Signal und Preis > VWAP. Geht short, wenn MACD < Signal und Preis < VWAP.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 109%. Sie funktioniert am besten auf dem Kryptomarkt.

Der MACD-Momentum wird relativ zur VWAP-Linie gemessen. Long-Trades suchen nach MACD-Stärke unterhalb des VWAP, während Shorts oberhalb davon entstehen.

Ideal für Intraday-Momentum-Trader, die volumengewichtete Referenzen verwenden. ATR-basierte Stops steuern das Risiko.

## Details

- **Einstiegskriterien**:
  - Long: `MACD > Signal && Close > VWAP`
  - Short: `MACD < Signal && Close < VWAP`
- **Long/Short**: Beide
- **Ausstiegskriterien**: MACD-Kreuzung in die entgegengesetzte Richtung
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MACD, VWAP
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

