# Aroon WPR Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die Aroon-Kreuzungen mit Williams-%R-Momentum-Filtern kombiniert. Ein Long-Trade wird eröffnet, wenn die schnelle Aroon-Up-Linie Aroon Down nach oben kreuzt, während Williams %R eine überverkaufte Umgebung bestätigt. Short-Trades folgen der umgekehrten Logik mit Williams %R im überkauften Bereich. Offene Positionen können durch Williams-%R-Umkehrungen oder durch optionale Stop-Loss- und Take-Profit-Niveaus in Preisschritten geschlossen werden.

## Details

- **Einstiegskriterien**:
  - Long: Aroon Up kreuzt Aroon Down nach oben und Williams %R < `-(100 - OpenWprLevel)`
  - Short: Aroon Down kreuzt Aroon Up nach oben und Williams %R > `-OpenWprLevel`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Williams %R verlässt die durch `CloseWprLevel` definierte überverkaufte/überkaufte Zone
  - Optionale Take-Profit- und Stop-Loss-Schwellen in Preisschritten
- **Stops**: Optionaler fester Stop-Loss und Take-Profit in Preisschritten
- **Standardwerte**:
  - `AroonPeriod` = 14
  - `WprPeriod` = 35
  - `OpenWprLevel` = 20
  - `CloseWprLevel` = 10
  - `TakeProfitSteps` = 0m (deaktiviert)
  - `StopLossSteps` = 0m (deaktiviert)
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Aroon, Williams %R
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
