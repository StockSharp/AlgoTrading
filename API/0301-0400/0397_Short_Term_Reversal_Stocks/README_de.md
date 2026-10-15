# Kurzfristige Umkehr-Strategie für Aktien
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Kurzfristige Umkehr für Aktien** wendet Mean-Reversion-Prinzipien auf Aktien an. Täglich werden die Aktien mit den größten Verlusten der vergangenen Woche gekauft, während die jüngsten Gewinner geshortet werden, in der Erwartung einer kurzlebigen Umkehr.

Positionen werden nur einige Tage gehalten, bevor sie neu bewertet werden.

## Details
- **Einstiegskriterien**: Tägliches Ranking nach der Eins-Wochen-Rendite.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Positionen nach mehreren Tagen oder bei Ranking-Update geschlossen.
- **Stops**: Volatilitätsbasierter Stop kann verwendet werden.
- **Standardwerte**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Preisbasiert
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
