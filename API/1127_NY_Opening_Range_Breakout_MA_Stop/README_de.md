# NY Eröffnungsbereich-Ausbruch - MA-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Ausbrüche aus dem New Yorker Eröffnungsbereich 9:30-9:45 mit optionalen gleitenden Durchschnitts-Ausstiegen. Einstiege erfolgen auf der Kerze nach dem Ausbruch, wenn diese innerhalb der Cutoff-Zeit liegt und der Preis den MA-Filter erfüllt.

## Details

- **Einstiegskriterien**:
  - Vorherige Kerze schließt jenseits des Eröffnungsbereich-Hochs (Long) oder -Tiefs (Short) vor der Cutoff-Zeit.
  - Aktuelle Kerze ist die erste nach dem Ausbruch und erfüllt den MA-Filter wenn aktiviert.
- **Long/Short**: Konfigurierbar über `TradeDirection`.
- **Ausstiegskriterien**:
  - Stop auf der gegenüberliegenden Seite des Eröffnungsbereichs.
  - Take-Profit gemäß `TakeProfitType`: festes Risiko-Ertrags-Verhältnis, gleitender Durchschnitt-Kreuzung oder beides.
- **Stops**: Ja, an Bereichsgrenzen.
- **Standardwerte**:
  - `CutoffHour` = 12
  - `CutoffMinute` = 0
  - `TradeDirection` = LongOnly
  - `TakeProfitType` = FixedRiskReward
  - `TpRatio` = 2.5
  - `MaType` = SMA
  - `MaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Konfigurierbar
  - Indikatoren: Moving Average
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
