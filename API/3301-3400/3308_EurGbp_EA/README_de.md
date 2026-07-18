# EurGbp-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die EurGbp-EA-Strategie spiegelt den ursprünglichen MetaTrader-Expert-Advisor wider, indem sie das stündliche MACD-Momentum von EUR/USD und GBP/USD vergleicht, während sie auf dem konfigurierten Primärinstrument handelt (typischerweise EUR/GBP). Der Ansatz nutzt relative Stärke zwischen Euro- und Pfund-Majors, um Bewegungen im Kreuzpaar vorwegzunehmen.

## Indikatoren
* **MACD (12, 26, 9)** auf EUR/USD (Signal und Histogramm).
* **MACD (12, 26, 9)** auf GBP/USD (Signal und Histogramm).

Beide Indikatoren werden auf demselben Zeitrahmen ausgewertet, der über den Parameter `Candle Type` gewählt wird (Standard ist 1 Stunde).

## Handelslogik
1. Kerzen für das Handelsinstrument sowie EUR/USD und GBP/USD abonnieren.
2. MACD-Signal und -Histogramm für beide Referenzpaare berechnen.
3. **Kaufbedingung:**
   * EUR/USD-Histogramm &lt; GBP/USD-Histogramm, **und**
   * EUR/USD-Signal &gt; GBP/USD-Signal,
   * Keine bestehende Long-Position (oder eine bestehende Short-Position, die glattgestellt wird).
4. **Verkaufsbedingung:**
   * GBP/USD-Histogramm &lt; EUR/USD-Histogramm, **und**
   * GBP/USD-Signal &gt; EUR/USD-Signal,
   * Keine bestehende Short-Position (oder eine bestehende Long-Position, die glattgestellt wird).
5. Pro Bar ist in jede Richtung nur ein Trade erlaubt, um doppelte Einstiege zu vermeiden.
6. Stop-Loss- und Take-Profit-Orders werden unmittelbar nach dem Einstieg mit den konfigurierten Punktdistanzen angefügt.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| Candle Type | Zeitrahmen für alle Kerzenabonnements. | 1 Stunde |
| EURUSD Security | Instrument, das EUR/USD-Kerzen liefert. | Muss gesetzt werden |
| GBPUSD Security | Instrument, das GBP/USD-Kerzen liefert. | Muss gesetzt werden |
| Volume | Ordervolumen (Lots). | 0.01 |
| Stop Loss | Schutzstop in Preisschritten. | 75 |
| Take Profit | Gewinnziel in Preisschritten. | 46 |

## Risikomanagement
* `Stop Loss` und `Take Profit` werden in Preisschritten des gehandelten Instruments gemessen. Stellen Sie sicher, dass das Instrument einen gültigen `PriceStep`-Wert hat.
* Der Schutz startet automatisch beim Start der Strategie (`StartProtection`).
* Ist eine der Distanzen null, wird die jeweilige Schutzorder übersprungen.

## Nutzungshinweise
* Weisen Sie der Strategieinstanz vor dem Start das Haupthandelsinstrument zu (zum Beispiel EUR/GBP).
* Konfigurieren Sie `EURUSD Security` und `GBPUSD Security`, sodass sie auf verfügbare Datenquellen innerhalb Ihrer Verbindung verweisen.
* Die Strategie benötigt synchronisierte Daten für alle drei Instrumente auf dem gewählten Zeitrahmen, um zuverlässig Signale zu erzeugen.
* Es werden nur Marktorders verwendet. Bestehende Gegenpositionen werden durch Senden des inversen Ordervolumens geschlossen.

## Konvertierungshinweise
* Die ursprünglichen Eingaben `_Lots`, `_SL`, `_TP`, `_MagicNumber`, `_Comment`, `_OnlyOneOpenedPos` und `_AutoDigits` werden auf StockSharp-Parameter oder integriertes Verhalten abgebildet.
* Hilfsroutinen zum Schließen von Orders aus der MQL-Version werden durch StockSharps High-Level-Verwaltung von Schutzorders ersetzt.
* Fehlerbehandlung und Retry-Schleifen des MQL-Codes werden ausgelassen, weil das StockSharp-Ausführungsmodell Orderzustände und Wiederholungen bereits verwaltet.
