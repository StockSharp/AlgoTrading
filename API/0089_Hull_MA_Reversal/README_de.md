# Hull MA-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Hull Moving Average reagiert schnell auf Preisänderungen und bleibt dabei glatt. Eine Änderung seiner Richtung kann eine kurzfristige Umkehr ankündigen. Diese Strategie überwacht aufeinanderfolgende Hull MA-Werte und handelt, wenn die Steigung wechselt.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 154%. Am besten funktioniert die Strategie am Aktienmarkt.

Wenn der gleitende Durchschnitt von fallend auf steigend wechselt, wird eine Long-Position eröffnet. Ein Wechsel von steigend auf fallend initiiert einen Short. Das Risiko wird mit einem ATR-basierten Stop kontrolliert, der jenseits der letzten Kerze platziert wird.

Ausstiege verlassen sich auf diesen Schutz-Stop und erfassen einen Teil der Bewegung, die auf den von Hull MA hervorgehobenen Schwungwechsel folgt.

## Details

- **Einstiegskriterien**: Hull MA-Steigung ändert die Richtung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `HmaPeriod` = 9
  - `AtrMultiplier` = 2 ATR
  - `CandleType` = 15 minute
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Hull MA, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

