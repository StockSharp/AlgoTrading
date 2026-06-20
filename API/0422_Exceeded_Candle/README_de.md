# Exceeded Candle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser musterbasierte Ansatz sucht nach bullischen Engulfing-Kerzen, die den vorherigen Balken übersteigen, während der Preis noch unterhalb der mittleren Bollinger-Bande liegt. Die Idee ist, dass eine starke Umkehr innerhalb eines Rückgangs den Preis zurück zur oberen Bande treiben kann. Die Strategie handelt nur Long und bricht Einstiege ab, wenn drei aufeinanderfolgende bearishe Kerzen erscheinen.

Sobald der Preis die obere Bollinger-Bande berührt, wird die Position geschlossen und der schnelle Rückprall erfasst. Die Methode eignet sich für kurze Zeitrahmen, bei denen Volatilitätsbänder Mean-Reversion-Schwingungen erfassen.

## Details

- **Einstiegskriterien**:
  - **Long**: vorherige Kerze rot, aktuelle Kerze grün und schließt über dem vorherigen Eröffnungskurs, `Close < MiddleBand`, keine drei aufeinanderfolgenden roten Kerzen
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - **Long**: `Close > UpperBand`
- **Stops**: Keine
- **Standardwerte**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: Bollinger Bands, price action
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
