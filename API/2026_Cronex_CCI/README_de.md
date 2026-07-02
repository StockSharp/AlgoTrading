# Strategie Cronex CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Cronex-Commodity-Channel-Index-Kreuzung. Der Indikator glättet den CCI durch zwei exponentielle gleitende Durchschnitte, um eine schnelle und eine langsame Linie zu erzeugen.

Die Strategie eröffnet eine Long-Position, wenn die schnelle Linie die langsame von oben nach unten kreuzt, und schließt Short-Positionen. Eine Short-Position wird eröffnet, wenn die schnelle Linie die langsame von unten nach oben kreuzt und alle Long-Positionen schließt.

Dieser Gegentrend-Ansatz versucht, Umkehrungen nach Momentum-Wechseln zu erfassen. Er funktioniert auf höheren Zeitrahmen wie 4-Stunden-Kerzen.

## Details

- **Einstiegskriterien**: Kreuzungen der schnellen und langsamen geglätteten CCI-Linien.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Kreuzung.
- **Stops**: Nein.
- **Standardwerte**:
  - `CciPeriod` = 25
  - `FastPeriod` = 14
  - `SlowPeriod` = 25
  - `CandleType` = TimeSpan.FromHours(4)
  - `EnableLongEntry` = true
  - `EnableShortEntry` = true
  - `EnableLongExit` = true
  - `EnableShortExit` = true
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: CCI, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing (4h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
