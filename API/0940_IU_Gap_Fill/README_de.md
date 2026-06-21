# IU Gap-Fill-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die IU Gap Fill Strategie eröffnet Trades, wenn der Preis eine Lücke zum Schlusskurs der vorherigen Session bildet und diese Lücke dann schließt. Eine Long-Position wird eröffnet, wenn nach einem Gap-up der Preis unter den vorherigen Schlusskurs fällt und wieder darüber schließt. Eine Short-Position wird eröffnet, wenn nach einem Gap-down der Preis über den vorherigen Schlusskurs steigt und wieder darunter schließt. Ein ATR-basierter Trailing-Stop verwaltet die Ausstiege.

## Details
- **Daten**: Kerzen aus einem benutzerdefinierten Zeitrahmen.
- **Einstiegskriterien**:
  - **Long**: Gap-up von mindestens `GapPercent` und Preis kreuzt über den vorherigen Session-Schlusskurs.
  - **Short**: Gap-down von mindestens `GapPercent` und Preis kreuzt unter den vorherigen Session-Schlusskurs.
- **Ausstiegskriterien**: ATR-Trailing-Stop.
- **Stops**: ATR `AtrLength` * `AtrFactor` Trailing-Niveau.
- **Standardwerte**:
  - `CandleType` = 1m
  - `GapPercent` = 0.2
  - `AtrLength` = 14
  - `AtrFactor` = 2
- **Filter**:
  - Kategorie: Gap
  - Richtung: Long & Short
  - Indikatoren: ATR
  - Komplexität: Niedrig
  - Risikolevel: Mittel
