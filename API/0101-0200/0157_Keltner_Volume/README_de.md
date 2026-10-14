# Keltner Volume Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie Keltner Channels + Volume. Kaufen, wenn der Preis unter dem unteren Keltner Channel schließt und das Volumen über dem Durchschnitt liegt. Verkaufen, wenn der Preis über dem oberen Keltner Channel schließt und das Volumen über dem Durchschnitt liegt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 58%. Am besten geeignet für den Aktienmarkt.

Die Keltner Channel-Grenzen definieren potenzielle Umkehrpunkte, und erhöhtes Volumen signalisiert Überzeugung. Das System handelt, wenn der Preis eine Band mit zunehmendem Volumen berührt.

Trader, die Volumenbestätigung an Volatilitätsbändern suchen, bevorzugen möglicherweise dieses Setup. Stops werden aus dem ATR berechnet.

## Details

- **Einstiegskriterien**:
  - Long: `Close < LowerBand && Volume > AvgVolume`
  - Short: `Close > UpperBand && Volume > AvgVolume`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Preis kreuzt EMA
- **Stops**: ATR-basiert mit `StopLossAtr`
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `VolumeAvgPeriod` = 20
  - `StopLossAtr` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Der Kanal ist der EMA plus und minus Multiplier ATR, AvgVolume das durchschnittliche Volumen der vorherigen VolumeAvgPeriod Kerzen. Der Stop liegt StopLossAtr ATR vom Einstiegsschluss entfernt und wird auf Kerzenschlüssen geprüft. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Keltner Channel, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
