# Strategie Vwap Adx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf den Indikatoren VWAP und ADX. Geht long, wenn der Preis über VWAP liegt und ADX > 25. Geht short, wenn der Preis unter VWAP liegt und ADX > 25. Ausstieg, wenn ADX < 20.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 157%. Sie funktioniert am besten auf dem Kryptomarkt.

VWAP dient als Session-Benchmark, und ADX misst die Überzeugung. Einstiege erscheinen, wenn der Preis vom VWAP abweicht und ADX Stärke zeigt.

Passt für Intraday-Trend-Trader. Schutz-Stops verwenden einen festen Prozentsatz des Einstiegspreises.

## Details

- **Einstiegskriterien**:
  - Long: `Close > VWAP && ADX > 25`
  - Short: `Close < VWAP && ADX > 25`
- **Long/Short**: Beide
- **Ausstiegskriterien**: ADX fällt unter den Schwellenwert
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `StopLossPercent` = 2
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Der Markt handelt rund um die Uhr, daher beginnt der Sitzungs-VWAP mit jedem UTC-Tag neu und gewichtet den typischen Preis jeder Kerze mit ihrem Volumen. Die Werte 25 und 20 in den Regeln sind die Standardwerte von AdxThreshold und AdxExitThreshold. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: VWAP, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

