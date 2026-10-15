# Konsistente Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Consistent Momentum**-Strategie wählt Instrumente aus, die über zwei Zeitfenster hinweg ein starkes Momentum aufweisen, und gewichtet das Portfolio monatlich neu. Jede Tranche wird für eine feste Anzahl von Monaten gehalten, und das Kapital wird gleichmäßig auf Long- und Short-Körbe aufgeteilt.

## Details
- **Einstiegskriterien**: Am ersten Handelstag jedes Monats long bei Wertpapieren im obersten Dezil beider Momentum-Maße und short beim untersten Dezil.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Positionen werden nach Ablauf der Halteperiode oder bei Neugewichtung geschlossen.
- **Stops**: Keine explizite Stop-Logik; Positionsgröße basiert auf Dollar-Allokation.
- **Standardwerte**:
  - `LookbackDays = 7 * 21`
  - `HoldingMonths = 6`
  - `MinTradeUsd = 50`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Price momentum
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
