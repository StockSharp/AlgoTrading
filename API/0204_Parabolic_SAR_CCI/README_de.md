# Parabolic SAR CCI Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Parabolic SAR CCI-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn Price > SAR && CCI < -100 (Aufwärtstrend mit überverkauften Bedingungen). Ein Short-Einstieg erfolgt, wenn Price < SAR && CCI > 100 (Abwärtstrend mit überkauften Bedingungen).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 49%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: Price > SAR && CCI < -100 (Aufwärtstrend mit überverkauften Bedingungen)
  - **Short**: Price < SAR && CCI > 100 (Abwärtstrend mit überkauften Bedingungen)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis unter SAR fällt
  - **Short**: Short-Position schließen, wenn der Preis über SAR steigt
- **Stops**: Nein.
- **Standardwerte**:
  - `SarAccelerationFactor` = 0.02m
  - `SarMaxAccelerationFactor` = 0.2m
  - `CciPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: Parabolic SAR CCI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

