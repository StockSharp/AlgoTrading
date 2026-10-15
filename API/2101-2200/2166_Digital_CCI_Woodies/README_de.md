# Digital CCI Woodies-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf dem Crossover zweier Commodity Channel Index (CCI)-Indikatoren. Ein schneller CCI reagiert schnell auf Preisänderungen, während ein langsamer CCI das Marktrauschen glättet. Signale werden erzeugt, wenn die schnelle Linie die langsame kreuzt.

## Details

- **Einstiegskriterien**:
  - Long: schneller CCI kreuzt über den langsamen CCI.
  - Short: schneller CCI kreuzt unter den langsamen CCI.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Long-Positionen werden geschlossen, wenn schneller CCI unter den langsamen CCI kreuzt.
  - Short-Positionen werden geschlossen, wenn schneller CCI über den langsamen CCI kreuzt.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = 6-Stunden-Kerzen
  - `FastLength` = 14
  - `SlowLength` = 6
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: CCI
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
