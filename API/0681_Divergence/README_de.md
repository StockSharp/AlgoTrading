# Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf Preis- und RSI-Divergenz mit einfacher Pivot-Erkennung.

Die Divergenz-Strategie verwendet Pivot-Hochs und -Tiefs in Preis und RSI, um bullische und bärische Divergenzen zu erkennen. Wenn der Preis ein neues Hoch bildet, der RSI dies aber nicht bestätigt, verkauft die Strategie. Wenn der Preis dagegen ein neues Tief bildet, während der RSI steigt, kauft sie.

## Details

- **Einstiegskriterien**: Preis- und RSI-Divergenzen.
- **Long/Short**: Beide Richtungen (konfigurierbar).
- **Ausstiegskriterien**: Entgegengesetztes RSI-Signal oder Schutzaufträge.
- **Stops**: Ja (Stop-Loss und Take-Profit).
- **Standardwerte**:
  - `TradeDirection` = Both
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
