# Parabolic SAR Bug
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Parabolic SAR Bug**-Strategie handelt Trendumkehrungen mit dem Parabolic-SAR-Indikator. Wenn der SAR unter den Preis kippt, geht die Strategie Long, und wenn der SAR über den Preis kippt, geht sie Short. Ein optionaler Reverse-Modus kehrt die Signale um. Schutz-Stop-Loss, Take-Profit und Trailing-Stop werden durch das integrierte Positionsschutzmodul unterstützt.

## Details

- **Einstiegskriterien**: Richtungsänderung des Parabolic SAR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes SAR-Signal oder Schutz-Stop.
- **Stops**: Stop-Loss, Take-Profit, optionaler Trailing-Stop.
- **Standardwerte**:
  - `Step` = 0.02
  - `MaxStep` = 0.2
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 1
  - `UseTrailingStop` = false
  - `Reverse` = false
  - `CloseOnSar` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Stop-Loss, Take-Profit
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
