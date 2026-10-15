# Random Bias Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Random Bias Trader-Strategie emuliert den Expertenberater „Random Trader“ von MetaTrader unter Verwendung des High-Level-API von StockSharp.
Bei jeder abgeschlossenen Kerze wirft die Strategie eine virtuelle Münze und eröffnet eine Position in diese Richtung, wenn kein Handel aktiv ist.
Stop-Loss- und Take-Profit-Level werden entweder von ATR(10) oder von einem festen Pip-Abstand abgeleitet und anhand des Chancen-Risiko-Verhältnisses dimensioniert.
Die Positionsgröße wird aus dem konfigurierten Risikoprozentsatz berechnet und automatisch durch die Volumenlimits des Instruments begrenzt.
Ein optionaler Breakeven-Trigger kann den Stop-Loss auf den Einstiegspreis verschieben, sobald ein bestimmter Pip-Gewinn erreicht ist.

## Einzelheiten
- **Daten**: Ein Kerzenabonnement, definiert durch `CandleType`.
- **Eintrittskriterien**:
  - Long: Keine offene Position, Münzwurf ergibt Long. Der Einstiegspreis entspricht dem letzten Schlusskurs.
  - Short: Keine offene Position, Münzwurf ergibt Short. Der Einstiegspreis entspricht dem letzten Schlusskurs.
- **Ausstiegskriterien**:
  - Stop-Loss: Die Distanz entspricht `LossPipDistance` × Pip-Größe oder `LossAtrMultiplier` × ATR(10), abhängig von `LossType`.
  - Take-Profit: Stoppdistanz multipliziert mit `RewardRiskRatio`.
  - Breakeven: Wenn aktiviert, wird der Stopp nach `BreakevenDistancePips` Gewinn zum Einstieg verschoben.
- **Stops**: Dynamischer Stop-Loss und Take-Profit pro Trade, Breakeven-Stopp optional.
- **Standardwerte**:
  - `CandleType` = 1 Minute Zeitrahmen
  - `RewardRiskRatio` = 2,0
  - `LossType` = Pip
  - `LossAtrMultiplier` = 5,0
  - `LossPipDistance` = 20 Pips
  - `RiskPercentPerTrade` = 1 %
  - `UseBreakeven` = Aktiviert
  - `BreakevenDistancePips` = 10 Pips
  - `UseMaxMargin` = Aktiviert
- **Filter**:
  - Kategorie: Randomisiert trendneutral
  - Richtung: Beide, pro Wurf bestimmt
  - Indikatoren: ATR(10) (optional)
  - Komplexität: Anfänger
  - Risikostufe: Mittel, abhängig von der Stoppgröße

## Notizen
- Wenn das risikobasierte Volumen zu klein wird, greift die Strategie optional auf das maximal handelbare Volumen zurück.
- Stop- und Zielniveaus werden vor der Auftragserteilung auf die Preisstufe des Instruments gerundet.
- Die Breakeven-Logik hält jeweils nur eine Position offen und spiegelt die ursprüngliche MetaTrader-Logik wider.
