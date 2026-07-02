# ADX & MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Experten **ADX_MA (Fortrader)**.
Es kombiniert einen SMMA-Filter (Smoothed Moving Average) mit dem Average Directional Index (ADX).
sodass Trades nur dann getätigt werden, wenn der Trend durch einen Crossover bestätigt und stark genug ist
gemäß ADX. Der Port behält das asymmetrische Risikomanagement des ursprünglichen Roboters bei:
Long-Positionen nutzen große Take-Profit- und Trailing-Distanzen, während Short-Trades engere Positionen nutzen
Ziele und Schutz.

## Handelslogik

1. Erstellen Sie einen geglätteten gleitenden Durchschnitt auf den mittleren Kerzenpreisen und einen ADX mit den konfigurierten Zeiträumen.
2. Bewerten Sie Signale nur bei geschlossenen Kerzen, um die MQL4-Logik (`iClose(...,1)` / `iClose(...,2)`) nachzuahmen.
3. Geben Sie Long ein, wenn die vorherige Kerze über dem SMMA schließt, und die Kerze, bevor sie unter dem SMMA schließt
Derselbe SMMA-Wert und der vorherige ADX-Messwert liegt über dem Schwellenwert.
4. Geben Sie Short ein, wenn die vorherige Kerze unter dem SMMA schließt, die Kerze, bevor sie über dem schließt
derselbe SMMA-Wert und ADX liegt über dem Schwellenwert.
5. Sobald die Ausgänge in Position sind, werden sie gesteuert durch:
   - Umkehrung des gleitenden Durchschnitts in die entgegengesetzte Richtung.
   - Individuelle Stop-Loss- und Take-Profit-Level, gemessen in Pips.
   - Optionale Trailing-Stop-Distanzen, die sich zu Gunsten des Handels auswirken.

Alle Preisversätze werden mithilfe der Preisstufe des Wertpapiers aus Pips umgerechnet. Wenn das Instrument dies nicht tut
Wenn Sie einen gültigen Schritt melden, wird ein Wert von 1 als sicherer Fallback verwendet.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `SMMA Period` | Länge des geglätteten gleitenden Durchschnitts (Standard 21). |
| `ADX Period` | Länge des durchschnittlichen Richtungsindex (Standard 14). |
| `ADX Threshold` | Mindestwert ADX erforderlich, um Einträge zuzulassen (Standard 16). |
| `Long Take Profit (pips)` | Take-Profit-Distanz für Kaufpositionen (Standard 1300 Pips). |
| `Long Stop Loss (pips)` | Stop-Loss-Distanz für Kaufpositionen (Standard 30 Pips). |
| `Long Trailing Stop (pips)` | Trailing-Stop-Distanz für Kaufpositionen (Standard 270 Pips). |
| `Short Take Profit (pips)` | Take-Profit-Distanz für Verkaufspositionen (Standard 160 Pips). |
| `Short Stop Loss (pips)` | Stop-Loss-Distanz für Verkaufspositionen (Standard 50 Pips). |
| `Short Trailing Stop (pips)` | Trailing-Stop-Distanz für Verkaufspositionen (Standard 20 Pips). |
| `Volume` | Für Neueingaben verwendetes Bestellvolumen (Standard 0,1). |
| `Candle Type` | Primäre Kerzenserie für Berechnungen (Standardzeitrahmen 1 Minute). |

Alle Parameter stehen zur Optimierung zur Verfügung. Die Standardeinstellungen entsprechen den ursprünglichen EA-Einstellungen.

## Notizen

- Trailing-Stops werden erst aktiviert, wenn sich der Preis mindestens um die konfigurierte Entfernung vom Einstiegspunkt bewegt.
- Gegensätzliche Signale schließen die aktive Position, bevor sie eine neue eröffnen.
- Die Strategie zeichnet automatisch Kerzen, Indikatoren und eigene Trades auf dem Chart ein, sofern ein Chartbereich verfügbar ist.
- Für diesen Port gibt es keine automatisierten Tests; Verwenden Sie manuelles Backtesting, um das Verhalten Ihrer Instrumente zu validieren.
