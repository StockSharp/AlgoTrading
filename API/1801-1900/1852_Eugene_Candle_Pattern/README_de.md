# Eugene Kerzenmuster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die ein von "Eugene" beschriebenes Kerzenmuster handelt. Der Algorithmus analysiert die letzten vier Kerzen, prüft auf Innenstäbe und spezielle "Vogel"-Formationen und berechnet Ausbruchsniveaus. Positionen werden bei Ausbrüchen aus den Extrema der vorherigen Kerze eröffnet, wenn zusätzliche Bestätigungsbedingungen erfüllt sind. Optionale Stop-Loss- und Take-Profit-Niveaus werden in Preisschritten ausgedrückt.

## Details

- **Einstiegskriterien**:
  - Long: aktuelles Hoch über vorherigem Hoch, vorheriges Tief unter früherem Hoch, aktuelles Tief über vorherigem Tief und Bestätigung durch Zig-Niveau oder Zeitfilter.
  - Short: aktuelles Tief unter vorherigem Tief, vorheriges Hoch über früherem Tief, aktuelles Hoch unter vorherigem Hoch und Bestätigung durch Zig-Niveau oder Zeitfilter.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: verkaufen wenn ein entgegengesetztes Signal erscheint oder Stop-Loss/Take-Profit erreicht wird.
  - Short: kaufen wenn ein entgegengesetztes Signal erscheint oder Stop-Loss/Take-Profit erreicht wird.
- **Stops**: fester Abstand in Preisschritten
- **Standardwerte**:
  - `Volume` = 1m
  - `StopLossPoints` = 0
  - `TakeProfitPoints` = 0
  - `InvertSignals` = false
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Optional
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Intraday (Stunde >= 8 Filter)
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
