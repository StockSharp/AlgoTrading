# Strategie zur IBS-Umkehr im durchschnittlichen Hoch-Tief-Bereich
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach Mean Reversion, nachdem der Preis unterhalb einer dynamischen Schwelle geblieben ist, die aus der durchschnittlichen Hoch-Tief-Spanne abgeleitet wird. Sie berechnet den gleitenden Durchschnitt der Balkenspanne, das höchste Hoch und das niedrigste Tief über den Beobachtungszeitraum. Eine Kaufschwelle wird als höchstes Hoch minus 2,5-facher Durchschnittsspanne definiert. Wenn der Preis für eine bestimmte Anzahl von Balken unter diesem Niveau bleibt und die intrabarbezeichnete Stärke (IBS) innerhalb des Handelsfensters unter einem bestimmten Limit liegt, wird eine Long-Position eröffnet. Die Position wird geschlossen, wenn der Schlusskurs das Hoch des vorherigen Balkens überschreitet.

## Details

- **Einstiegskriterien**:
  - Der Preis ist `BarsBelowThreshold` Balken lang unter der Kaufschwelle geblieben.
  - IBS < `IbsBuyThreshold`.
  - Zeit zwischen `StartTime` und `EndTime`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Schlusskurs überschreitet das Hoch des vorherigen Balkens.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 20
  - `BarsBelowThreshold` = 2
  - `IbsBuyThreshold` = 0.2
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: SMA, Highest, Lowest
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
