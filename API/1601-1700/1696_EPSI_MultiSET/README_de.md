# EPSI Multi SET-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie, konvertiert aus dem originalen MQL4-Expert *e-PSI@MultiSET*.
Sie beobachtet jede Kerze und steigt ein, wenn sich der Kurs eine festgelegte Distanz vom Eröffnungskurs entfernt.
Positionen werden mit Take-Profit- und Stop-Loss-Leveln geschützt und Trades sind nur während eines
benutzerdefinierten Zeitfensters erlaubt.

## Details

- **Einstiegskriterien**:
  - Long: `High - Open >= MinDistance`
  - Short: `Open - Low >= MinDistance`
- **Long/Short**: Beide
- **Ausstiegskriterien**: TakeProfit oder StopLoss
- **Stops**: Ja
- **Standardwerte**:
  - `MinDistance` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 200
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `OpenHour` = 2
  - `CloseHour` = 20
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
