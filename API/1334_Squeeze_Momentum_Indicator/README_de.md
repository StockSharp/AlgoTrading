# Squeeze-Momentum-Indikator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Squeeze-Momentum-Indikator-Strategie erkennt Volatilitätskontraktionen, wenn Bollinger Bänder innerhalb der Keltner-Kanäle liegen. Eine Long-Position wird eröffnet, wenn der Squeeze nach oben auflöst, mit steigendem Momentum und Preis über dem 100-Perioden-EMA. Shorts werden bei einer Abwärtsauflösung mit fallendem Momentum und Preis unterhalb des EMA eingegangen. Positionen werden bei einer Momentum-Umkehr geschlossen.

## Details

- **Einstiegskriterien**:
  - Bollinger Bänder bewegen sich außerhalb der Keltner-Kanäle (Squeeze-Auflösung).
  - **Long**: Momentum steigt, Preis über vorherigem Schlusskurs und EMA100, und die Squeeze-Farbe wechselt von Schwarz zu Grau.
  - **Short**: Momentum sinkt, Preis unter vorherigem Schlusskurs und EMA100, und die Farbe wechselt von Grau zu Schwarz.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Momentum kehrt um.
- **Stops**: Keine.
- **Standardwerte**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `KcLength` = 20
  - `KcMultiplier` = 1.5
  - `EmaLength` = 100
- **Filter**:
  - Kategorie: Volatilitätsausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Keltner Channels, Linear Regression, EMA
  - Stops: Keine
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
