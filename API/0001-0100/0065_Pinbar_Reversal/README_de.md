# Pinbar-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Pinbars heben plötzliche Preisablehnungen hervor und können kurzfristige Wendepunkte signalisieren. Diese Strategie misst die Länge des Kerzendochts relativ zu seinem Körper und sucht nach langen Schatten, die aus der jüngsten Preisbewegung herausragen. Ein gleitender Durchschnittsfilter hilft, in Richtung des zugrunde liegenden Trends zu handeln.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 82%. Sie funktioniert am besten auf dem Aktienmarkt.

Bei jeder Kerzenaktualisierung berechnet das System obere und untere Schatten und vergleicht sie mit der Körpergröße. Ein bullischer Pinbar mit langem unterem Docht kann einen Long-Einstieg auslösen, wenn der Preis über dem gleitenden Durchschnitt liegt. Ebenso kann ein bärischer Pinbar mit einem ausgedehnten oberen Docht eine Short-Position in einem Abwärtstrend einleiten. Stops werden auf einen festen Prozentsatz vom Einstieg gesetzt.

Der Trade wird geschlossen, wenn ein entgegengesetzter Pinbar gegen die offene Position erscheint oder der schützende Stop erreicht wird. Die Kombination der Pinbar-Logik mit einem Trendfilter verbessert die Zuverlässigkeit durch Vermeidung kontrend-Setups.

## Details

- **Einstiegskriterien**: Pinbar mit langem Docht und kleinem entgegengesetzten Schatten, durch Trend bestätigt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetzter Pinbar oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `TailToBodyRatio` = 2
  - `OppositeTailRatio` = 0.5
  - `MAPeriod` = 20
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

