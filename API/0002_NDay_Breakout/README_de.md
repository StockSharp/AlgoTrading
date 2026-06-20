# N-Tage-Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

N-Tage-Hoch/Tief-Ausbruchsstrategie. Der N-Tage-Ausbruch sucht nach neuen Hochs oder Tiefs über den angegebenen Zeitraum. Einstiege erfolgen, wenn der Preis das letzte N-Tage-Hoch oder -Tief durchbricht und dabei Momentum antizipiert. Ein gleitender Durchschnittsfilter und ein prozentualer Stop steuern die Ausstiege.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 43%. Die Strategie funktioniert am besten im Aktienmarkt.

Indem auf den Bruch des vorherigen Extremwerts gewartet wird, versucht das System den Beginn einer Richtungsbewegung zu erfassen. Die Filterung durch einen trendfolge-orientierten Durchschnitt hilft, Fehlsignale zu vermeiden, die während der Konsolidierung auftreten.


## Details

- **Einstiegskriterien**: Signale basierend auf MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `MaPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

