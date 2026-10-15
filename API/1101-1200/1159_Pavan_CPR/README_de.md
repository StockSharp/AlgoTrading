# Pavan CPR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht Long, wenn der Kurs die obere Central Pivot Range des Tages nach oben kreuzt, nachdem er zuvor darunter geschlossen hatte. Der Stop wird auf dem Pivot-Niveau platziert und der Take Profit in einem festen Abstand.

## Details

- **Einstiegskriterien**: Vorheriger Schlusskurs unter dem oberen CPR und aktueller Schlusskurs darüber.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Take Profit oder Stop am Pivot.
- **Stops**: Ja.
- **Standardwerte**:
  - `TakeProfitTarget` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long
  - Indikatoren: Pivot
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
