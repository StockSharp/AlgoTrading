# Bot für Spot-Markt - Benutzerdefiniertes Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Bot-für-Spot-Markt-Strategie mit benutzerdefiniertem Grid kauft eine Anfangsposition und fügt neue Aufträge hinzu, wenn der Preis um einen bestimmten Prozentsatz unter den letzten Einstieg fällt. Alle Positionen werden geschlossen, wenn der Preis das Gewinnziel über dem durchschnittlichen Einstiegspreis überschreitet.

## Details

- **Einstiegskriterien**:
  - Kauf zum Startzeitpunkt.
  - Zusätzliche Menge kaufen, wenn der Preis `NextEntryPercent`% unter den letzten Einstieg fällt.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Alle Positionen schließen, wenn der Preis den durchschnittlichen Einstiegspreis um `ProfitPercent`% übersteigt und die offene Position profitabel ist.
- **Stops**: Keine.
- **Standardwerte**:
  - `OrderValue` = 10
  - `MinAmountMovement` = 0.00001
  - `Rounding` = 5
  - `NextEntryPercent` = 0.5
  - `ProfitPercent` = 2
- **Filter**:
  - Kategorie: Grid-Trading
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
