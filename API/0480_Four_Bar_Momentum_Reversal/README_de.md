# Vier-Balken-Momentum-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Vier-Balken-Momentum-Umkehr-Strategie geht long, wenn der Schlusskurs innerhalb des ausgewählten Zeitfensters mindestens `BuyThreshold` aufeinanderfolgende Kerzen lang unter dem Schlusskurs von vor `Lookback` Balken gelegen hat. Die Position wird geschlossen, sobald der Preis über das Hoch der vorherigen Kerze bricht.

## Details

- **Einstiegskriterien**: `BuyThreshold` aufeinanderfolgende Schlusskurse unter dem Schlusskurs von vor `Lookback` Balken innerhalb des Zeitfensters.
- **Ausstiegskriterien**: Schlusskurs größer als das Hoch der vorherigen Kerze.
- **Stops**: Keine.
- **Standardwerte**:
  - `BuyThreshold` = 4
  - `Lookback` = 4
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
