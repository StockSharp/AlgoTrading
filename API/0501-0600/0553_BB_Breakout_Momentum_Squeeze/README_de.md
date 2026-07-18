# BB-Breakout-Momentum-Squeeze-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die BB Breakout Momentum Squeeze Strategie kombiniert einen Bollinger-Breakout-Oszillator mit einem Volatilitäts-Squeeze-Filter. Der Squeeze wird erkannt, wenn die Bollinger Bands die Keltner Channels nach außen verlassen, was eine potenzielle Expansion signalisiert. Ein Long-Trade entsteht, wenn der bullische Breakout-Oszillator während dieser Expansion über den Schwellenwert kreuzt, während ein Short-Trade den bearischen Kreuzung verwendet. Die Stops basieren auf einem ATR-Band und ein Risiko-Ertrag-Ziel vervollständigt die Ausstiegslogik.

## Details

- **Einstiegskriterien**:
  - Squeeze aus (Bollinger Bands außerhalb der Keltner Channels).
  - **Long**: Bullischer Oszillator kreuzt über den Schwellenwert.
  - **Short**: Bearischer Oszillator kreuzt über den Schwellenwert.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Kurs erreicht ATR-Stop oder Risiko-Ertrag-Ziel.
- **Stops**: ATR-Band mit Risiko-Ertrag-Ziel.
- **Standardwerte**:
  - `BbLength` = 14
  - `BbMultiplier` = 1.0
  - `Threshold` = 50
  - `SqueezeLength` = 20
  - `SqueezeBbMultiplier` = 2.0
  - `KcMultiplier` = 2.0
  - `AtrLength` = 30
  - `AtrMultiplier` = 1.4
  - `RrRatio` = 1.5
- **Filter**:
  - Kategorie: Volatilitäts-Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Keltner Channels, ATR
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
