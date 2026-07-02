# Strategie Keltner Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die Keltner Channels und den Stochastic Oszillator kombiniert.
Einstieg in Positionen, wenn der Preis die Keltner Channel-Grenzen erreicht und Stochastic überkaufte/überverkaufte Bedingungen bestätigt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 163%. Sie funktioniert am besten im Aktienmarkt.

Dieses Setup zielt darauf ab, Umkehrungen nahe den Keltner-Bändern zu erfassen, während der Oszillator Impulswechsel bestätigt. Signale können in beide Richtungen ausgelöst werden, wenn der Preis gegen einen Kanal drückt.

Kurzfristige Trader, die schnelle Umkehrungen suchen, können es nützlich finden. Das Risiko wird durch einen ATR-basierten Stop-Abstand begrenzt.

## Details

- **Einstiegskriterien**:
  - Long: `Close < LowerBand && StochK < StochOversold`
  - Short: `Close > UpperBand && StochK > StochOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: `Close > EMA`
  - Short: `Close < EMA`
- **Stops**: `StopLossAtr` ATR vom Einstieg
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `KeltnerMultiplier` = 2.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossAtr` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Keltner Channel, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

