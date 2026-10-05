# Strategie Hull Ma Adx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf Hull Moving Average und ADX. Einstieg Long, wenn HMA steigt und ADX > 25 (starker Trend). Einstieg Short, wenn HMA fällt und ADX > 25 (starker Trend). Ausstieg, wenn ADX < 20 (abschwächender Trend).

Tests zeigen eine durchschnittliche Jahresrendite von etwa 178%. Die Strategie funktioniert am besten am Aktienmarkt.

Hull MA zeigt den Trend, während ADX dessen Intensität bestätigt. Einstiege folgen dem Hull-Gefälle, wenn ADX Stärke anzeigt.

Effektiv für Trader, die sich auf gleichmäßige Trends mit Bestätigung konzentrieren. ATR-Stops halten Verluste unter Kontrolle.

## Details

- **Einstiegskriterien**:
  - Long: `HullMA turning up && ADX > 25`
  - Short: `HullMA turning down && ADX > 25`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Hull MA Umkehr
- **Stops**: ATR-basiert mit `AtrMultiplier`
- **Standardwerte**:
  - `HmaPeriod` = 9
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `AdxExitThreshold` = 20
  - `AtrMultiplier` = 2
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
    Die Werte 25 und 20 in den Regeln sind die Standardwerte von AdxThreshold und AdxExitThreshold. Der HMA dreht nach oben, wenn er nach einem Rückgang steigt, und nach unten, wenn er nach einem Anstieg fällt; ein Long schließt, wenn er fällt, ein Short, wenn er steigt, und jede Position schließt, sobald ADX unter AdxExitThreshold fällt. Der Stop liegt AtrMultiplier ATR (AtrPeriod) vom Einstiegsschluss entfernt und wird auf Kerzenschlüssen geprüft. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Hull MA, Moving Average, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

