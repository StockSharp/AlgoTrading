# Strategie Starter V6 Mod E
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Starter V6 Mod E** ist eine High-Level-StockSharp-Konvertierung des MetaTrader 4 Expert Advisors `Starter_v6mod_e_www_forex-instruments_info.mq4`. Der Port behält die ursprüngliche Kombination aus Laguerre-Oszillator-Extremen, dualer EMA-Impulsbestätigung, CCI-Filterung und EMA-Winkel-Gating bei und passt die Ausführung gleichzeitig an die ereignisgesteuerte Architektur von StockSharp an.

## Handelslogik

- **Trend-Gate:** Eine 34-Perioden-EMA-Steigung wird zwischen konfigurierbaren Start-/Endverschiebungen gemessen. Die Steigung wird in Pip-Einheiten ausgedrückt; Nur positive Steigungen ermöglichen Long-Trades, nur negative Steigungen ermöglichen Short-Positionen und flache Werte blockieren neue Einstiege.
- **Laguerre-Extreme:** Ein handgefertigter Laguerre RSI (Gamma = 0,7 standardmäßig) verfolgt überverkaufte/überkaufte Zustände auf der Skala von 0–1. Bei Long-Positionen müssen sowohl der aktuelle als auch der vorherige Wert unter dem Niveau `Laguerre Oversold` bleiben, bei Short-Positionen müssen beide Werte über `Laguerre Overbought` liegen.
- **EMA Momentumfilter:** 120- und 40-Perioden-EMAs (Medianpreis) müssen bei Long-Positionen steigen und bei Short-Positionen fallen, was dem ursprünglichen MA-Filter entspricht.
- **CCI-Bestätigung:** Ein 14-Perioden-CCI muss für Long-Positionen unter `-CCI Threshold` und für Short-Positionen über `+CCI Threshold` liegen und den `Alpha`-Filter von MQL replizieren.
- **Sicherheit am Freitag:** Neue Trades werden nach `Friday Block Hour` blockiert und alle verbleibenden Positionen werden liquidiert, sobald `Friday Exit Hour` erreicht ist.

## Risikomanagement

- Konfigurierbare Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände (in Pips) emulieren den Geldverwaltungsblock des Experten.
- Trailing-Stops folgen dem günstigsten Preis nach dem Einstieg und schließen den Handel, wenn das Retracement die konfigurierte Distanz überschreitet.
- Die manuelle Positionsschließung wird über `SellMarket`/`BuyMarket` ausgeführt, wodurch eine hohe API-Konformität gewährleistet wird.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Auftragsvolumen für jeden Markteintritt. |
| `StopLossPips` | Schutzstoppabstand in Pips. |
| `TakeProfitPips` | Gewinnziel in Pips. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips (0 deaktiviert das Trailing). |
| `SlowEmaPeriod` | Zeitraum des langsamen EMA, berechnet am PRICE_MEDIAN. |
| `FastEmaPeriod` | Periode des schnellen EMA berechnet am PRICE_MEDIAN. |
| `AngleEmaPeriod` | EMA Zeitraum, der für den Winkeldetektor verwendet wird. |
| `AngleStartShift` / `AngleEndShift` | Zur Berechnung der Steigung EMA verwendete Balkenverschiebungen. |
| `AngleThreshold` | Mindeststeilheit (in Pip-Einheiten), die erforderlich ist, um den Handel zu ermöglichen. |
| `CciPeriod` / `CciThreshold` | Zeitraum und absoluter Schwellenwert für den CCI-Filter. |
| `LaguerreGamma` | Gamma-Parameter für den Laguerre-Oszillator. |
| `LaguerreOversold` / `LaguerreOverbought` | Eintrittsschwellen auf der Laguerre-Skala von 0–1. |
| `CandleType` | Kerzendatentyp (Standard 1 Minute). |
| `FridayBlockHour` / `FridayExitHour` | Stunden (lokale Instrumentenzeit), die die Freitagsrisikolimits steuern. |

## Konvertierungshinweise

- Der Laguerre-Oszillator wird direkt aus der ursprünglichen rekursiven Formel implementiert, wobei der Ausgangsbereich 0–1 und die Gammaglättung beibehalten werden.
- Die Steigung EMA ersetzt den Winkelhelfer MQL, indem Pip-normalisierte Unterschiede zwischen historischen EMA-Punkten berechnet werden.
- Auf Geldverwaltungsfunktionen wie Equity Cut-off und Grid Stacking wird bewusst verzichtet, da die MT4-Variante, die konvertiert wird, diese standardmäßig deaktiviert hat und StockSharp eine explizite Portfoliokontrolle fördert.
- Bestellungen werden über `BuyMarket`/`SellMarket` gesendet und verlassen sich auf `OnNewMyTrade`, um die Füllpreise für die abschließende Logik zu verfolgen.
