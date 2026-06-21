# Sea Dragon 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sea Dragon 2 ist eine hedgende Grid-Strategie, die Positionen in beide Richtungen eröffnet und neue Aufträge hinzufügt, wenn sich der Preis um einen benutzerdefinierten Schritt bewegt. Die Auftragsgrößen folgen einer vordefinierten Sequenz, und die Take-Profit-Niveaus passen sich abhängig vom Gleichgewicht zwischen Long- und Short-Exposure an.

## Details

- **Initiale Aufträge**: Eröffnet zu Beginn sowohl einen Kauf- als auch einen Verkaufsauftrag mit demselben Volumen.
- **Auftragsergänzung**: Wenn sich der Markt um *Step* Punkte vom letzten Auftragspreis bewegt, wird ein neues Auftragspaar hinzugefügt. Die Seite mit mehr Exposure erhält den größeren Auftrag gemäß der Sequenz.
- **Volumensequenz**: 1,1,2,3,6,9,14,22,33,48,82,111,122,164,185 skaliert durch *Volume Scale*.
- **Take Profit**:
  - Wenn Long- und Short-Anzahl gleich sind, verwendet jede Seite *Take Profit*.
  - Wenn eine Seite dominiert, verwendet diese Seite *Alt Take Profit*, während die andere *Take Profit* beibehält.
- **Stop Loss**: Jede Seite hat einen Stop, der *Max Stop* Punkte von ihrem Durchschnittspreis entfernt platziert wird.
- **Datenquelle**: Die Strategie arbeitet auf abgeschlossenen Kerzen des Typs *Candle Type*.
- **Long/Short**: Beide, abgesichert.
- **Ausstieg**: Positionen schließen, wenn der Preis Take-Profit- oder Stop-Niveaus erreicht.
