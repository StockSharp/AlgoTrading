# AutoFib Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie zeichnet eine dynamische Fibonacci-Erweiterung vom jüngsten Swing-Hoch und -Tief und geht long, wenn der Preis in einem durch den 200er EMA definierten Aufwärtstrend über das 1.618-Niveau ausbricht. Das Risiko wird durch einen ATR-basierten Stop und ein ATR-basiertes Ziel verwaltet.

## Details

- **Einstiegskriterien**: Schluss über der 1.618-Fibonacci-Erweiterung und über EMA200.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: ATR-basierter Stop-Loss oder 3×ATR Take-Profit.
- **Stops**: Ja, basierend auf ATR.
- **Standardwerte**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `FibLevel` = 1.618
  - `PivotPeriod` = 10
  - `CandleType` = 5 Minuten
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long
  - Indikatoren: EMA, ATR, Highest, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
