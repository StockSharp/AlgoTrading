# RSI Donchian Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die RSI Donchian Strategie sucht nach Momentum-Extremen, die mit Ausbrüchen aus dem Donchian Channel zusammenfallen. Der Relative-Stärke-Index misst überkaufte und überverkaufte Bedingungen, während der Kanal die jüngsten Kurshochs und -tiefs definiert.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 82%. Sie funktioniert am besten auf dem Aktienmarkt.

Ein Kaufsignal erscheint, wenn der RSI unter 30 fällt und der Preis über das obere Donchian-Band bricht. Ein Short-Signal entsteht, wenn der RSI über 70 steigt und der Preis durch das untere Band fällt. Ausstiege erfolgen, sobald der Preis zur Donchian-Mittellinie zurückkehrt, was eine Rückkehr zum Gleichgewicht signalisiert.

Diese Methode eignet sich gut für aktive Trader, die gegen Erschöpfungsbewegungen handeln möchten, aber dennoch mit klaren Ausbruch-Levels agieren. Der Stop-Loss hilft, das Risiko zu begrenzen, wenn das Momentum nicht schnell umkehrt.

## Details
- **Einstiegskriterien**:
  - **Long**: RSI < 30 && Close > Donchian High
  - **Short**: RSI > 70 && Close < Donchian Low
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn close < Donchian Middle
  - **Short**: Ausstieg, wenn close > Donchian Middle
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: RSI, Donchian Channel
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

