# NY Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Ausbrüche aus dem zwischen 13:00 und 13:30 UTC gebildeten Bereich. Nach Schließen des Zeitfensters steigt die Strategie ein, wenn der Preis das Sitzungshoch oder -tief durchbricht, mit einem Ziel von doppelter Bereichsgröße und Stop auf der gegenüberliegenden Seite.

## Details

- **Einstiegskriterien**:
  - Erste Kerze nach 13:30 UTC schließt über dem Sitzungshoch -> Long.
  - Erste Kerze nach 13:30 UTC schließt unter dem Sitzungstief -> Short.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Gewinnziel bei `RewardRisk`-facher Bereichsgröße.
  - Stop an der gegenüberliegenden Bereichsgrenze.
- **Stops**: Ja.
- **Standardwerte**:
  - `RewardRisk` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
