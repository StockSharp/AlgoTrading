# JSatl Digit-System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das JSatl Digit-System verwendet einen Jurik Moving Average (JMA), um die Trendrichtung zu bestimmen.
Die Strategie misst die Steigung des JMA und eröffnet eine Position, wenn der Kurs die Steigungsrichtung bestätigt.

Eine Long-Position wird eröffnet, wenn der JMA steigt und der Schlusskurs über dem Durchschnitt liegt.
Eine Short-Position wird eröffnet, wenn der JMA fällt und der Schlusskurs unter dem Durchschnitt liegt.
Gegensätzliche Signale schließen jede offene Position.

## Details

- **Einstiegskriterien**: JMA-Steigung mit Kursbestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `JmaLength` = 14
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: JMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing (4h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
