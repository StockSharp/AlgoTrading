# Twisted SMA Strategie 4h
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Twisted SMA Strategie verwendet drei einfache gleitende Durchschnitte und einen KAMA-Filter auf 4-Stunden-Kerzen. Eine Long-Position wird eröffnet, wenn der schnelle SMA über dem mittleren, der mittlere über dem langsamen liegt, der Kurs über einem längeren SMA notiert und der KAMA nicht flach ist. Die Position wird geschlossen, wenn die SMAs bärisch ausgerichtet sind.

## Details

- **Einstiegskriterien**: schneller SMA > mittlerer SMA > langsamer SMA, Schluss > Haupt-SMA, KAMA nicht flach.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: schneller SMA < mittlerer SMA < langsamer SMA.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastLength` = 4
  - `MidLength` = 9
  - `SlowLength` = 18
  - `MainSmaLength` = 100
  - `KamaLength` = 25
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: SMA, KAMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
