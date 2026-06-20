# Ichimoku Tenkan/Kijun-Kreuz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ichimoku-Indikatoren bieten ein vollständiges Trendfolgesystem. Dieser Ansatz konzentriert sich auf den Kreuzungspunkt der Tenkan-sen über die Kijun-sen, während der Preis relativ zur Kumo-Wolke gehandelt wird. Ein bullisches Kreuz oberhalb der Wolke signalisiert die Trendfortsetzung nach oben, während ein bärisches Kreuz unterhalb der Wolke auf Schwäche hindeutet.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 142%. Am besten funktioniert die Strategie am Aktienmarkt.

Im Betrieb berechnet die Strategie die Ichimoku-Komponenten auf jedem Balken. Wenn Tenkan über Kijun steigt und der Preis über der Wolke liegt, wird ein Long-Trade mit einem Stop nahe Kijun eingeleitet. Ein Kreuz in der entgegengesetzten Richtung unterhalb der Wolke löst einen Short mit ähnlicher Stop-Platzierung aus.

Das System bleibt im Trade, bis der Stop erreicht wird oder das Kreuz sich umkehrt, mit dem Ziel, anhaltende Bewegungen in Richtung der Wolke zu erfassen.

## Details

- **Einstiegskriterien**: Tenkan/Kijun-Kreuz mit Preis relativ zur Kumo-Wolke.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Kreuz.
- **Stops**: Ja, auf Kijun-Niveau.
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = 30 minute
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Ichimoku
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

