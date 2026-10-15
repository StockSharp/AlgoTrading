# PZ Umkehr-Trendfolge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie folgt Ausbrüchen aus langfristigen Hochs und Tiefs. Sie kauft, wenn der Schlusskurs das höchste Hoch des Rückblickzeitraums überschreitet, und verkauft leer, wenn der Schlusskurs unter das niedrigste Tief fällt. Die Position wird bei entgegengesetzten Signalen immer umgekehrt, sodass die Strategie kontinuierlich im Markt bleibt.

Der Ansatz versucht, anhaltende Trends zu erfassen, indem nach einem bedeutenden Ausbruch eingestiegen wird. Da das System nur bei wichtigen Extremen handelt, kann es kleinere Geräusche vermeiden, aber in trendlosen Marktphasen erhebliche Drawdowns erleiden.

## Details

- **Einstiegskriterien**: Ausbruch aus dem Hoch/Tief der vorherigen `Period` Balken.
- **Long/Short**: Beide Richtungen, immer im Markt.
- **Ausstiegskriterien**: Entgegengesetztes Ausbruchssignal.
- **Stops**: Nein
- **Standardwerte**:
  - `Period` = 100
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
