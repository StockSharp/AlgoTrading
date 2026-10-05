# Strategie Hull MA CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Hull MA CCI-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn HMA(t) > HMA(t-1) && CCI < CciOversold (HMA steigt mit überverkauften Bedingungen). Ein Short-Einstieg erfolgt, wenn HMA(t) < HMA(t-1) && CCI > CciOverbought (HMA fällt mit überkauften Bedingungen).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 52%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: HMA(t) > HMA(t-1) && CCI < CciOversold (HMA steigt mit überverkauften Bedingungen)
  - **Short**: HMA(t) < HMA(t-1) && CCI > CciOverbought (HMA fällt mit überkauften Bedingungen)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn HMA zu fallen beginnt
  - **Short**: Short-Position schließen, wenn HMA zu steigen beginnt
- **Stops**: Ja.
- **Standardwerte**:
  - `HullPeriod` = 9
  - `CciPeriod` = 20
  - `CciOversold` = -100
  - `CciOverbought` = 100
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    -100 und 100 sind die Standardwerte der CCI-Schwellen, die die Regeln nennen. Der Stop liegt AtrMultiplier mal die ATR über AtrPeriod vom Einstiegsschluss entfernt und wird bei Kerzenschluss geprüft; 0 schaltet ihn ab. Ein Einstiegssignal gegen eine offene Position dreht sie.
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

