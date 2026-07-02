# Effizientes polnisches Layer-Expert-Advisor-System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine direkte Portierung des MQL4-Expertenberaters „Polish Layer Expert Advisor System Efficient“. Es ist für Intraday-Charts konzipiert (der ursprüngliche Autor empfahl 5- oder 15-Minuten-Kerzen) und beschränkt den Handel auf jeweils eine einzelne Position. Die Trendrichtung wird durch die Ausrichtung zwischen einem schnellen und einem langsamen gleitenden Preisdurchschnitt zusammen mit zwei geglätteten RSI-Filtern definiert. Tatsächliche Einträge erfordern eine dreifache Bestätigung durch die Indikatoren Stochastic Oscillator, DeMarker und Williams %R, um Umkehrungen von extremen Bedingungen zu erfassen, die innerhalb des vorherrschenden Trends auftreten.

## Handelslogik
1. **Trendfilter.** Ein einfacher gleitender 9-Perioden-Durchschnitt (SMA) der Schlusskurse muss über dem linear gewichteten gleitenden Durchschnitt (LWMA) über 45 Perioden liegen, um Long-Positionen zuzulassen, und darunter, um Short-Positionen zu ermöglichen. Gleichzeitig muss der 9-Perioden-SMA von RSI über (für Long-Positionen) oder unter (für Short-Positionen) dem 45-Perioden-SMA von RSI liegen. Bei Unstimmigkeiten zwischen dem Preis und den RSI-Filtern werden neue Bestellungen blockiert.
2. **Stochastic-Trigger.** Wenn der Trendfilter bullisch ist, wartet die Strategie darauf, dass die Stochastic %K-Linie den überverkauften Schwellenwert (Standard 19) nach oben und gleichzeitig %D überschreitet. Für bärische Setups muss %K unter die überkaufte Schwelle (Standard 81) fallen und unter %D fallen. Der Verlangsamungsfaktor bleibt aus dem Skript MQL4 erhalten.
3. **Momentum-Bestätigungen.** Ein Long-Signal erfordert außerdem, dass DeMarker 0,35 nach oben kreuzt und Williams %R bei −81 der aktuell abgeschlossenen Kerze nach oben kreuzt. Kurze Signale erfordern Abwärtsdurchgänge durch 0,63 bzw. −19. Alle Kreuzungen zwischen der vorherigen fertigen Kerze und der aktuellen werden ausgewertet.
4. **Positionsmanagement.** Es werden nur Marktaufträge ausgegeben und die Strategie bleibt unverändert, bis ein schützender Stopp oder ein Ziel den Handel schließt. Die Schutzniveaus werden aus Pip-basierten Parametern mithilfe der Instrumentenpreisstufe neu berechnet. Wenn die Preisstufe nicht verfügbar ist, ist der Schutz deaktiviert.

## Risikomanagement
* **Stop-Loss / Take-Profit.** Abstände werden in Pips konfiguriert. Wenn sie positiv sind, werden die Werte mit `Security.PriceStep` (1 Pip = 1 Preisschritt) in tatsächliche Preisversätze umgewandelt und sofort nach der Eingabe angewendet. Wenn Sie einen Parameter auf `0` setzen, wird die entsprechende Schutzstufe deaktiviert.
* **Einzelne Position.** Der ursprüngliche EA hat nie eine Pyramide gebildet, daher verweigert der Port die Eingabe, wenn bereits eine Position vorhanden ist.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Volume` | `0.1` | Bestellvolumen in Losen. Passen Sie es entsprechend der Größe des Maklervertrags an. |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` | Kerzentyp, der für Indikatorberechnungen verwendet wird. Legen Sie einen Zeitrahmen von 5 oder 15 Minuten fest, um das Original EA widerzuspiegeln. |
| `RsiPeriod` | `14` | Lookback-Länge für die Basisberechnung RSI. |
| `ShortPricePeriod` | `9` | Zeitraum des im Trendfilter verwendeten Schnellpreises SMA. |
| `LongPricePeriod` | `45` | Zeitraum des langsamen Preis-LWMA, der im Trendfilter verwendet wird. |
| `ShortRsiPeriod` | `9` | Länge des schnellen SMA, angewendet auf RSI-Werte. |
| `LongRsiPeriod` | `45` | Länge des langsamen SMA, angewendet auf RSI-Werte. |
| `StochasticKPeriod` | `5` | Basis-%K-Periode für den Stochastic-Oszillator. |
| `StochasticDPeriod` | `3` | Glättungszeitraum für die %D-Linie. |
| `StochasticSlowing` | `3` | Zusätzlicher Glättungsfaktor auf %K angewendet. |
| `DemarkerPeriod` | `14` | Mittelungszeitraum für den DeMarker-Indikator. |
| `WilliamsPeriod` | `14` | Lookback-Zeitraum für Williams %R. |
| `StochasticOversoldLevel` | `19` | Überverkaufter Schwellenwert, den %K nach oben überschreiten muss, um Long-Einträge zu ermöglichen. |
| `StochasticOverboughtLevel` | `81` | Überkaufter Schwellenwert, den %K nach unten überschreiten muss, um Short-Einstiege zu ermöglichen. |
| `DemarkerBuyLevel` | `0.35` | Erforderlicher Mindestwert für DeMarker für lange Einträge (Kreuzung von unten). |
| `DemarkerSellLevel` | `0.63` | Maximal zulässiger DeMarker-Wert für kurze Einträge (Kreuzung von oben). |
| `WilliamsBuyLevel` | `-81` | Williams %R überschreitet den Pegel und bestätigt lange Eingaben. |
| `WilliamsSellLevel` | `-19` | Williams %R überschreitet Niveau und bestätigt kurze Eingaben. |
| `StopLossPips` | `7777` | Stop-Loss-Distanz in Pips. Der sehr große Standardwert deaktiviert den Stopp effektiv, sofern er nicht konfiguriert ist. |
| `TakeProfitPips` | `17` | Take-Profit-Distanz in Pips. Auf `0` setzen, um das feste Ziel zu deaktivieren. |

## Notizen
* Stellen Sie sicher, dass `Security.PriceStep`, `Security.MinVolume` und `Security.VolumeStep` ordnungsgemäß konfiguriert sind. Bei der Umrechnung der Risikoparameter geht die Strategie davon aus, dass ein Pip einem Preisschritt entspricht.
* Die Eintrittsfilter hängen von den Indikatorkreuzungen zwischen aufeinanderfolgenden abgeschlossenen Kerzen ab. Achten Sie beim Importieren historischer Daten darauf, dass die Balkenausrichtung mit dem ursprünglichen Zeitrahmen übereinstimmt, um die Ergebnisse zu reproduzieren.
