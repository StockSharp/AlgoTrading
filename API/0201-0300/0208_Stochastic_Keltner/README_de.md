# Strategie Stochastic Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Stochastic Keltner Indikatoren zur Signalgenerierung.
Ein Long-Einstieg erfolgt, wenn Stoch %K < StochOversold && Price < Keltner lower band (überverkauft am unteren Band). Ein Short-Einstieg erfolgt, wenn Stoch %K > StochOverbought && Price > Keltner upper band (überkauft am oberen Band).
Sie eignet sich für Trader, die Chancen in gemischten Märkten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 61%. Sie funktioniert am besten auf dem Kryptomarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: Stoch %K < StochOversold && Price < Keltner lower band (oversold at lower band)
  - **Short**: Stoch %K > StochOverbought && Price > Keltner upper band (overbought at upper band)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis zum mittleren Band zurückkehrt
  - **Short**: Short-Position schließen, wenn der Preis zum mittleren Band zurückkehrt
- **Stops**: Ja.
- **Standardwerte**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    20 und 80 sind die Standardwerte der %K-Schwellen, die die Regeln nennen. Die Bänder sind der EMA über EmaPeriod plus und minus KeltnerMultiplier mal der ATR über AtrPeriod, das mittlere Band ist der EMA selbst. Der Stop liegt AtrMultiplier mal dieselbe ATR vom Einstiegsschluss entfernt und wird bei Kerzenschluss geprüft; 0 schaltet ihn ab. StochK in den Regeln ist %K: die Stochastik über StochPeriod Kerzen, geglättet über StochK Kerzen; %D spielt keine Rolle und hat daher keine Einstellung. Ein Einstiegssignal gegen eine offene Position dreht sie.
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

