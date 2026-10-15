# Eröffnungsbereich-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Eröffnungsbereich-Ausbruch-Strategie verfolgt die höchsten und niedrigsten Preise während der ersten Minuten einer Handelssitzung. Nach dem Ende des Bereichs werden Ausbruchsorders jenseits des Bereichs mit einem konfigurierbaren Puffer platziert. Ziele werden aus einem Gewinn-Risiko-Verhältnis abgeleitet, während Stops auf der gegenüberliegenden Seite des Bereichs gesetzt werden.

## Details

- **Einstiegskriterien**:
  - Nach dem Eröffnungsbereich Long gehen, wenn der Preis über dem Hoch plus Puffer schließt.
  - Short gehen, wenn der Preis unter dem Tief minus Puffer schließt.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Stop und Ziel basierend auf Bereich und Gewinn-Risiko-Verhältnis.
- **Stops**: Ja
- **Standardwerte**:
  - `RangeMinutes` = 15
  - `RewardRisk` = 2.0
  - `EntryBuffer` = 0.0001
  - `SessionStart` = 08:00
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
