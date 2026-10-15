# ATR-Normalisiertes-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die ATR-Normalisiertes-Histogramm-Strategie reproduziert das Verhalten des MetaTrader-Experten *Exp_ATR_Normalize_Histogram* innerhalb von StockSharp. Das System beobachtet das normalisierte Verhältnis zwischen dem geglätteten Abstand vom Schlusskurs zum Tief und dem geglätteten True Range. Farbänderungen des Histogramms steuern sowohl Ein- als auch Ausstiege und emulieren die Multi-Buffer-Logik der ursprünglichen MQL5-Implementierung.

## Indikatorberechnung
1. Für jede abgeschlossene Kerze berechnet die Strategie:
   - `diff = Close − Low`.
   - `range = max(High, vorheriger Close) − min(Low, vorheriger Close)`.
2. Jede Reihe wird unabhängig mit den gewählten Methoden und Längen geglättet. Fünf Methoden sind verfügbar: Simple, Exponential, Smoothed (RMA), Weighted und Jurik. Nicht unterstützte MQL-Methoden (JurX, Parabolic, T3, VIDYA, AMA) fallen auf den einfachen gleitenden Durchschnitt zurück.
3. Der normalisierte Histogrammwert wird berechnet als

   `normalized = 100 × smoothedDiff / max(|smoothedRange|, PriceStep)`.
4. Schwellenwerte unterteilen das Histogramm in fünf Bänder. Übergänge zwischen Bändern spiegeln den Farb-Buffer wider, der vom MQL-Indikator erzeugt wird.

## Signallogik
- **Einstiegsfilter** – `SignalBar` wählt aus, welcher historische Balken ausgewertet werden soll (Standard 1, der zuletzt geschlossene Balken). Die Strategie vergleicht die Farbe dieses Balkens mit dem vorherigen:
  - Ein Übergang vom bullischen Extrem (Farbe `0`) zu einer anderen Farbe eröffnet eine Long-Position, wenn Long-Trades aktiviert sind.
  - Ein Übergang vom bärischen Extrem (Farbe `4`) zu einer anderen Farbe eröffnet eine Short-Position, wenn Short-Trades aktiviert sind.
- **Ausstiegsfilter** – die Farbe des vorherigen Balkens allein reicht aus, um Positionen zu schließen:
  - Farbe `0` schließt Short-Positionen, wenn Short-Exits aktiviert sind.
  - Farbe `4` schließt Long-Positionen, wenn Long-Exits aktiviert sind.
- Ausstiege werden vor neuen Einstiegen verarbeitet, damit die Strategie niemals überlappende Trades hält.

## Risikomanagement
Die Strategie verfolgt den letzten Ausführungspreis und erzwingt optional Schutz-Stops und Ziele in Instrumentpunkten. Die Konvertierung verwendet `Security.PriceStep` und entspricht damit dem "Punkte"-Konzept des ursprünglichen Experten. Wenn eines der Limits innerhalb der Kerze erreicht wird, wird die Position sofort geschlossen und die Handelsrichtung kann beim folgenden Signal wechseln.

## Parameter
- `CandleType` – Zeitrahmen für die Berechnung.
- `FirstSmoothingMethod` / `SecondSmoothingMethod` – Glättungstyp für `diff`- und `range`-Streams.
- `FirstLength` / `SecondLength` – Perioden für die Glätter.
- `HighLevel`, `MiddleLevel`, `LowLevel` – Histogramm-Schwellenwerte (Standard 60/50/40).
- `SignalBar` – Versatz für die Buffer-Auswertung (Minimum 1).
- `EnableBuyEntries`, `EnableSellEntries`, `EnableBuyExits`, `EnableSellExits` – Schalter zur Verwaltung der vier Handelsrichtungen.
- `TradeVolume` – Basis-Ordergröße. Die Strategie gleicht vorhandene Exposition automatisch aus, wenn die Richtung gewechselt wird.
- `StopLossPoints`, `TakeProfitPoints` – optionale Schutzabstände in Punkten; auf null setzen, um zu deaktivieren.

## Hinweise und Unterschiede zur MQL-Version
- Beide Glättungsstufen sind unabhängig konfigurierbar, aber nur die fünf StockSharp-Implementierungen für gleitende Durchschnitte sind verfügbar. Wenn eine andere MQL-Methode gewählt wird, verwendet die Strategie den einfachen gleitenden Durchschnitt mit unveränderter Länge.
- Die `SignalBar`-Logik folgt dem Buffer-Versatz aus `CopyBuffer`, sodass größere Versatze den gewählten Balken weiterhin mit seinem unmittelbaren Vorgänger vergleichen.
- Die Geldmanagement-Parameter des ursprünglichen Experten (`MM`, `MMMode`, `Deviation`) werden auf einen einzigen `TradeVolume`-Parameter vereinfacht. Die Orderausführung erfolgt zum Marktpreis mit optionalem Stop/Ziel-Monitoring.
