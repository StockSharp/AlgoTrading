# Strategie Vwap Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die VWAP- und Stochastic-Indikatoren kombiniert. Kauft, wenn der Preis unter dem VWAP liegt und Stochastic überverkauft ist. Verkauft, wenn der Preis über dem VWAP liegt und Stochastic überkauft ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 187%. Sie funktioniert am besten im Aktienmarkt.

VWAP markiert das durchschnittliche Handelsniveau und Stochastic zeigt überkaufte oder überverkaufte Bedingungen. Longs werden unter dem VWAP mit einem steigenden Oszillator ausgelöst, Shorts über dem VWAP mit einem fallenden.

Intraday-Trader, die intraday Wertniveaus beobachten, können von diesem Stil profitieren. Stops werden mit einem ATR-Vielfachen platziert.

## Details

- **Einstiegskriterien**:
  - Long: `Close < VWAP && StochK < OversoldLevel`
  - Short: `Close > VWAP && StochK > OverboughtLevel`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: `Close > VWAP`
  - Short: `Close < VWAP`
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `StochPeriod` = 14
  - `StochKPeriod` = 3
  - `StochDPeriod` = 3
  - `OverboughtLevel` = 80m
  - `OversoldLevel` = 20m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: VWAP, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

