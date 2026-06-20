# Volume Spike Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Volume Spike Trend überwacht plötzliche Anstiege im gehandelten Volumen. Wenn das aktuelle Volumen den jüngsten Durchschnitt um einen festgelegten Multiplikator überschreitet, signalisiert es eine starke Marktbeteiligung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 175%. Am besten funktioniert es im Aktienmarkt.

Wenn das Volumen spikt und der Preis über dem gleitenden Durchschnitt liegt, kauft die Strategie; wenn das Volumen bei einem Preis unter dem Durchschnitt spikt, wird eine Short-Position eröffnet. Trades enden, wenn das Volumen wieder unter den Durchschnitt fällt oder der Stop-Loss erreicht wird.

Diese Methode versucht, Bewegungen zu erfassen, die durch einen Aktivitätsausbruch angetrieben werden.

## Details

- **Einstiegskriterien**: Volumenänderung überschreitet `VolumeSpikeMultiplier` mal den Durchschnitt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Volumen fällt unter den Durchschnitt oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `VolAvgPeriod` = 20
  - `VolumeSpikeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Volumen, MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

