# TrueSort 1001-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

TrueSort 1001 ist ein striktes Trendfolgesystem, das den ursprünglichen Expertenberater MQL widerspiegelt. Die Strategie überwacht fünf einfache gleitende Durchschnitte und reagiert nur, wenn sie drei aufeinanderfolgende abgeschlossene Kerzen lang perfekt geordnet bleiben. Ein steigender durchschnittlicher Richtungsindex (ADX) bestätigt die Dynamik, bevor ein Handel eröffnet wird. Sobald die Position auf dem Markt ist, wird sie durch einen adaptiven Trailing Stop geschützt, der in Preisschritten gemessen wird, und der Handel wird geschlossen, sobald die gleitenden Durchschnitte ihre Ausrichtung verlieren.

## Logik

### Trend- und Momentumfilter
- Für den ausgewählten Zeitrahmen werden fünf SMAs (standardmäßig 10, 20, 50, 100 und 200 Perioden) berechnet.
- Bei langen Setups müssen die schnellen SMAs bei jeder der letzten drei abgeschlossenen Kerzen streng über den langsameren liegen: `SMA10 > SMA20 > SMA50 > SMA100 > SMA200`.
- Für kurze Setups ist die umgekehrte Reihenfolge für dieselben drei Kerzen erforderlich.
- ADX mit der Periode `AdxPeriod` muss über `AdxThreshold` bleiben und der aktuelle Wert muss höher sein als die vorherige Kerze, um sicherzustellen, dass die Trendstärke zunimmt.

### Teilnahmebedingungen
1. Es ist keine Position offen.
2. Drei historische Kerzen erfüllen die oben beschriebene Ordnungsregel.
3. Der ADX-Filter ist erfolgreich.
4. Eine Marktorder von `Volume` Lots wird sofort beim Schließen der aktuellen Kerze gesendet.

### Ausstiegsbedingungen
- **Desynchronisierung des gleitenden Durchschnitts:** Wenn die aktuelle Kerze schließt und der MA-Stapel nicht mehr streng in Richtung des Handels geordnet ist, wird die Position liquidiert.
- **Trailing Protection:** `StopLossPoints` werden durch Multiplikation mit dem Instrument `PriceStep` in den absoluten Preisabstand umgewandelt. Bei Long-Trades wird der Stop auf das Maximum zwischen `SMA100` und `Close - distance` initialisiert. Bei Kurzfilmen liegt der Mindestwert zwischen `SMA100` und `Close + distance`. Nach jeder Kerze wird der Stopp in Richtung des Preises angezogen, aber nie gelockert. Wenn der Preis den Stop überschreitet, wird die Position zum Marktwert geschlossen.

### Zusätzliche Hinweise
- Alle Entscheidungen werden nur für fertige Kerzen getroffen; Unfertige Kerzen werden ignoriert.
- Der Algorithmus speichert die letzten drei SMA-Werte intern, um die `shift`-Logik aus dem ursprünglichen MQL-Skript zu replizieren, ohne den Indikatorverlauf anzufordern.
- ADX-Werte werden über `BindEx` verarbeitet und der Handel wird nur versucht, wenn die Strategie online ist und die Daten vollständig gebildet sind.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `0.1` | Bestellgröße in Losen für jeden Markteintritt. |
| `StopLossPoints` | `100` | Trailing-Stop-Distanz, ausgedrückt in Instrumentenpreisschritten. `0` deaktiviert das Trailing. |
| `Sma10Length` | `10` | Zeitraum der schnellsten SMA. |
| `Sma20Length` | `20` | Zeitraum der Sekunde SMA. |
| `Sma50Length` | `50` | Zeitraum des Mediums SMA. |
| `Sma100Length` | `100` | Zeitraum, der sowohl für die Ausrichtung als auch für die anfängliche Stoppreferenz verwendet wird. |
| `Sma200Length` | `200` | Langsamster SMA, der den langfristigen Trend bestätigt. |
| `AdxPeriod` | `14` | Zeitraum des ADX-Indikators. |
| `AdxThreshold` | `25` | Mindestniveau ADX und steigende Bedingung vor Einträgen erforderlich. |
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Kerzenreihe, die für alle Indikatorberechnungen verwendet wird. |

## Details zur Implementierung
- Der Code basiert auf dem High-Level-Kerzenabonnement StockSharp und bindet sechs Indikatoren (fünf SMAs und ADX) in einer einzigen Pipeline.
- Verlaufspuffer mit der Länge drei speichern die neuesten SMA-Werte, vermeiden Aufrufe von `GetValue()` und wahren gleichzeitig die exakte Parität mit den MQL-Verschiebungen.
- Trailing Stops werden manuell verwaltet; `StartProtection()` ist weiterhin aktiviert, sodass die Standardinfrastruktur bereit ist, falls weitere Schutzmaßnahmen erforderlich sind.
- Kommentare im Code erklären jeden Schritt auf Englisch, um die Wartung zu erleichtern.
