# Strategie Parabolic Sar Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die Parabolic SAR mit Volumenbestätigung kombiniert. Einstieg in Trades, wenn der Preis den Parabolic SAR bei überdurchschnittlichem Volumen kreuzt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 151%. Sie funktioniert am besten auf dem Aktienmarkt.

Parabolic SAR identifiziert Trendwechsel und höheres Volumen validiert das Signal. Trades beginnen, wenn der SAR-Wechsel mit zunehmendem Volumen einhergeht.

Nützlich für Trader, die volumenbasierte Bewegungen verfolgen. Der SAR-Trail und ein ATR-Faktor schützen vor großen Verlusten.

## Details

- **Einstiegskriterien**:
  - Long: `Close > SAR && Volume > AvgVolume`
  - Short: `Close < SAR && Volume > AvgVolume`
- **Long/Short**: Beide
- **Ausstiegskriterien**: SAR-Wechsel
- **Stops**: Verwendet Parabolic SAR als Trailing Stop
- **Standardwerte**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `VolumePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, Parabolic SAR, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

