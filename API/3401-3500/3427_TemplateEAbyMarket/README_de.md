# Strategie TemplateEAbyMarket Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
TemplateEAbyMarket ist eine direkte StockSharp-Portierung des ursprünglichen MetaTrader 4-Expertenberaters *TemplateEAbyMarket.mq4*. Die Strategie verwendet den Indikator „Moving Average Convergence Divergence“ (MACD), um Momentumverschiebungen zu erkennen. Wenn die MACD-Hauptlinie die Signallinie kreuzt, während sich beide Komponenten in derselben positiven oder negativen Zone befinden, eröffnet die Strategie eine Marktposition in der Richtung der Kreuzung. Ausstiege werden ausschließlich durch Schutzaufträge (Take Profit und Stop Loss) verwaltet, die über den integrierten `StartProtection`-Helfer konfiguriert werden.

Die Version StockSharp behält das Verhalten des Programms MQL bei: Sie öffnet nur neue Positionen, ohne zu versuchen, die Gegenseite automatisch zu schließen. Sobald eine Position besetzt ist, muss der Handel durch Schutzniveaus oder manuelle Eingriffe verwaltet werden.

## Handelslogik
1. Abonnieren Sie den vom Benutzer ausgewählten Kerzentyp (Standard: 15-Minuten-Zeitrahmen).
2. Berechnen Sie MACD (standardmäßig 26.12.9) für jede fertige Kerze.
3. Verfolgen Sie die relative Position der MACD-Haupt- und Signalleitungen, um ein Crossover-Ereignis zu erkennen:
   - **Bulnisches Setup:** Bei der vorherigen Kerze lag die Hauptlinie unter der Signallinie, die aktuelle Kerze schließt mit der Hauptlinie über der Signallinie und beide Linien liegen über Null. Eine Marktkauforder mit `OrderVolume` wird übermittelt, wenn das aktuelle Engagement unter `MaxOrders * OrderVolume` liegt.
   - **Bearisches Setup:** Bei der vorherigen Kerze lag die Hauptlinie über der Signallinie, die aktuelle Kerze schließt mit der Hauptlinie unter der Signallinie und beide Linien liegen unter Null. Ein Market-Verkaufsauftrag mit `OrderVolume` wird vorbehaltlich der gleichen Exposure-Obergrenze übermittelt.
4. Die Schutzstufen `takeProfit` und `stopLoss` werden einmalig beim Start aktiviert. Die Strategie schließt entgegengesetzte Positionen nicht automatisch; Das Risiko wird durch das Schutzmodul oder durch den Benutzer kontrolliert.

## Parameter
| Name | Beschreibung |
|------|-------------|
| `MacdFastPeriod` | Schnelle EMA-Länge für die MACD-Berechnung. |
| `MacdSlowPeriod` | Langsame EMA-Länge für die MACD-Berechnung. |
| `MacdSignalPeriod` | Signallänge EMA für die MACD-Berechnung. |
| `CandleType` | Kerzentyp (Zeitrahmen), der den Indikator speist. |
| `OrderVolume` | Mit jeder Market-Order übermitteltes Volumen. |
| `MaxOrders` | Maximale Anzahl gleichzeitiger Bestellungen, ausgedrückt als Vielfaches von `OrderVolume`. Die Strategie prüft `abs(Position) < MaxOrders * OrderVolume`, bevor eine neue Bestellung gesendet wird. |
| `TakeProfitPoints` | Take-Profit-Distanz in Preispunkten. Der Wert `0` deaktiviert den Take-Profit. |
| `StopLossPoints` | Stop-Loss-Distanz in Preispunkten. Der Wert `0` deaktiviert den Stop-Loss. |

## Notizen
- Slippage- und Magic Number-Einstellungen aus der MQL-Version werden absichtlich weggelassen, da sie in StockSharp anders gehandhabt werden.
- Stellen Sie sicher, dass der Connector die richtigen Preisschritt-Metadaten bereitstellt. `StartProtection` interpretiert Entfernungen in Instrumentenpreispunkten.
- Die Vorlage ist bewusst minimalistisch gehalten und verwaltet keine Teilfüllungen oder Pyramideneinträge über das Limit von `MaxOrders` hinaus.
