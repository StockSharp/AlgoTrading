# ESG-Faktor-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie rotiert innerhalb eines Universums von Wertpapieren, die nach Umwelt-, Sozial- und Governance-Kriterien bewertet werden. Zu Beginn jedes Monats werden alle Symbole nach ihrer zurückliegenden Rendite eingestuft, und nur der stärkste Performer wird gehalten. Die Annahme ist, dass Vermögenswerte, die ESG-Kapital anziehen, ihren Momentum aufrechterhalten. Um übermäßigen Umschlag zu vermeiden, handelt der Algorithmus nur, wenn der Positionswert einen Mindestdollar-Schwellenwert überschreitet.

Beim Rebalancing schließt das System alle bestehenden Positionen und allokiert in das Wertpapier mit dem höchsten Momentum um. Das Portfolio nutzt weder Hebel noch Leerverkäufe; es ist vollständig in einem einzigen Vermögenswert investiert, der nach Momentum-Stärke ausgewählt wird.

## Details

- **Einstiegskriterien**:
  - Am ersten Handelstag des Monats die Gesamtrendite über `LookbackDays` für jedes Wertpapier berechnen.
  - Das Wertpapier mit der höchsten Rendite kaufen, wenn die Ordergröße mindestens `MinTradeUsd` beträgt.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Alle Positionen werden bei jedem monatlichen Rebalancing geschlossen, bevor die neue Position eröffnet wird.
- **Stops**: Keine.
- **Standardwerte**:
  - `Universe` – Liste ESG-fokussierter Symbole.
  - `LookbackDays` = 252.
  - `CandleType` = 1 Tag.
  - `MinTradeUsd` – Mindesttransaktionswert.
- **Filter**:
  - Kategorie: Momentum.
  - Richtung: Nur Long.
  - Zeitrahmen: Mittelfristig.
  - Rebalancing: Monatlich.

