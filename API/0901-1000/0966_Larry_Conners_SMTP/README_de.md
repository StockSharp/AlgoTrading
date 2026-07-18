# Larry Conners SMTP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine nur-Long-Strategie, die nach einem 10-Balken-Tief kauft, wenn der aktuelle Balken die größte Range der letzten 10 Balken hat und im oberen 25% seiner Range schließt. Der Einstieg erfolgt einen Tick über dem Hoch; der Stop-Loss folgt aufeinanderfolgenden Tiefs.

## Details

- **Einstiegskriterien**:
  - **Long**: Das aktuelle Tief ist gleich dem niedrigsten Wert der letzten 10 Balken, die heutige Range ist die größte der letzten 10 und der Schlusskurs liegt im oberen 25% der Range; Buy-Stop bei `High + TickSize` platzieren.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Trailing-Stop am höchsten Tief seit Einstieg.
- **Stops**: Ja.
- **Standardwerte**:
  - `TickSize` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Long
  - Indikatoren: Highest, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
