# Bollinger Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die Bollinger Bands-Ausbrüche mit Volumenbestätigung nutzt.
Einstieg in Positionen, wenn der Preis über/unter die Bollinger Bands bricht und dabei erhöhtes Volumen vorliegt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 178%. Sie funktioniert am besten im Aktienmarkt.

Bollinger-Bänder zeigen Volatilitätsexpansion und das Volumen bestätigt den Ausbruch. Positionen werden eröffnet, wenn der Preis außerhalb eines Bands mit starker Aktivität schließt.

Geeignet für Ausbruchs-Trader, die eine Fortsetzung erwarten. Ein ATR-basierter Stop hält Verluste überschaubar.

## Details

- **Einstiegskriterien**:
  - Long: `Close > UpperBand && Volume > AvgVolume * VolumeMultiplier`
  - Short: `Close < LowerBand && Volume > AvgVolume * VolumeMultiplier`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Preis kehrt zum mittleren Band zurück
- **Stops**: ATR-basiert mit `StopLossAtr`
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

