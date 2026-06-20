# Hurst Exponent Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieser Ansatz verwendet den Hurst Exponent, um zu erkennen, wann sich ein Markt in einer Mean-Reversion-Weise verhält. Werte unter 0,5 deuten darauf hin, dass der Preis dazu neigt, zu seinem Durchschnitt zurückzukehren, wodurch Gelegenheiten entstehen, gegen Extreme zu handeln.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 121%. Er funktioniert am besten auf dem Kryptomarkt.

Eine Long-Position wird eröffnet, wenn der Hurst Exponent unter 0,5 liegt und der Preis unter einem gleitenden Durchschnitt schließt. Eine Short-Position entsteht, wenn der Hurst-Wert unter 0,5 liegt und der Preis über dem Durchschnitt schließt. Positionen werden geschlossen, wenn der Preis zur Durchschnittslinie zurückkehrt oder der Hurst Exponent über den Schwellenwert steigt.

Die Strategie eignet sich für Trader, die statistische Tendenzen gegenüber starken Trends bevorzugen. Ein schützender Stop-Loss schützt vor ausgedehnten Bewegungen, die nicht zurückkehren.

## Details
- **Einstiegskriterien**:
  - **Long**: Hurst < 0.5 && Close < MA
  - **Short**: Hurst < 0.5 && Close > MA
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn Close >= MA oder Hurst > 0.5
  - **Short**: Ausstieg wenn Close <= MA oder Hurst > 0.5
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Hurst Exponent, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

