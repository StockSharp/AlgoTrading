# Ultimate Balance Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Ultimate Balance Strategie kombiniert ROC, RSI, CCI, Williams %R und ADX zu einem gewichteten Oszillator. Ein gleitender Durchschnitt dieses Oszillators erzeugt Signale: Ein Kreuz nach oben über das überverkaufte Niveau löst einen Long aus, ein Kreuz nach unten unter das überkaufte Niveau schließt oder kehrt die Position um.

## Details

- **Einstiegskriterien**: Oszillator-MA kreuzt `OversoldLevel` nach oben.
- **Long/Short**: Beide (Shorts optional über `EnableShort`).
- **Ausstiegskriterien**: Oszillator-MA kreuzt `OverboughtLevel` nach unten.
- **Stops**: Nein.
- **Standardwerte**:
  - `WeightRoc` = 2
  - `WeightRsi` = 0.5
  - `WeightCci` = 2
  - `WeightWilliams` = 0.5
  - `WeightAdx` = 0.5
  - `EnableShort` = false
  - `OverboughtLevel` = 0.75
  - `OversoldLevel` = 0.25
  - `MaType` = SMA
  - `MaLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: ROC, RSI, CCI, WilliamsR, ADX
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
