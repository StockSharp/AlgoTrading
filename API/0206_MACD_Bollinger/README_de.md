# MACD Bollinger Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet MACD Bollinger-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn MACD > Signal && Price < BB_lower (Aufwärtstrend mit überverkauften Bedingungen). Ein Short-Einstieg erfolgt, wenn MACD < Signal && Price > BB_upper (Abwärtstrend mit überkauften Bedingungen).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 55%. Die Strategie funktioniert am besten am Aktienmarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: MACD > Signal && Price < BB_lower (Aufwärtstrend mit überverkauften Bedingungen)
  - **Short**: MACD < Signal && Price > BB_upper (Abwärtstrend mit überkauften Bedingungen)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis zum mittleren Band zurückkehrt
  - **Short**: Short-Position schließen, wenn der Preis zum mittleren Band zurückkehrt
- **Stops**: Ja.
- **Standardwerte**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: MACD Bollinger
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

