# Adam and Eve-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die Heiken Ashi-Kerzen mit einer Kaskade einfacher gleitender Durchschnitte kombiniert. Eine Short-Position wird eröffnet, wenn eine bärische Heiken Ashi-Kerze ohne oberen Docht erscheint und alle überwachten gleitenden Durchschnitte (5, 7, 9, 10, 12, 14, 20) nach unten zeigen. Eine Long-Position wird durch eine bullische Kerze ohne unteren Docht und alle aufwärts zeigenden Durchschnitte ausgelöst. Jeder Trade zielt auf einen Gewinn in einem Abstand von einem ATR(14) vom Einstieg ohne Stop-Loss.

## Details

- **Einstiegskriterien**: vorherige Heiken Ashi-Kerze ohne oberen (Short) oder unteren (Long) Docht und ausgerichteter SMA-Stapel
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gewinnziel im ATR(14)-Abstand
- **Stops**: Keine
- **Standardwerte**:
  - `AtrPeriod` = 14
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA (5,7,9,10,12,14,20), Heiken Ashi, ATR
  - Stops: Nur Ziel
  - Komplexität: Mittel
  - Zeitrahmen: Konfigurierbar, Standard 15 Minuten
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
