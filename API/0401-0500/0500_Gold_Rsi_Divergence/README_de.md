# Gold-RSI-Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Gold-RSI-Divergenz-Strategie scalpiert Gold, indem sie bullische und bärische Divergenzen zwischen dem Preis und dem Relative Strength Index (RSI) identifiziert.
Wenn der Preis ein neues Tief markiert, aber der RSI ein höheres Tief druckt, sucht die Strategie nach einem Kauf.
Umgekehrt, wenn der Preis ein neues Hoch markiert, aber der RSI ein niedrigeres Hoch druckt, verkauft die Strategie.
Beide Setups werden nur bestätigt, wenn zwei Pivots innerhalb eines konfigurierbaren Balkenbands auftreten.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis tieferes Tief, RSI höheres Tief, RSI < 40.
  - **Short**: Preis höheres Hoch, RSI niedrigeres Hoch, RSI > 60.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Verwendet Stop-Loss und Take-Profit.
- **Stops**: Fester Stop-Loss und Take-Profit in Pips.
- **Standardwerte**:
  - `RsiLength` = 60
  - `StopLossPips` = 11
  - `TakeProfitPips` = 33
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
