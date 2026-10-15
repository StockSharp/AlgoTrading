# Order Example-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie, konvertiert aus dem MQL5-Beispiel `OrderExample.mq5`.
Sie geht Trades ein, wenn der Kurs über aktuelle Hochs oder unter aktuelle Tiefs ausbricht.

Die Strategie verwendet die Indikatoren `Highest` und `Lowest`, um Ausbruchniveaus über ein konfigurierbares Fenster zu verfolgen.

## Details

- **Einstiegskriterien**:
  - Long: `Close` bricht über das höchste Hoch der letzten `Lookback` Kerzen aus
  - Short: `Close` bricht unter das niedrigste Tief der letzten `Lookback` Kerzen aus
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetzter Ausbruch
- **Stops**: Nein
- **Standardwerte**:
  - `Lookback` = 26
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
