# PFE Extremwerte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche des Polarized Fractal Efficiency (PFE)-Indikators. Wenn PFE über das obere Level steigt, schließt die Strategie Short-Positionen und eröffnet eine Long-Position. Wenn PFE unter das untere Level fällt, werden Long-Positionen geschlossen und eine Short-Position eröffnet.

Der PFE-Indikator bewertet, wie effizient sich der Preis relativ zu seinem Pfad bewegt. Werte nahe +1 deuten auf eine starke Aufwärtsbewegung hin, während Werte nahe -1 eine starke Abwärtsbewegung anzeigen. Schwellenwertkreuzungen können den Beginn eines neuen Trends signalisieren.

## Details

- **Einstiegskriterien**: PFE kreuzt über `UpLevel` für Long oder unter `DownLevel` für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Ausbruch auf dem gegenüberliegenden Level oder Umkehrsignal.
- **Stops**: Standardmäßig nicht verwendet; können über Positionsschutz hinzugefügt werden.
- **Standardwerte**:
  - `PfePeriod` = 5
  - `UpLevel` = 0.5
  - `DownLevel` = -0.5
  - `CandleType` = 4-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: PFE
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
