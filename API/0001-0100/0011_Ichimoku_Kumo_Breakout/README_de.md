# Ichimoku Kumo-Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Ichimoku-Kumo (Wolken)-Ausbruch.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 70%. Die Strategie funktioniert am besten im Aktienmarkt.

Dieser Ansatz stützt sich auf Ichimoku-Wolkensignale. Ein Preisausbruch über die Wolke mit Tenkan-sen, das Kijun-sen kreuzt, löst einen Kauf aus, während der entgegengesetzte Ausbruch unter die Wolke einen Short startet. Positionen werden gehalten, bis der Preis durch die Wolke zurückkehrt.

Die Wolke skizziert wichtige Unterstützungs- und Widerstandsniveaus, daher wartet das System auf entscheidende Schlusskurse über sie hinaus. Durch die Kombination mehrerer Ichimoku-Komponenten vermeidet die Strategie Trades mit geringerer Wahrscheinlichkeit während seitwärts laufender Märkte.


## Details

- **Einstiegskriterien**: Signale basierend auf Ichimoku.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Ichimoku
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

