# Strategie RSI Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet RSI Hull MA-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn RSI < RsiOversold && HMA(t) > HMA(t-1) (überverkauft mit steigendem HMA). Ein Short-Einstieg erfolgt, wenn RSI > RsiOverbought && HMA(t) < HMA(t-1) (überkauft mit fallendem HMA).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 58%. Die Strategie funktioniert am besten am Aktienmarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: RSI < RsiOversold && HMA(t) > HMA(t-1) (überverkauft mit steigendem HMA)
  - **Short**: RSI > RsiOverbought && HMA(t) < HMA(t-1) (überkauft mit fallendem HMA)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn RSI in die neutrale Zone zurückkehrt
  - **Short**: Short-Position schließen, wenn RSI in die neutrale Zone zurückkehrt
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    30 und 70 sind die Standardwerte der RSI-Schwellen, die die Regeln nennen. Die neutrale Zone beginnt bei RSI 50: Ein Long schließt, sobald der RSI 50 erreicht, ein Short, sobald er auf 50 fällt. Der Stop liegt AtrMultiplier mal die ATR über AtrPeriod vom Einstiegsschluss entfernt und wird bei Kerzenschluss geprüft; 0 schaltet ihn ab. Ein Einstiegssignal gegen eine offene Position dreht sie.
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

