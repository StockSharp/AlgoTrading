# Lucky Jump-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Lucky Jump-Strategie ist ein kurzfristiges Mean-Reversion-System, das auf plötzliche Preissprünge beim besten Bid und Ask reagiert. Wenn der Ask-Preis gegenüber der vorherigen Notierung um eine bestimmte Anzahl von Punkten nach oben springt, eröffnet die Strategie eine Short-Position in Erwartung eines Rückschlags. Umgekehrt geht sie long, wenn der Bid-Preis um denselben Betrag fällt. Positionen werden entweder beim ersten günstigen Tick oder wenn der Verlust einen vordefinierten Grenzwert überschreitet, geschlossen.

Dieser Ansatz versucht, schnelle Korrekturen nach aggressiven Marktbewegungen zu erfassen. Er arbeitet ausschließlich mit Level1-Kursdaten und ist nicht auf Kerzen oder Indikatoren angewiesen.

## Details

- **Einstiegskriterien**:
  - **Short**: `Ask(t) - Ask(t-1) >= Shift * PriceStep`.
  - **Long**: `Bid(t-1) - Bid(t) >= Shift * PriceStep`.
- **Ausstiegskriterien**:
  - Position schließen, sobald sie profitabel wird.
  - Schließen, wenn der Verlust `Limit * PriceStep` überschreitet.
- **Stops**: impliziter Stop basierend auf dem `Limit`-Parameter.
- **Standardwerte**:
  - `Shift` = 30 Punkte.
  - `Limit` = 180 Punkte.
  - `Volume` = 1.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Ultra-kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch

