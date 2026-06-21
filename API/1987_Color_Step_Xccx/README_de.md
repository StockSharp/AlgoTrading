# Color Step Xccx-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Color Step XCCX-Indikator. Der Indikator misst die Abweichung des Preises von einem geglätteten Durchschnitt und zeichnet zwei Stufenlinien. Ein Long-Trade wird eröffnet, wenn die schnelle Linie unter die langsame Linie fällt. Ein Short-Trade wird eröffnet, wenn die schnelle Linie über die langsame Linie steigt.

## Details

- **Einstiegskriterien**:
  - Long: schnelle Linie kreuzt unter die langsame Linie
  - Short: schnelle Linie kreuzt über die langsame Linie
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: schnelle Linie kreuzt über die langsame Linie
  - Short: schnelle Linie kreuzt unter die langsame Linie
- **Stops**: Keine
- **Standardwerte**:
  - `DPeriod` = 30
  - `MPeriod` = 7
  - `StepSizeFast` = 5
  - `StepSizeSlow` = 30
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Custom, EMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
