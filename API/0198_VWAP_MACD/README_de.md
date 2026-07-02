# Strategie Vwap Macd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf VWAP und MACD. Einstieg Long, wenn der Preis über dem VWAP liegt und MACD > Signal. Einstieg Short, wenn der Preis unter dem VWAP liegt und MACD < Signal. Ausstieg, wenn MACD seine Signallinie in die entgegengesetzte Richtung kreuzt.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 181%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

VWAP leitet den Intraday-Wert, und MACD-Kreuzungen zeigen Momentum-Wechsel an. Trades werden gestartet, wenn MACD in der Nähe des VWAP-Levels dreht.

Geeignet für kurzfristige Momentum-Trader. ATR-Stop-Regeln verhindern übermäßiges Risiko.

## Details

- **Einstiegskriterien**:
  - Long: `Close > VWAP && MACD > Signal`
  - Short: `Close < VWAP && MACD < Signal`
- **Long/Short**: Beide
- **Ausstiegskriterien**: MACD-Kreuzung in entgegengesetzter Richtung
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: VWAP, MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

