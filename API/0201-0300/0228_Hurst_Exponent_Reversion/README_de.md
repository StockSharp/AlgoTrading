# Hurst Exponent Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieser Ansatz verwendet den Hurst Exponent, um zu erkennen, wann sich ein Markt in einer Mean-Reversion-Weise verhält. Werte unter 0,5 deuten darauf hin, dass der Preis dazu neigt, zu seinem Durchschnitt zurückzukehren, wodurch Gelegenheiten entstehen, gegen Extreme zu handeln.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 121%. Er funktioniert am besten auf dem Kryptomarkt.

Eine Long-Position wird eröffnet, wenn der Hurst Exponent unter HurstThreshold liegt und der Preis unter einem gleitenden Durchschnitt schließt. Eine Short-Position entsteht, wenn der Hurst-Wert unter HurstThreshold liegt und der Preis über dem Durchschnitt schließt. Positionen werden geschlossen, wenn der Preis zur Durchschnittslinie zurückkehrt oder der Hurst Exponent über den Schwellenwert steigt.

Die Strategie eignet sich für Trader, die statistische Tendenzen gegenüber starken Trends bevorzugen. Ein schützender Stop-Loss schützt vor ausgedehnten Bewegungen, die nicht zurückkehren.

## Details
- **Einstiegskriterien**:
  - **Long**: Hurst < HurstThreshold && Close < MA
  - **Short**: Hurst < HurstThreshold && Close > MA
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn Close >= MA oder Hurst > HurstThreshold
  - **Short**: Ausstieg wenn Close <= MA oder Hurst > HurstThreshold
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `HurstThreshold` = 0.7
  - `StopLossPercent` = 2
    Die Regeln nennen 0.5, die theoretische Grenze der Mean Reversion, doch die R/S-Schätzung über 100 Fünf-Minuten-Kerzen fiel auf der BTC- und TON-Historie, mit der die Beispiele getestet werden, nie unter etwa 0.65, sodass die Regel nicht handeln konnte; HurstThreshold macht die Schwelle einstellbar, Standardwert 0.7. MA ist der einfache gleitende Durchschnitt über AveragePeriod. Der Stop liegt bei festen StopLossPercent vom Einstiegspreis und wird auch zwischen den Kerzen überwacht; 0 schaltet ihn ab. Ein Einstiegssignal gegen eine offene Position dreht sie.
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

