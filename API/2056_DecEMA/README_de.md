# DecEMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den DecEMA-Indikator verwendet, um die Trendrichtung zu verfolgen. Der Indikator wendet zehn aufeinanderfolgende exponentielle Glättungen an und kombiniert sie zu einem gleitenden Durchschnitt mit geringer Verzögerung. Die Strategie vergleicht die letzten drei DecEMA-Werte. Wenn die Linie nach oben dreht und der neueste Wert den vorherigen überschreitet, kauft sie und schließt jede Short-Position. Wenn die Linie nach unten dreht und der neueste Wert unter dem vorherigen liegt, verkauft sie und schließt jede Long-Position.

## Details

- **Einstiegskriterien**:
  - Long: DecEMA-Steigung dreht nach oben und aktueller Wert > vorheriger Wert
  - Short: DecEMA-Steigung dreht nach unten und aktueller Wert < vorheriger Wert
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: Steigung dreht nach unten
  - Short: Steigung dreht nach oben
- **Stops**: Keine
- **Standardwerte**:
  - `EmaPeriod` = 3
  - `Length` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: DecEMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
