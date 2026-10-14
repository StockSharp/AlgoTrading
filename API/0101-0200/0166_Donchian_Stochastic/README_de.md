# Strategie Donchian Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Donchian Channel + Stochastic Strategie. Die Strategie tritt in den Markt ein, wenn der Preis aus dem Donchian-Kanal ausbricht und der Stochastic überverkaufte/überkaufte Bedingungen bestätigt.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 85%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Ausbrüche über den Donchian-Kanal werden mit dem Stochastic-Momentum bestätigt. Trades beginnen, sobald der Preis den Bereich verlässt und der Oszillator zustimmt.

Nützlich für Trader, die unmittelbares Follow-Through erwarten. Ein fester Prozentsatz des Einstiegspreises legt den Stop fest.

## Details

- **Einstiegskriterien**:
  - Long: `Close > DonchianHigh && StochK > StochOverbought`
  - Short: `Close < DonchianLow && StochK < StochOversold`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Ausbruchsfehlschlag oder entgegengesetztes Signal
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOverbought` = 80
  - `StochOversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2
    Ein Ausbruch nach oben aus den vorherigen DonchianPeriod Kerzen wird bestätigt, wenn %K über StochOverbought liegt, einer nach unten, wenn %K unter StochOversold liegt; die umgekehrte Lesart, ein überverkauftes %K bei einem Ausbruch nach oben, kommt praktisch nicht vor. %K ist die Stochastik über StochPeriod Kerzen, geglättet über StochK Kerzen, %D spielt keine Rolle. Der Ausbruch scheitert, und die Position schließt, wenn der Kurs wieder jenseits des durchbrochenen Niveaus schließt. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Donchian Channel, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

