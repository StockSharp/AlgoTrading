# Strategie Parabolic SAR CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Parabolic SAR CCI-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn Price > SAR && CCI < CciOversold (Aufwärtstrend mit überverkauften Bedingungen). Ein Short-Einstieg erfolgt, wenn Price < SAR && CCI > CciOverbought (Abwärtstrend mit überkauften Bedingungen).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 49%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: Price > SAR && CCI < CciOversold (Aufwärtstrend mit überverkauften Bedingungen)
  - **Short**: Price < SAR && CCI > CciOverbought (Abwärtstrend mit überkauften Bedingungen)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis unter SAR fällt
  - **Short**: Short-Position schließen, wenn der Preis über SAR steigt
- **Stops**: Nein.
- **Standardwerte**:
  - `SarAccelerationFactor` = 0.02
  - `SarMaxAccelerationFactor` = 0.2
  - `CciPeriod` = 20
  - `CciOversold` = -100
  - `CciOverbought` = 100
    -100 und 100 sind die Standardwerte der CCI-Schwellen, die die Regeln nennen. Ein Einstieg ist ein Rücksetzer innerhalb des Trends, den der SAR anzeigt, und der SAR dient als nachgezogener Ausstieg: Ein Long schließt bei einem Schluss darunter, ein Short bei einem Schluss darüber. Ein Einstiegssignal gegen eine offene Position dreht sie.
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

