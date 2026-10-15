# Hpcs Inter7-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Hpcs Inter7-Strategie ist ein Bollinger-Band-Breakout-System, das aus dem MetaTrader 4-Expertenberater `_HPCS_Inter7_MT4_EA_V01_We.mq4` umgewandelt wurde. Der Algorithmus überwacht Standard-Bollinger-Bänder, die für die ausgewählte Kerzenserie berechnet wurden. Wenn der Preis die Bänder überschreitet, interpretiert er dies als Momentum-Ausbruch und eröffnet eine Position in Richtung des Ausbruchs. Bei jedem neuen Einstieg platziert die Strategie sofort sowohl Stop-Loss- als auch Take-Profit-Ziele in einem festen Abstand zum Einstiegspreis, um das ursprüngliche Verhalten des Expert Advisors nachzubilden.

## Handelslogik
- **Short-Einstieg**: Wenn die vorherige Kerze oberhalb des unteren Bandes schloss und die zuletzt geschlossene Kerze unterhalb des unteren Bandes endete, eröffnet die Strategie einen Marktverkauf. Dadurch wird der ursprüngliche Zustand `Close[0] < LowerBand[0] && Close[1] > LowerBand[1]` wiederhergestellt.
- **Long-Einstieg**: Wenn die vorherige Kerze unterhalb des oberen Bandes schloss und die letzte geschlossene Kerze oberhalb des oberen Bandes endete, eröffnet die Strategie einen Marktkauf. Dadurch wird `Close[0] > UpperBand[0] && Close[1] < UpperBand[1]` aus der MQL-Implementierung repliziert.
- **Einzelhandel pro Kerze**: Der Algorithmus merkt sich die Öffnungszeit der Kerze, die den letzten Auftrag generiert hat. Ein neues Signal auf derselben Kerze wird ignoriert, um doppelte Trades zu vermeiden, was die Guard-Variable `gdt_Candle` von MQL4 widerspiegelt.
- **Schutzanordnungen**: Unmittelbar nachdem eine neue Position eröffnet wurde, ruft die Strategie `SetStopLoss` und `SetTakeProfit` unter Verwendung der konfigurierten Distanz auf. Beide sind symmetrisch um den Einstiegspreis platziert, sodass die Position immer vordefinierte Risiko- und Ertragsziele aufweist.

## Parameter
| Name | Beschreibung | Standard | Optimierbar |
| --- | --- | --- | --- |
| `BollingerLength` | Anzahl der Kerzen, die zum Aufbau der Bollinger-Bänder verwendet werden. | 20 | Ja |
| `BollingerDeviation` | Standardabweichungsmultiplikator für die Bandbreite Bollinger. | 2 | Ja |
| `CandleType` | Für Berechnungen verwendete Kerzenserie (standardmäßig 1 Minute Zeitrahmen). | 1-Minuten-Kerzen | Nein |
| `ProtectionDistancePoints` | Stop-Loss- und Take-Profit-Distanz, ausgedrückt in Preisschritten. | 10 | Ja |

## Zusätzliche Hinweise
- Die Strategie verwendet die StockSharp-Hochebene API (`SubscribeCandles().Bind(...)`) und speichert keine benutzerdefinierten Verlaufsarrays.
- `StartProtection()` wird beim Start aktiviert, sodass die Plattform automatisch Schutzanordnungen verwaltet, die von `SetStopLoss` und `SetTakeProfit` aufgegeben wurden.
- Die Positionsgröße wird durch die Basiseigenschaft `Strategy.Volume` gesteuert, genau wie beim ursprünglichen Expert Advisor, der ein festes Volumen von einem Lot handelte.
- Die Strategie wurde für FX-Instrumente entwickelt, bei denen der ursprüngliche EA eingesetzt wurde, sie kann jedoch auf jedes Wertpapier angewendet werden, das aussagekräftige Bollinger-Bandsignale und einen gültigen `PriceStep`-Wert liefert.
