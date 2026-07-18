# RSI Mean Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verfolgt den Relative Strength Index und misst seinen Abstand von einem Durchschnittsniveau. Wenn RSI um mehr als ein Vielfaches seiner jüngsten Standardabweichung abweicht, erwartet der Algorithmus eine Rückkehr zum Mittelwert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 61%. Er funktioniert am besten auf dem Kryptomarkt.

Ein Long-Trade wird eröffnet, wenn RSI unter das untere Band fällt, das durch den Durchschnitt minus `Multiplier` mal die Standardabweichung definiert wird. Ein Short-Trade wird eingegangen, wenn RSI über das obere Band steigt. Ausstiege erfolgen, wenn RSI zu seinem gleitenden Durchschnitt zurückkehrt.

Die Methode eignet sich für Trader, die nach objektiven überverkauften und überkauften Signalen suchen. Die Verwendung eines volatilitätsbasierten Bandes passt die Schwellenwerte an die aktuellen Marktbedingungen an, während ein Stop-Loss die Verluste begrenzt.

## Details
- **Einstiegskriterien**:
  - **Long**: RSI < Avg - Multiplier * StdDev
  - **Short**: RSI > Avg + Multiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn RSI > Avg
  - **Short**: Ausstieg wenn RSI < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

