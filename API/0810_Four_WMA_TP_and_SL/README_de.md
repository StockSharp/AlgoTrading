# Vier-WMA-Strategie mit TP und SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Kreuzungen von vier gleitenden Durchschnitten mit optionalem Take-Profit, Stop-Loss und alternativer Ausstiegsbedingung nutzt.

## Details

- **Einstiegskriterien**:
  - Long: Long MA1 kreuzt Long MA2 von unten
  - Short: Short MA1 kreuzt Short MA2 von oben
- **Long/Short**: Konfigurierbar
- **Stops**: Prozentbasiertes TP und SL
- **Standardwerte**:
  - `LongMa1Length` = 10
  - `LongMa2Length` = 20
  - `ShortMa1Length` = 30
  - `ShortMa2Length` = 40
  - `MaType` = Wma
  - `EnableTpSl` = true
  - `TakeProfitPercent` = 1m
  - `StopLossPercent` = 1m
  - `Direction` = Both
  - `EnableAltExit` = false
  - `AltExitMaOption` = LongMa1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Gleitende Durchschnitte
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
