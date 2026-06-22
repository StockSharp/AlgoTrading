# Drei-Linien-Durchbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die vom Three Line Break-Indikator erkannte Umkehrungen handelt.
Der Indikator vergleicht das aktuelle Hoch und Tief mit dem höchsten Hoch und niedrigsten Tief der vorherigen N abgeschlossenen Kerzen.
Ein Ausbruch über das jüngste Hoch während eines Abwärtstrends signalisiert einen neuen Aufwärtstrend und löst einen Long-Einstieg aus; ein Einbruch unter das jüngste Tief während eines Aufwärtstrends löst einen Short-Einstieg aus.
Positionen werden bei jedem Signal umgekehrt.

## Details

- **Einstiegskriterien**:
  - Long: `Downtrend` wechselt zu `Uptrend`
  - Short: `Uptrend` wechselt zu `Downtrend`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal (Positionsumkehr)
- **Stops**: Nein
- **Standardwerte**:
  - `LinesBreak` = 3
  - `CandleType` = TimeSpan.FromHours(12).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Highest, Lowest (Three Line Break-Logik)
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
