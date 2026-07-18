# NonLagDot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie inspiriert durch den NonLagDot-Indikator. Der Indikator approximiert den Preistrend mit einem geglätteten gleitenden Durchschnitt und farbcodierten Punkten.
Die Strategie eröffnet eine Long-Position, wenn der Indikator nach oben dreht, und eine Short-Position, wenn er nach unten dreht.
Vorherige entgegengesetzte Positionen werden vor dem Öffnen einer neuen geschlossen.

## Details

- **Einstiegskriterien**:
  - Long: Indikator wechselt von abwärts zu aufwärts (Steigung des gleitenden Durchschnitts wird positiv)
  - Short: Indikator wechselt von aufwärts zu abwärts (Steigung wird negativ)
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: optionaler Stop-Loss in Prozent
- **Standardwerte**:
  - `Length` = 10
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `StopLossPercent` = 1m
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA-Steigungsannäherung von NonLagDot
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
