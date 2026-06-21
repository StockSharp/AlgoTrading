# Momentum Long + Short-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Momentum-Strategie handelt sowohl Long- als auch Short-Positionen auf einem 3-Stunden-Zeitrahmen. Long-Setups erfordern, dass der Preis über den gleitenden Durchschnitten der Perioden 100 und 500 bleibt, und können durch RSI, ADX, ATR und Trendausrichtung gefiltert werden. Short-Einstiege suchen nach einem Preisausbruch unter das untere Bollinger Band, während der Preis unter beiden Durchschnitten bleibt, mit optionaler ATR-Bestätigung und der Möglichkeit, Shorts während starker Aufwärtstrends zu blockieren.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis über MA100 und MA500, optionale Trendausrichtung, RSI über seinem geglätteten Wert, ADX über seinem geglätteten Wert und ATR über seinem geglätteten Wert.
  - **Short**: Preis unter MA100 und MA500, unter dem unteren Bollinger Band, RSI unter Schwellenwert, ATR über seinem geglätteten Wert und optionale Aufwärtstrend-Blockierung.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Stop-Loss bei `slPercentLong`% unter dem Einstieg; frühzeitiger Ausstieg, wenn der Preis unter MA500 fällt.
  - **Short**: Stop-Loss und Take-Profit basierend auf den Prozentsätzen `slPercentShort` und `tpPercentShort`.
- **Stops**: Ja.
- **Standardwerte**:
  - `slPercentLong = 3`
  - `slPercentShort = 3`
  - `tpPercentShort = 4`
  - `rsiLengthLong = 14`
  - `rsiLengthShort = 14`
  - `adxLength = 14`
  - `atrLength = 14`
  - `bbLength = 20`
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
