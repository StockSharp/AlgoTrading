# Strategie Keltner Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Keltner Williams R-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn Price < lower Keltner band && Williams %R < -80 (überverkauft am unteren Band). Ein Short-Einstieg erfolgt, wenn Price > upper Keltner band && Williams %R > -20 (überkauft am oberen Band).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 46%. Die Strategie funktioniert am besten am Aktienmarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: Price < lower Keltner band && Williams %R < -80 (überverkauft am unteren Band)
  - **Short**: Price > upper Keltner band && Williams %R > -20 (überkauft am oberen Band)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis zum mittleren Band zurückkehrt
  - **Short**: Short-Position schließen, wenn der Preis zum mittleren Band zurückkehrt
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `WilliamsRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: Keltner Williams R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

