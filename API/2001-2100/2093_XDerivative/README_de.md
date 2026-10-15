# XDerivative-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die XDerivative-Strategie verfolgt Verschiebungen im Preismomentum mithilfe einer geglätteten Änderungsrate. Der originale MQL-Experte kombiniert eine Änderungsraten-Berechnung mit Jurik-Glättung zur Erkennung von Wendepunkten. Die StockSharp-Version nutzt integrierte Indikatoren, um dasselbe Konzept umzusetzen.

Die Strategie berechnet die Änderungsrate über `RocPeriod` Balken und glättet sie mit einem Jurik Moving Average der Länge `MaLength`. Wenn die geglättete Ableitung ein Tal bildet (der vorherige Wert ist niedriger als sein Vorgänger und der aktuelle Wert steigt über den vorherigen), eröffnet die Strategie eine Long-Position oder wechselt dazu. Wenn ein Gipfel entsteht (der vorherige Wert ist höher als sein Vorgänger und der aktuelle fällt darunter), eröffnet die Strategie eine Short-Position oder wechselt dazu. Schutzstopps verwalten die Ausstiege.

## Details

- **Einstiegskriterien**:
  - Long: Geglättete Ableitung dreht nach oben nach einem lokalen Minimum.
  - Short: Geglättete Ableitung dreht nach unten nach einem lokalen Maximum.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Ableitungswechsel oder Schutzstop.
- **Stops**: Ja, prozentuale Take-Profit und Stop-Loss.
- **Standardwerte**:
  - `RocPeriod` = 34
  - `MaLength` = 7
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: RateOfChange, JurikMovingAverage
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: 4H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
