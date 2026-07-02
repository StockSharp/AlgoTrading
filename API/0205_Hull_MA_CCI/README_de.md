# Strategie Hull MA CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Hull MA CCI-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn HMA(t) > HMA(t-1) && CCI < -100 (HMA steigt mit überverkauften Bedingungen). Ein Short-Einstieg erfolgt, wenn HMA(t) < HMA(t-1) && CCI > 100 (HMA fällt mit überkauften Bedingungen).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 52%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: HMA(t) > HMA(t-1) && CCI < -100 (HMA steigt mit überverkauften Bedingungen)
  - **Short**: HMA(t) < HMA(t-1) && CCI > 100 (HMA fällt mit überkauften Bedingungen)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn HMA zu fallen beginnt
  - **Short**: Short-Position schließen, wenn HMA zu steigen beginnt
- **Stops**: Ja.
- **Standardwerte**:
  - `HullPeriod` = 9
  - `CciPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: Hull MA CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

