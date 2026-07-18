# OneHrStocTrader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **OneHrStocTrader**-Strategie repliziert den MetaTrader 4 Experten-Advisor *OneHrStocTrader.mq4* innerhalb der StockSharp High-Level-API. Sie handelt ein einzelnes Instrument auf stündlichen Kerzen und kombiniert den stochastischen Oszillator mit einem Bollinger-Band-Breite-Filter. Ein Trade wird nur eröffnet, wenn die Volatilität (gemessen am Abstand zwischen den Bollinger-Bändern) innerhalb des konfigurierten Bereichs liegt und der stochastische Oszillator eine extreme Zone genau zur konfigurierten Stunde verlässt.

## Handelslogik

1. **Daten**
   - Arbeitet standardmäßig mit stündlichen Kerzen (konfigurierbar).
   - Verwendet die neuesten *abgeschlossenen* Kerzenwerte, um das MetaTrader-Verhalten zu entsprechen.
2. **Bollinger-Band-Filter**
   - Berechnet die Spanne zwischen dem oberen und unteren Band in Pips.
   - Handelssignale werden ignoriert, wenn die Spanne außerhalb des `[BollingerSpreadLower, BollingerSpreadUpper]`-Bereichs liegt.
3. **Stochastischer Oszillator-Trigger**
   - Referenziert die zwei neuesten abgeschlossenen Kerzen der stochastischen %K-Linie.
   - **Kauf**: Aktueller %K unter `StochasticLower`, vorheriger %K steigend (`prev < current`) und die neue Bar beginnt bei `BuyHourStart`.
   - **Verkauf**: Aktueller %K über `StochasticUpper`, vorheriger %K fallend (`prev > current`) und die neue Bar beginnt bei `SellHourStart`.
4. **Order-Management**
   - Schließt eine entgegengesetzte Position vor dem Öffnen einer neuen.
   - Begrenzt aufeinanderfolgende Einstiege in dieselbe Richtung über `MaxOrdersPerDirection`.
5. **Risikomanagement**
   - Feste Take-Profit- und Stop-Loss-Abstände in Pips definiert.
   - Optionaler Trailing-Stop, der sich in Pip-Schritten bewegt, sobald der Preis über die konfigurierte Distanz hinausgeht.
   - Interne Schutzlevel werden bei jeder abgeschlossenen Kerze überwacht; wenn erreicht, schließt die Strategie die Position zum Marktpreis.

## Parameter

| Name | Beschreibung | Standardwert |
|------|-------------|---------|
| `TradeVolume` | Ordergröße in Lots. | `0.01` |
| `CandleType` | Zeitrahmen für alle Berechnungen. | `1h` |
| `BollingerPeriod` | Bollinger-Band-Rückblickzeitraum. | `20` |
| `BollingerSigma` | Bollinger-Band-Sigma-Multiplikator. | `2.0` |
| `BollingerSpreadLower` | Minimale Band-Spanne in Pips zum Handeln. | `56` |
| `BollingerSpreadUpper` | Maximale Band-Spanne in Pips zum Handeln. | `158` |
| `BuyHourStart` | Stunde (0-23) für Long-Einstiegsbewertung. | `4` |
| `SellHourStart` | Stunde (0-23) für Short-Einstiegsbewertung. | `0` |
| `StochasticKPeriod` | Stochastische %K-Periode. | `5` |
| `StochasticDPeriod` | Stochastische %D-Periode. | `3` |
| `StochasticSlowing` | Stochastischer Verlangsamungsfaktor. | `5` |
| `StochasticLower` | Überverkauft-Schwellenwert. | `36` |
| `StochasticUpper` | Überkauft-Schwellenwert. | `70` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | `200` |
| `StopLossPips` | Stop-Loss-Abstand in Pips. | `95` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (0 = deaktiviert). | `40` |
| `MaxOrdersPerDirection` | Maximale aufeinanderfolgende Einstiege pro Richtung. | `1` |

## Charting

Wenn eine Chartfläche verfügbar ist, zeichnet die Strategie:
- Preiskerzen.
- Bollinger-Bänder.
- Stochastischen Oszillator auf einem separaten Bereich.
- Ausgeführte Trades zur schnellen visuellen Validierung.

## Hinweise

- Die Pip-Größe wird aus dem Instrument-Preisschritt und der Dezimalgenauigkeit abgeleitet, entsprechend der MetaTrader-Multiplikatorlogik.
- Schutzlevel werden mit `Security.ShrinkPrice` neu berechnet, um börsenkonformes Preisrounding sicherzustellen.
- Trailing-Stop-Anpassungen ahmen den ursprünglichen EA nach, indem der Stop nur gestrafft wird, wenn der Preis mindestens einen Pip über den vorherigen Stop hinausgeht.
- Die Implementierung erstellt keine ausstehenden Orders; alle Einstiege und Ausstiege verwenden Market-Orders genau wie der Quell-Experten-Advisor.
