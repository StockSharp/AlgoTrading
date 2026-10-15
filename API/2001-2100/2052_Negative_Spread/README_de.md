# Negativer-Spread-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Negative Spread-Strategie nutzt seltene Momente aus, wenn der beste Briefkurs unter den besten Geldkurs fällt und so einen negativen Spread erzeugt.
Wenn diese Fehlbewertung auftritt, verkauft die Strategie zum Marktpreis und versucht, den abnormalen Spread zu erfassen.
Nachdem die Short-Position eröffnet wurde, wird sie beim nächsten Orderbuch-Update geschlossen, sobald der Markt in einen normalen Zustand zurückkehrt.

Das System hört ausschließlich auf Orderbuch-Ereignisse und verlässt sich nicht auf Kerzen oder Indikatoren.
Optionale Stop-Loss- und Take-Profit-Parameter werden als Sicherheitsmaßnahmen bereitgestellt und werden in Pips unter Verwendung der Tick-Größe des Instruments berechnet.

## Details
- **Einstiegskriterien**: `BestAsk < BestBid` und keine aktive Position.
- **Long/Short**: Nur Short.
- **Ausstiegskriterien**: Die Position wird unmittelbar nach der Eröffnung geschlossen.
- **Stops**: Optionaler Stop-Loss und Take-Profit in Pips.
- **Standardwerte**:
  - `Volume` = 1
  - `TakeProfitPips` = 5000
  - `StopLossPips` = 5000
- **Filter**:
  - Kategorie: Arbitrage
  - Richtung: Short
  - Indikatoren: Keine
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Tick
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
