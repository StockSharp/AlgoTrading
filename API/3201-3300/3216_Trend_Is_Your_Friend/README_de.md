# Trend Is Your Friend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Trend Is Your Friend-Strategie ist ein Multi-Timeframe-Trendfolge-System, inspiriert vom ursprünglichen MetaTrader-Expertenberater. Es richtet Intraday-Momentum mit einem MACD-Filter des höheren Zeitrahmens aus, während das Risiko durch Bollinger-Bänder-Ausstiege, klassische Stop-Loss- und Take-Profit-Ziele, eine optionale Break-Even-Sperre und Trailing-Stop-Management verwaltet wird.

Die Strategie arbeitet auf einem konfigurierbaren Basis-Zeitrahmen (Standard: 1 Stunde) und analysiert die Kerzenstruktur für ein kurzfristiges Momentum-Muster: eine bärische Kerze gefolgt von einer stärkeren bullischen Kerze für Long-Trades oder das Gegenteil für Short-Trades. Diese Muster müssen mit einem gleitenden Durchschnitts-Trendfilter und einem monatlichen MACD-Signal übereinstimmen, bevor eine Position eröffnet wird.

## Einstiegslogik
1. Eine schnelle EMA und eine langsame LWMA auf dem Einstiegs-Zeitrahmen berechnen.
2. Die letzten zwei abgeschlossenen Kerzen verfolgen, um ein Momentum-Muster zu bilden:
   - **Long-Setup:** die Kerze vor zwei Bars ist bärisch, die vorherige Kerze ist bullisch und größer in der Magnitude.
   - **Short-Setup:** die Kerze vor zwei Bars ist bullisch, die vorherige Kerze ist bärisch und kleiner in der Magnitude.
3. Das Setup mit dem gleitenden Durchschnitts-Trendfilter bestätigen (schnelle MA über langsamer MA für Long-Trades, darunter für Short-Trades).
4. Den langfristigen Trend mit einem MACD-Signal bestätigen, das auf dem höheren Zeitrahmen berechnet wird (Standard: monatlich). Die MACD-Linie muss für Long-Trades über der Signallinie liegen und darunter für Short-Trades.
5. Wenn alle Filter übereinstimmen, eine Position zum Markt mit dem konfigurierten Volumen eröffnen.

## Ausstiegslogik
- **Bollinger-Bänder-Ausstieg:** Long-Positionen werden geschlossen, wenn der Preis über das obere Band schließt; Short-Positionen, wenn der Preis unter das untere Band schließt.
- **Take-Profit / Stop-Loss:** optionale feste Abstände in Pips. Die Implementierung konvertiert Pips über den Wertpapier-Preisschritt in Preisabstand.
- **Break-Even:** optional, verschiebt den Schutz-Stop auf (oder über) den Einstiegspreis, nachdem eine konfigurierbare Gewinnschwelle erreicht wurde.
- **Trailing Stop:** optional, wird nach einem Gewinnschwellenwert aktiviert und verfolgt den Preis um eine feste Pip-Distanz. Der Trailing Stop teilt denselben Speicher mit dem Break-Even-Niveau.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| Entry Candle | Kerzentyp für Einstiegslogik | 1 Stunde |
| MACD Candle | Höherer Zeitrahmen für MACD-Filter | 30 Tage |
| Fast MA | Schnelle EMA-Länge | 8 |
| Slow MA | Langsame LWMA-Länge | 20 |
| Bollinger Length | Bollinger-Bänder-Periode | 20 |
| Bollinger Width | Standardabweichungs-Multiplikator der Bollinger-Bänder | 2.0 |
| Stop Loss (pips) | Schutz-Stop-Distanz | 20 |
| Take Profit (pips) | Gewinnziel-Distanz | 50 |
| Use Break-Even | Break-Even-Anpassung aktivieren | true |
| Break-Even Trigger | Gewinn (Pips) zum Verschieben des Stops erforderlich | 10 |
| Break-Even Offset | Offset am Break-Even-Stop angewendet | 5 |
| Use Trailing | Trailing Stop aktivieren | true |
| Trailing Activation | Gewinn (Pips) zum Aktivieren des Trailings erforderlich | 40 |
| Trailing Distance | Distanz (Pips) vom Trailing Stop gehalten | 40 |

## Hinweise
- Die Strategie speichert nur die letzten zwei abgeschlossenen Kerzen, um schwere historische Buffer zu vermeiden.
- MACD-Daten werden vom konfigurierten höheren Zeitrahmen mit aktivierter Aggregation abonniert, was ermöglicht, dass monatliche Signale aus Tagesdaten aufgebaut werden, wenn nötig.
- Die Pip-zu-Preis-Konvertierung verwendet den Wertpapier-Preisschritt. Instrumente mit nicht-standardmäßigen Pip-Definitionen können eine Parameter-Anpassung erfordern.
