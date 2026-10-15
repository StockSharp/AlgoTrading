# Exp CyclePeriod-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den CyclePeriod-Indikator, um Marktzykluswenden zu erkennen. Sie eröffnet Long-Positionen, wenn der Indikator steigt, und Short-Positionen, wenn er fällt, und schließt dabei entgegengesetzte Positionen entsprechend.

## Details

- **Einstiegskriterien:**
  - **Long**: CyclePeriod steigt und der aktuelle Wert liegt über dem vorherigen.
  - **Short**: CyclePeriod fällt und der aktuelle Wert liegt unter dem vorherigen.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien:**
  - Short schließen, wenn CyclePeriod nach oben dreht.
  - Long schließen, wenn CyclePeriod nach unten dreht.
- **Stops**: Verwendet Take-Profit und Stop-Loss in Preiseinheiten.
- **Standardwerte:**
  - `CandleType` = TimeSpan.FromHours(6).TimeFrame().
  - `Alpha` = 0.07.
  - `SignalBar` = 1.
  - `TakeProfit` = 2000.
  - `StopLoss` = 1000.
  - `BuyPosOpen` = true.
  - `SellPosOpen` = true.
  - `BuyPosClose` = true.
  - `SellPosClose` = true.
- **Filter:**
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: CyclePeriod
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 6 Stunden
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
