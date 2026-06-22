# Delta WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Delta WPR vergleicht einen schnellen und einen langsamen Williams %R Oszillator, um Momentumverschiebungen zu erfassen. Wenn der schnelle Wert den langsamen überschreitet und der langsame Oszillator über einem Schwellenniveau bleibt, eröffnet die Strategie eine Long-Position und schließt jedes Short-Engagement. Die entgegengesetzte Konfiguration — schnell unter langsam mit dem langsamen Oszillator unter dem Niveau — löst einen Short-Einstieg aus. Jede neue Kerze wird erst nach der Fertigstellung verarbeitet, um Rauschen zu vermeiden.

Backtests mit 4-Stunden-Daten zeigen, dass der Ansatz am besten in Seitwärtsmärkten funktioniert, wo Williams %R zwischen überkauften und überverkauften Zonen oszilliert.

## Details

- **Einstiegskriterien**:
  - Long: `WPR slow > Level && WPR fast > WPR slow`
  - Short: `WPR slow < Level && WPR fast < WPR slow`
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenteiliges Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 30
  - `Level` = -50m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: WilliamsR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
