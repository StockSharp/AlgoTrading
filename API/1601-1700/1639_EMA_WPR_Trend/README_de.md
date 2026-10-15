# EMA WPR Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen EMA-Trendfilter mit Williams-%R-Signalen kombiniert. Kauft bei überverkauften Niveaus und verkauft bei überkauften Niveaus. Ein Rückzugs-Schwellenwert verhindert aufeinanderfolgende Einstiege. Optionale Ausstiege schließen Trades bei entgegengesetzten Williams-%R-Extremen oder nach mehreren unrentablen Bars.

## Details

- **Einstiegskriterien**:
  - Long: Williams %R <= -100 und EMA-Trend aufwärts
  - Short: Williams %R >= 0 und EMA-Trend abwärts
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Williams %R kreuzt entgegengesetztes Extrem, wenn `UseWprExit` aktiviert ist
  - Position bleibt `MaxUnprofitBars` Bars unrentabel, wenn `UseUnprofitExit` aktiviert ist
- **Stops**: Nein
- **Standardwerte**:
  - `WprPeriod` = 46
  - `WprRetracement` = 30
  - `EmaPeriod` = 144
  - `BarsInTrend` = 1
  - `MaxUnprofitBars` = 5
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: EMA, Williams %R
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
