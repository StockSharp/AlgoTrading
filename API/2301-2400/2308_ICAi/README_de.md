# ICAi Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem adaptiven gleitenden Durchschnittsindikator ICAi. Der Indikator glättet den Preis und passt seine Steigung mithilfe der Standardabweichung an. Long-Positionen werden eröffnet, wenn der Indikator nach oben dreht; Short-Positionen, wenn er nach unten dreht.

Der Algorithmus funktioniert auf jedem Markt, wo Kerzendaten verfügbar sind. Standardeinstellungen verwenden einen 4-Stunden-Zeitrahmen und eine Glättungslänge von 12.

## Details

- **Einstiegskriterien**:
  - Long: `Prev < PrevPrev && Current >= Prev`
  - Short: `Prev > PrevPrev && Current <= Prev`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Optionaler fester Stop-Loss und Take-Profit
- **Standardwerte**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ICAi
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
