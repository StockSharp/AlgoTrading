# Parabolic SAR Frühkauf-Strategie mit MA-basiertem Ausstieg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt den Parabolic SAR-Indikator, um Trades einzugehen, wenn der Indikator die Seite relativ zum Kurs wechselt. Ein einfacher gleitender Durchschnitt liefert eine zusätzliche Ausstiegsregel: Long-Positionen werden geschlossen, wenn der Kurs unter den gleitenden Durchschnitt fällt und der SAR über dem Kurs liegt.

## Details

- **Einstiegskriterien**: SAR wechselt die Seite relativ zum Kurs.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Bei Long-Positionen aussteigen, wenn SAR > Kurs und Kurs < MA.
- **Stops**: Nicht definiert.
- **Standardwerte**:
  - `Acceleration` = 0.02
  - `AccelerationStep` = 0.02
  - `MaxAcceleration` = 0.2
  - `MaPeriod` = 11
  - `CandleType` = 5 Minuten
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, SMA
  - Stops: Keine
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
