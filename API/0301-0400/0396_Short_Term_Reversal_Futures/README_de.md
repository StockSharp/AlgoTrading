# Kurzfristige Umkehr-Strategie für Futures
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Kurzfristige Umkehr für Futures** sucht nach Mean Reversion bei Futures-Kontrakten. Täglich werden die Kontrakte mit der schlechtesten Rendite der vergangenen Woche identifiziert und gekauft, während die am stärksten gestiegenen Kontrakte verkauft werden, in Erwartung einer Gegenbewegung.

Trades werden einige Tage gehalten, bevor sie beim nächsten Signal geschlossen werden.

## Details
- **Einstiegskriterien**: Tägliches Ranking nach der Eins-Wochen-Rendite.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Positionen nach einer kurzen Halteperiode oder bei Ranking-Update geschlossen.
- **Stops**: Volatilitätsbasierter Stop kann angewendet werden.
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
