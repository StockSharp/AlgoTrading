# TTM Squeeze-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die TTM Squeeze-Strategie sucht nach Perioden der Preisverdichtung, wenn Bollinger
Bands innerhalb von Keltner Channels kontrahieren. Dieser "Squeeze" signalisiert eine
potenzielle Volatilitätsexpansion. Während des Squeeze überwacht die Strategie einen
linearen Regressions-Momentum-Oszillator und RSI, um die Richtung einzuschätzen. Wenn
der Squeeze nachlässt und das Momentum dreht, werden Positionen in Richtung der Bewegung
eingegangen.

Die Methode sucht nach explosiven Ausbrüchen aus ruhigen Ranges. Trades werden so
gefiltert, dass Long-Setups steigende Momentum-Werte unterhalb von null mit RSI über
30 benötigen, während Short-Setups fallendes Momentum aus positivem Bereich mit RSI
unter 70 erfordern. Ein optionaler Take-Profit-Parameter kann Trades bei einem
vordefinierten Gewinn automatisch schließen.

## Details

- **Einstiegskriterien**:
  - Squeeze aus (Bollinger Bands außerhalb der Keltner Channels).
  - **Long**: Momentum < 0 und steigend, RSI > 30.
  - **Short**: Momentum > 0 und fallend, RSI < 70.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal oder Take-Profit, wenn aktiviert.
- **Stops**: Standardmäßig keine, optionaler Take-Profit.
- **Standardwerte**:
  - `SqueezeLength` = 20
  - `RsiLength` = 14
  - `UseTP` = False
  - `TpPercent` = 1.2
- **Filter**:
  - Kategorie: Volatilitäts-Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Keltner Channels, RSI, Lineare Regression
  - Stops: Optional
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
