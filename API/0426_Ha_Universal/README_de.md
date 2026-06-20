# Universelle Heikin Ashi-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese universelle Vorlage konvertiert Standardkerzen in Heikin Ashi-Kerzen und handelt in Richtung ihres Körpers. Die Methode glättet Preisrauschen, sodass Trends klarer sichtbar werden. Sie ist leichtgewichtig und kann als Basis für benutzerdefinierte Filter oder Ausstiege dienen.

Das System steigt long ein, wenn der Heikin Ashi-Schlusskurs über seinem Eröffnungskurs liegt, und wechselt short, wenn der Schlusskurs unter den Eröffnungskurs fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: `HA_Close > HA_Open`
  - **Short**: `HA_Close < HA_Open`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `CandleType` = 1 minute
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Heikin Ashi
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
