# DoubleUp2 Martingale Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die DoubleUp2 Martingale-Strategie reproduziert den ursprünglichen MetaTrader-Experten, indem sie den Commodity Channel Index (CCI) und den MACD-Oszillator kombiniert. Trades werden nur eröffnet, wenn beide Indikatoren extreme Niveaus in die gleiche Richtung erreichen. Die Positionsgröße folgt einem Martingal-Schema, bei dem sich das Volumen nach einem Verlusthandel verdoppelt. Profitable Geschäfte werden teilweise gesperrt, indem die Position geschlossen wird, sobald der Preis eine konfigurierbare Distanz zugunsten der Position überschreitet.

## Wie es funktioniert
1. Abonnieren Sie eine einzelne Kerzenserie (Standard 1 Minute) und berechnen Sie CCI und MACD für jeden abgeschlossenen Balken.
2. Extreme Dynamik erkennen:
   * Geben Sie short ein, wenn sowohl CCI als auch MACD den positiven Schwellenwert überschreiten.
   * Geben Sie long ein, wenn beide Werte unter den negativen Schwellenwert fallen.
3. Vor der Umkehrung wird die aktuelle Position geschlossen und der Martingalschritt basierend auf dem simulierten Gewinn des letzten Handels aktualisiert.
4. Das Handelsvolumen entspricht dem Basisvolumen, das sich aus dem Kontokapital ergibt, dividiert durch einen Saldodivisor, multipliziert mit dem Martingalfaktor, erhöht auf die aktuelle Stufe.
5. Sichern Sie sich Gewinne, indem Sie alle offenen Positionen schließen, sobald der Preis seit dem letzten Eintrag um eine vordefinierte Anzahl von Punkten gestiegen ist. Gewinnende Exits erhöhen den Martingalschritt um zwei, um dem ursprünglichen EA-Verhalten zu entsprechen.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `CciPeriod` | Lookback-Zeitraum für den Indikator CCI. | 8 |
| `MacdFastPeriod` | Schnelle EMA-Länge für MACD. | 13 |
| `MacdSlowPeriod` | Langsame EMA-Länge für MACD. | 33 |
| `MacdSignalPeriod` | Signallänge von EMA für die Glättung von MACD. | 2 |
| `Threshold` | Absoluter Indikatorschwellenwert, der überschritten werden muss, um Einträge auszulösen. | 230 |
| `ExitDistancePoints` | Gewinndistanz in Punkten, die die Schließung einer Position auslöst. | 120 |
| `BalanceDivisor` | Der Divisor wird auf das Portfolioeigenkapital angewendet, um das Basisvolumen zu erhalten. | 50001 |
| `MinimumVolume` | Untergrenze für das berechnete Handelsvolumen. | 0,1 |
| `MartingaleMultiplier` | Der Multiplikator wird nach jedem verlorenen Schlusskurs auf die Positionsgröße angewendet. | 2 |
| `CandleType` | Für alle Berechnungen verwendeter Kerzenzeitrahmen. | 1 Minute |

## Notizen
* Die Martingal-Logik erhöht die Positionsgröße nach Verlusten und setzt sie nach profitablen Umkehrungen zurück und spiegelt die Quelllogik MQL wider.
* Preisschrittinformationen werden verwendet, um die Ausstiegsentfernung (Punkte) in absolute Preiseinheiten umzurechnen. Bietet das Instrument keinen Preisschritt, wird ein Wert von 1 verwendet.
* Die Strategie erwartet ein einzelnes Instrument und platziert keine gleichzeitigen Long- und Short-Positionen.
