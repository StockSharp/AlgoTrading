# XOSignal Wiedereinstiegs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader-Experten *Exp_XOSignal_ReOpen* innerhalb von StockSharp unter Verwendung der High-Level-API. Sie handelt Kerzendaten des ausgewählten Symbols und Zeitrahmens mit einem XO-Stil-Ausbruchsdetektor, der auf ATR(13) aufgebaut ist. Wenn ein Aufwärtspfeil erscheint, schließt der Algorithmus Shorts, öffnet optional einen Long, und fügt dann jedes Mal zur Position hinzu, wenn der Preis um eine feste Anzahl von Ticks fortschreitet. Abwärtspfeile verhalten sich symmetrisch für Shorts. Harte Stops und Targets in Ticks werden auf jede Schicht der Pyramide angewendet.

## Kernlogik

- Die Strategie berechnet einen XO-Bereichskanal, dessen Bänder sich um `Range * PriceStep` erweitern. Ausbrüche setzen die Bänder zurück und legen die aktuelle Trendrichtung fest.
- ATR(13) steuert, wie weit unterhalb/oberhalb der Kerze die virtuellen Einstiegsniveaus (Pfeile) gezeichnet werden: Long-Pfeile erscheinen bei `Low - ATR * 3/8`, Short-Pfeile bei `High + ATR * 3/8`.
- Nur abgeschlossene Kerzen werden verarbeitet. Signale können um `SignalBar` Bars verzögert werden, um die ursprüngliche Pufferlogik nachzuahmen.

## Einstiegsregeln

- **Long-Einstieg**: wenn ein Aufwärtspfeil ausgelöst wird, Long-Einstiege erlaubt sind (`EnableBuyEntries = true`), keine Short-Position offen ist und das Signal noch nicht ausgeführt wurde. Das Tradevolumen beträgt `Volume`.
- **Long-Wiedereinstieg**: während einer Long-Position löst jede weitere `PriceStepTicks` Ticks zugunsten des Trades (basierend auf dem Kerzenschluss) eine weitere Kauforder aus, bis `MaxPyramidingPositions` Schichten geöffnet sind. Jeder Wiedereinstieg aktualisiert die Schutz-Stop-/Target-Niveaus.
- **Short-Einstieg/-Wiedereinstieg**: Spiegellogik der Long-Seite unter Verwendung des Abwärtspfeils.

## Ausstiegsregeln

- **Signalbasierte Ausstiege**: Ein Aufwärtspfeil schließt jeden aktiven Short, wenn `EnableSellExits = true`; ein Abwärtspfeil schließt den Long, wenn `EnableBuyExits = true`.
- **Risikoausstiege**: Jede offene Schicht trägt dieselbe Stop-Loss- und Take-Profit-Distanz, definiert in Ticks (`StopLossTicks`, `TakeProfitTicks`). Wenn der Preis das Niveau innerhalb der aktuellen Kerze durchbricht, wird die gesamte Position glattgestellt.
- **Manuelles Glätten**: Entgegengesetzte Einstiegssignale neutralisieren auch die vorherige Richtung vor dem Öffnen einer neuen Position.

## Positionsverwaltung

- Die Positionsgröße ist durch `Volume` für jede Order fest.
- Stop-Loss und Take-Profit werden in Sicherheits-Ticks gemessen. Sie auf null setzen deaktiviert den entsprechenden Schutz.
- Der Pyramidenzähler setzt sich nach jedem vollständigen Ausstieg auf null zurück, damit das nächste Signal von einer frischen Basisposition beginnt.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|----------|
| `Volume` | Ordergröße für jeden Einstieg | `1` |
| `StopLossTicks` | Stop-Abstand in Ticks, 0 deaktiviert | `1000` |
| `TakeProfitTicks` | Take-Profit-Abstand in Ticks, 0 deaktiviert | `2000` |
| `PriceStepTicks` | Minimale günstige Bewegung vor dem Aufstocken | `300` |
| `MaxPyramidingPositions` | Maximale Anzahl gestapelter Einstiege (einschließlich des ersten) | `10` |
| `EnableBuyEntries` / `EnableSellEntries` | Öffnen von Long/Short-Positionen erlauben | `true` |
| `EnableBuyExits` / `EnableSellExits` | Schließen von Long/Short-Positionen bei entgegengesetzten Pfeilen erlauben | `true` |
| `CandleType` | Zeitrahmen für Signale | `H4` |
| `Range` | XO-Box-Höhe in Ticks | `10` |
| `AppliedPrice` | Im XO-Detektor verwendete Preisquelle | `Close` |
| `SignalBar` | Anzahl geschlossener Bars zur Signalverzögerung | `1` |

Die Strategie ist für Backtesting oder Live-Trading mit Instrumenten konzipiert, die einen zuverlässigen Kursschritt bereitstellen. Die tick-basierten Abstände anpassen, um der Volatilität des ausgewählten Marktes zu entsprechen.
