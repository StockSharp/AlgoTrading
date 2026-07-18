# Martini Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein abgesichertes Martingale-Grid. Sie beginnt damit, Stop-Orders auf beiden Seiten des aktuellen Kurses zu platzieren und verdoppelt die Positionsgröße in der entgegengesetzten Richtung, wenn sich der Markt um einen festgelegten Schritt gegen die aktuelle Positionierung bewegt. Alle Trades werden geschlossen, sobald der angesammelte Gewinn das Ziel überschreitet.

## Details

- **Einstiegskriterien**:
  - Buy-Stop oberhalb und Sell-Stop unterhalb des Marktes im Abstand `Step` platzieren.
  - Wenn eine Order ausgelöst wird, den entgegengesetzten Stop stornieren.
- **Positionsverwaltung**:
  - Den Kurs der zuletzt ausgeführten Order verfolgen.
  - Wenn sich der Kurs um `Step * orderCount` gegen die offene Position bewegt, eine Marktorder in der entgegengesetzten Richtung mit doppeltem Volumen senden.
- **Ausstiegskriterien**:
  - Alle Positionen schließen, wenn der unrealisierte Gewinn `ProfitClose` erreicht.
- **Long/Short**: Beide.
- **Stops**: Verwendet Stop-Orders für erste Einstiege; kein Stop-Loss.
- **Indikatoren**: Keine.
- **Filter**: Keine.

### Parameter

- `Step` – Kursschritt in absoluten Einheiten.
- `ProfitClose` – Gewinnschwelle zum Schließen aller Trades.
- `InitialVolume` – Startvolumen für die erste Order.
- `CandleType` – Kerzenserie für Kursaktualisierungen.
