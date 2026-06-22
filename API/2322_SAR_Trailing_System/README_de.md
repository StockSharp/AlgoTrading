# SAR Trailing-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die in festen Zeitintervallen zufällige Long- oder Short-Positionen eingeht und den Ausstieg mithilfe des Parabolic-SAR-Indikators verwaltet.
Der Parabolic-SAR-Wert fungiert als Trailing Stop: Die Position wird geschlossen, wenn der Preis das SAR-Level kreuzt.

## Details

- **Einstiegskriterien**:
  - Jedes `TimerInterval`, wenn keine offene Position vorhanden und `UseRandomEntry` aktiviert ist, wird ein zufälliger Long- oder Short-Trade eröffnet.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Preis, der den Parabolic SAR kreuzt.
- **Stops**: Anfänglicher Stop-Loss in Ticks mit Parabolic-SAR-Trailing-Ausstieg.
- **Standardwerte**:
  - `TimerInterval` = 300 Sekunden
  - `StopLossTicks` = 10
  - `AccelerationStep` = 0.02
  - `AccelerationMax` = 0.2
  - `UseRandomEntry` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
