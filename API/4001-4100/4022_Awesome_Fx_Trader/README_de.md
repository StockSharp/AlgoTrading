# Toller Devisenhändler
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das MetaTrader-Setup von `MQL/8539`, das aus den benutzerdefinierten Indikatoren **AwesomeFxTradera.mq4** und **t_ma.mq4** besteht. Der Originalcode malt das Bill Williams Awesome Oscillator-Histogramm in Grün oder Rot, je nachdem, ob der Wert steigt oder fällt, und überlagert einen linear gewichteten gleitenden Durchschnitt (LWMA) mit 34 Perioden neben einem geglätteten Klon derselben Kurve. Der StockSharp-Port behält die gleichen Berechnungen bei und wandelt die Indikatorfarben in Handelssignale um.

## Ursprüngliche MQL-Logik

1. **AwesomeFxTradera.mq4** berechnet zwei exponentielle gleitende Durchschnitte, die auf den **Eröffnungspreis** mit den Perioden 8 und 13 angewendet werden. Ihre Differenz wird in `ExtBuffer0` gespeichert. Der Puffer wird grün gefärbt, wenn der aktuelle Wert höher als der vorherige Balken ist, und rot, wenn er niedriger ist. Dies kodiert effektiv die Richtung des Impulses, nicht nur sein Vorzeichen.
2. **t_ma.mq4** stellt einen 34-Perioden-LWMA des Eröffnungspreises (`ExtMapBuffer1`) und einen 6-Perioden-einfachen gleitenden Durchschnitt dieses LWMA (`ExtMapBuffer2`) dar. Der Smoother verfolgt, ob sich der Trenddurchschnitt beschleunigt oder verlangsamt.

Das MetaTrader-Diagramm verdeutlicht daher das zinsbullische Momentum, wenn der Oszillator über Null liegt und weiter steigt, während der Preis über dem geglätteten LWMA liegt. Das rückläufige Momentum ist die entgegengesetzte Konfiguration.

## StockSharp-Implementierung

Der `AwesomeFxTraderStrategy` abonniert einen konfigurierbaren Kerzentyp (Standard **M15**) und speist die Indikatoren mit dem Kerzenöffnungspreis, um ihn an die MetaTrader-Puffer anzupassen.

1. Die schnellen und langsamen EMAs werden bei jeder fertigen Kerze neu berechnet; Ihre Differenz gibt das oszillierende Histogramm wieder.
2. Der LWMA verfolgt den 34-Balken-Trend und ein 6-Balken SMA glättet ihn. Der Vergleich beider Serien zeigt, ob die Trendkurve steigt oder fällt.
3. Die Oszillatorfarbe wird neu erstellt, indem der aktuelle Histogrammwert mit dem vorherigen Balken verglichen wird und dabei der `bool up`-Logik aus der MQL-Implementierung folgt.
4. **Eintrittsregeln**:
   - Geben Sie „long“ ein, wenn der Oszillator positiv ist, ansteigt (grüner Puffer) und der LWMA über seinem Glättungswert liegt.
   - Geben Sie kurz ein, wenn der Oszillator negativ ist, fällt (roter Puffer) und der LWMA unter seinem Glättungswert liegt.
5. **Ausstiegs-/Umkehrregeln**: Ein entgegengesetztes Signal kehrt die Position um. Die Ordergröße wird automatisch um die absolute aktuelle Position erhöht, sodass Short-Positionen geschlossen werden, bevor eine Long-Position entsteht, und umgekehrt.

Im Quellcode sind keine zusätzlichen Stop-Loss- oder Take-Profit-Levels definiert, daher verlässt sich der Port bei Ausstiegen ausschließlich auf Momentum-Flips. Protokollierungsanweisungen dokumentieren jeden Handelsauslöser zusammen mit den Indikatorwerten.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `FastEmaPeriod` | 8 | Länge des schnellen EMA, das im Oszillator-Replikat verwendet wird. |
| `SlowEmaPeriod` | 13 | Länge des langsamen EMA. |
| `TrendLwmaPeriod` | 34 | Zeitraum des LWMA-Trendfilters aus `t_ma.mq4`. |
| `TrendSmoothingPeriod` | 6 | Fenster des auf die LWMA-Werte angewendeten SMA. |
| `CandleType` | 15-minütiger Zeitrahmen | Kerzendatentyp, der sowohl für Momentum- als auch für Trendberechnungen verwendet wird. |

Dank der `StrategyParam`-Metadaten können alle Parameter über die StockSharp-Benutzeroberfläche optimiert werden.

## Dateizuordnung

| MetaTrader-Datei | StockSharp Gegenstück | Notizen |
| --- | --- | --- |
| `MQL/8539/AwesomeFxTradera.mq4` | `CS/AwesomeFxTraderStrategy.cs` | Erstellt den EMA-on-open-Oszillator und seine steigende/fallende Farblogik neu. |
| `MQL/8539/t_ma.mq4` | `CS/AwesomeFxTraderStrategy.cs` | Implementiert den 34-Perioden-LWMA mit einem 6-Perioden-SMA-Glätter zur Trenderkennung. |

Die Python-Version wird wie gewünscht bewusst weggelassen.
