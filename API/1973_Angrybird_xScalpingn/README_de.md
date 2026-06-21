# Angrybird xScalpingn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Angrybird xScalpingn ist eine Martingal-Scalping-Strategie. Sie eröffnet einen ersten Trade basierend auf der kurzfristigen Preisrichtung und einem RSI-Filter. Wenn sich der Preis gegen die offene Position um einen dynamischen Schritt bewegt, der vom aktuellen Range abgeleitet wird, fügt die Strategie einen weiteren Trade mit einem Volumen hinzu, das mit einem Faktor multipliziert wird. Alle Positionen werden geschlossen, wenn der CCI eine starke Gegenbewegung anzeigt oder Stop-Loss bzw. Take-Profit erreicht wird.

## Details

- **Einstiegskriterien**: Der erste Trade folgt der aktuellen Schlusskursrichtung mit einem RSI-Filter. Zusätzliche Trades werden eröffnet, wenn der Preis um den berechneten Schritt gegen die Position läuft.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: CCI-Umkehr oder schützender Stop-Loss/Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `Volume` = 0.01
  - `LotExponent` = 2
  - `DynamicPips` = true
  - `DefaultPips` = 12
  - `Depth` = 24
  - `Del` = 3
  - `TakeProfit` = 20
  - `StopLoss` = 500
  - `Drop` = 500
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `MaxTrades` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Grid
  - Richtung: Beide
  - Indikatoren: RSI, CCI
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
