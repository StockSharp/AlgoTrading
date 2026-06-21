# Mechanische Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine zeitbasierte mechanische Strategie, die täglich zu einer bestimmten Stunde einen Trade ausführt. Die Positionsrichtung kann auf Long oder Short konfiguriert werden. Der Trade wird automatisch mit prozentualen Take-Profit- und Stop-Loss-Niveaus geschützt.

## Details

- **Einstiegskriterien**:
  - **Long**: Bei `TradeHour`, wenn `Short Mode` deaktiviert ist.
  - **Short**: Bei `TradeHour`, wenn `Short Mode` aktiviert ist.
- **Long/Short**: Beide, abhängig von `Short Mode`.
- **Ausstiegskriterien**:
  - `Profit Target (%)` ober-/unterhalb des Einstiegs.
  - `Stop Loss (%)` unter-/oberhalb des Einstiegs.
- **Stops**: Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `Profit Target (%)` = 0.4.
  - `Stop Loss (%)` = 0.2.
  - `Trade Hour` = 16.
- **Filter**:
  - Kategorie: Zeit
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
