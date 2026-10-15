# CandlesticksBW-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den Bill Williams CandlesticksBW-Ansatz. Jede Kerze wird anhand des Momentums des Awesome Oscillator (AO) und des Accelerator Oscillator (AC) eingefärbt. Die Strategie öffnet oder schließt Positionen basierend auf Übergängen zwischen bullischen und bearischen Farben.

## Funktionsweise
- Berechnet AO als Differenz zwischen 5- und 34-Perioden-SMAs des Medianpreises.
- Berechnet AC als AO minus 5-Perioden-SMA des AO.
- Jede Kerze wird in sechs Farben eingeteilt, abhängig vom AO/AC-Wachstum und der Kerzenrichtung.
- Ein bullisches Setup tritt auf, wenn die vorletzte Kerze bullisch ist (Farbe 0 oder 1). Wenn die Farbe der letzten Kerze über 1 liegt, wird eine Long-Position eröffnet und Short-Positionen werden geschlossen.
- Ein bearisches Setup tritt auf, wenn die vorletzte Kerze bearisch ist (Farbe 4 oder 5). Wenn die Farbe der letzten Kerze unter 4 liegt, wird eine Short-Position eröffnet und Long-Positionen werden geschlossen.
- Stops und Ziele werden über `StartProtection` angewendet.

## Parameter
- `CandleType` – Kerzen-Zeitrahmen.
- `SignalBar` – Versatz-Balken für die Signalauswertung.
- `StopLoss` – Stop-Loss-Distanz in Punkten.
- `TakeProfit` – Take-Profit-Distanz in Punkten.
- `BuyPosOpen` – Long-Positionen öffnen erlauben.
- `SellPosOpen` – Short-Positionen öffnen erlauben.
- `BuyPosClose` – Long-Positionen schließen erlauben.
- `SellPosClose` – Short-Positionen schließen erlauben.

## Indikatoren
- Awesome Oscillator (aus SMAs abgeleitet).
- Accelerator Oscillator.

## Handelsregeln
- **Long-Einstieg:** vorletzte Kerzenfarbe <2 und letzte Farbe >1.
- **Short-Einstieg:** vorletzte Kerzenfarbe >3 und letzte Farbe <4.
- **Long-Ausstieg:** bei Short-Einstiegsbedingung wenn Position >0.
- **Short-Ausstieg:** bei Long-Einstiegsbedingung wenn Position <0.
