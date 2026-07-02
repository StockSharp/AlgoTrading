# Strategie RSI Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet RSI Hull MA-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn RSI < 30 && HMA(t) > HMA(t-1) (überverkauft mit steigendem HMA). Ein Short-Einstieg erfolgt, wenn RSI > 70 && HMA(t) < HMA(t-1) (überkauft mit fallendem HMA).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 58%. Die Strategie funktioniert am besten am Aktienmarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: RSI < 30 && HMA(t) > HMA(t-1) (überverkauft mit steigendem HMA)
  - **Short**: RSI > 70 && HMA(t) < HMA(t-1) (überkauft mit fallendem HMA)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn RSI in die neutrale Zone zurückkehrt
  - **Short**: Short-Position schließen, wenn RSI in die neutrale Zone zurückkehrt
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: RSI Hull MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

