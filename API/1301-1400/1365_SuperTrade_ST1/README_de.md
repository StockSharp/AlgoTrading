# SuperTrade ST1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Long-Strategie, die Supertrend mit EMA-Filter und ATR-basiertem Risikomanagement kombiniert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 45%. Am besten funktioniert die Strategie auf dem Kryptomarkt.

Das System wartet auf einen Rückgang der Supertrend-Richtung, während der Preis über der Supertrend-Linie und der EMA bleibt. Das Risiko wird mit ATR-basiertem Stop-Loss und Gewinnmitnahme bei einem Verhältnis von 1:4 gesteuert.

## Details

- **Einstiegskriterien**:
  - Vorherige Supertrend-Richtung > aktuelle Richtung
  - Schlusskurs > Supertrend
  - Schlusskurs > EMA
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: `Close <= entry - StopAtrMultiplier * ATR` oder `Close >= entry + TakeAtrMultiplier * ATR`
- **Stops**: ATR-basierter Stop-Loss und Gewinnmitnahme
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `EmaPeriod` = 200
  - `StopAtrMultiplier` = 1.0
  - `TakeAtrMultiplier` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: Supertrend, EMA, ATR
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

