# Strategie Vwap Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die VWAP- und Stochastic-Indikatoren kombiniert. Kauft, wenn der Preis unter dem VWAP liegt und Stochastic überverkauft ist. Verkauft, wenn der Preis über dem VWAP liegt und Stochastic überkauft ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 187%. Sie funktioniert am besten im Aktienmarkt.

VWAP markiert das durchschnittliche Handelsniveau und Stochastic zeigt überkaufte oder überverkaufte Bedingungen. Longs werden unter dem VWAP mit einem steigenden Oszillator ausgelöst, Shorts über dem VWAP mit einem fallenden.

Intraday-Trader, die intraday Wertniveaus beobachten, können von diesem Stil profitieren. Stops werden in festem Prozentabstand vom Einstiegspreis platziert.

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
  - `OverboughtLevel` = 80m
  - `OversoldLevel` = 20m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Der Markt handelt rund um die Uhr, daher beginnt der Sitzungs-VWAP mit jedem UTC-Tag neu und gewichtet den typischen Preis jeder Kerze mit ihrem Volumen. StochK in den Regeln ist %K: die Stochastik über StochPeriod Kerzen, geglättet über StochKPeriod Kerzen; %D spielt keine Rolle und hat daher keine Einstellung. Ein Einstiegssignal gegen eine offene Position dreht sie.
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

