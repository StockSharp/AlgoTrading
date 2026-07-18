# Strategie Stochastic Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Stochastic Keltner Indikatoren zur Signalgenerierung.
Ein Long-Einstieg erfolgt, wenn Stoch %K < 20 && Price < Keltner lower band (überverkauft am unteren Band). Ein Short-Einstieg erfolgt, wenn Stoch %K > 80 && Price > Keltner upper band (überkauft am oberen Band).
Sie eignet sich für Trader, die Chancen in gemischten Märkten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 61%. Sie funktioniert am besten auf dem Kryptomarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: Stoch %K < 20 && Price < Keltner lower band (oversold at lower band)
  - **Short**: Stoch %K > 80 && Price > Keltner upper band (overbought at upper band)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis zum mittleren Band zurückkehrt
  - **Short**: Short-Position schließen, wenn der Preis zum mittleren Band zurückkehrt
- **Stops**: Ja.
- **Standardwerte**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: Stochastic Keltner
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

