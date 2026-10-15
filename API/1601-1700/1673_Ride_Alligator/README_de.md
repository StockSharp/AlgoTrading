# Alligator-Folge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Ride Alligator Strategie. Die Methode verwendet drei gleitende Durchschnitte, die als Alligator-Indikator bekannt sind. Eine Long-Position wird eröffnet, wenn die Lips-Linie die Jaws-Linie nach oben kreuzt, während die Teeth-Linie unterhalb der Jaws liegt. Eine Short-Position wird eröffnet, wenn Lips die Jaws nach unten kreuzt und die Teeth-Linie oberhalb der Jaws liegt. Die offene Position ist durch einen Stop an der Jaws-Linie geschützt, der nachzieht, wenn sich die Linie bewegt.

## Details

- **Einstiegskriterien**:
  - Long: `Lips > Jaws && Teeth < Jaws && previous Lips < previous Jaws`
  - Short: `Lips < Jaws && Teeth > Jaws && previous Lips > previous Jaws`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: `price <= Jaws`
  - Short: `price >= Jaws`
- **Stops**: Trailing Stop an Alligator Jaws
- **Standardwerte**:
  - `AlligatorPeriod` = 5
  - `MaType` = MovingAverageTypeEnum.Weighted
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Alligator
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
