# InstantaneousTrendFilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet John Ehlers' Instantaneous Trendline und eine Triggerlinie, um Signale auf jedem Zeitrahmen zu erzeugen. Der Trigger wird als `2 * ITrend - ITrend[2]` berechnet, und bildet eine schnelle Linie, die die langsamere Trendlinie kreuzt. Ein Abwärtskreuz schließt Short-Positionen und öffnet eine Long-Position, während ein Aufwärtskreuz Long-Positionen schließt und eine Short-Position öffnet. Der Glättungsfaktor `Alpha` steuert die Reaktionsfähigkeit: Niedrigere Werte erzeugen glattere Linien, höhere Werte reagieren schneller.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Trigger lag auf dem vorherigen Balken über der Trendlinie und kreuzt auf dem aktuellen Balken darunter.
  - **Short**: Der Trigger lag auf dem vorherigen Balken unter der Trendlinie und kreuzt auf dem aktuellen Balken darüber.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Long-Positionen werden bei einem Short-Signal geschlossen.
  - Short-Positionen werden bei einem Long-Signal geschlossen.
- **Stops**: Standardmäßig keine.
- **Standardwerte**:
  - `Alpha` = 0.07.
  - `Candle Type` = 4-Stunden-Zeitrahmen.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
