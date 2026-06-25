# XRSI DeMarker Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
Diese Strategie repliziert den Expertenberater **Exp_XRSIDeMarker_Histogram**. Sie handelt Umkehrungen, die von einem benutzerdefinierten Oszillator erkannt werden, der einen Relative Strength Index (RSI) mit dem DeMarker-Indikator kombiniert und das Ergebnis dann glättet. Das System kann Long- und Short-Trades unabhängig voneinander öffnen oder schließen, und optionale Schutz-Stops, ausgedrückt in Preisschritten, werden unterstützt.

## Indikatoraufbau
1. **Angewandter Preis** – der RSI wird auf dem ausgewählten Eingang (Schlusskurs, Eröffnung, Hoch, Tief, Median, typisch oder gewichtet) mit dem konfigurierten Zeitraum berechnet.
2. **DeMarker-Komponente** – für jede abgeschlossene Kerze misst die Strategie den Aufwärts- (`deMax`) und Abwärtsdruck (`deMin`):
   - `deMax = max(High_t - High_{t-1}, 0)`
   - `deMin = max(Low_{t-1} - Low_t, 0)`
   Beide Serien werden mit einem einfachen gleitenden Durchschnitt geglättet, dessen Länge dem RSI-Zeitraum entspricht.
   - `DeMarker = deMaxAvg / (deMaxAvg + deMinAvg)` (skaliert auf den Bereich 0–100).
3. **Zusammengesetzter Oszillator** – der Endwert ist `(RSI + 100 * DeMarker) / 2`.
4. **Glättung** – der zusammengesetzte Oszillator wird durch einen der unterstützten gleitenden Durchschnitte (SMA, EMA, SMMA, LWMA oder Jurik) geleitet. Wenn ein nicht unterstützter Glättungsmodus der ursprünglichen MQL-Version ausgewählt wird, fällt der Indikator auf eine EMA mit der angeforderten Länge zurück. Die Jurik-Option berücksichtigt auch den Phasenparameter.
5. **Signalhistorie** – die Strategie speichert historische Werte und wertet Signale auf der durch `SignalBar` definierten Balken aus, was das ursprüngliche EA nachahmt, das auf die nächste Kerze wartete, bevor Trades ausgeführt wurden.

## Handelslogik
- **Bullische Umkehr**
  - Bedingung: Wert bei `SignalBar+1` liegt unter `SignalBar+2` (Abwärtsneigung) und der Wert bei `SignalBar` dreht wieder nach oben (`>=`).
  - Aktionen:
    - Bestehende Short-Trades schließen, wenn `CloseShortOnLongSignal` wahr ist.
    - Einen neuen Long-Trade mit `TradeVolume` öffnen (plus die für eine Umkehr von einem Short benötigte Menge), wenn `AllowBuyEntries` aktiviert ist.
- **Bärische Umkehr**
  - Bedingung: Wert bei `SignalBar+1` liegt über `SignalBar+2` (Aufwärtsneigung) und der Wert bei `SignalBar` dreht nach unten (`<=`).
  - Aktionen:
    - Bestehende Long-Trades schließen, wenn `CloseLongOnShortSignal` wahr ist.
    - Einen neuen Short-Trade öffnen, wenn `AllowSellEntries` aktiviert ist.
- Signale werden ignoriert, bis der Indikator und die DeMarker-Komponenten vollständig gebildet sind, und Aufträge werden nur platziert, wenn die Strategie online ist und der Handel erlaubt ist.

## Risikomanagement
- `StopLossTicks` und `TakeProfitTicks` repräsentieren Abstände in **Preisschritten**. Die Strategie multipliziert diese Werte mit `Security.PriceStep` (auf `1` zurückgreifend, wenn der Instrumentenschritt unbekannt ist) und schließt die Position, wenn der Abstand innerhalb des Kerzenbereichs erreicht wird.
- Die Übergabe von `0` deaktiviert den jeweiligen Schutz.
- Der Parameter `TradeVolume` wird als Standard-Auftragsgröße verwendet und auch zur Berechnung von Umkehrungen (die entgegengesetzte Position wird geschlossen, bevor eine neue eröffnet wird).

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `TradeVolume` | Volumen beim Öffnen neuer Positionen. | `0.1` |
| `StopLossTicks` | Schutz-Stop in Preisschritten. | `1000` |
| `TakeProfitTicks` | Gewinnziel in Preisschritten. | `2000` |
| `AllowBuyEntries` | Long-Trades aktivieren/deaktivieren. | `true` |
| `AllowSellEntries` | Short-Trades aktivieren/deaktivieren. | `true` |
| `CloseLongOnShortSignal` | Longs schließen wenn ein Short-Signal erscheint. | `true` |
| `CloseShortOnLongSignal` | Shorts schließen wenn ein Long-Signal erscheint. | `true` |
| `CandleType` | Zeitrahmen für die Analyse (Standard 4-Stunden-Kerzen). | `H4` |
| `IndicatorPeriod` | Rückblick für RSI- und DeMarker-Komponenten. | `14` |
| `AppliedPriceSelection` | Angewandter Preis für die RSI-Berechnung. | `Close` |
| `SmoothingMethodSelection` | Gleitender Durchschnitt für die Glättung (SMA/EMA/SMMA/LWMA/Jurik/Adaptive). | `Sma` |
| `SmoothingLength` | Zeitraum des Glättungsdurchschnitts. | `5` |
| `SmoothingPhase` | Phasenargument für die Jurik-Glättung. | `15` |
| `SignalBar` | Anzahl der geschlossenen Balken zurück für die Signalauswertung. | `1` |

## Hinweise vs. Original-EA
- Geldverwaltungsmodi der MQL-Version (balancebasiert, freie-Margin-basiert, etc.) werden durch einen direkten `TradeVolume`-Parameter ersetzt.
- Auftragsslippage (`Deviation`) ist nicht erforderlich, da StockSharp Marktaufträge verwendet.
- Fortgeschrittene Glättungsalgorithmen (Parabolischer MA, T3, VIDYA, AMA) sind in StockSharp nicht verfügbar und werden über die `Adaptive`-Option zur EMA zugeordnet.
- Alle Kommentare im C#-Quellcode sind auf Englisch verfasst, und die Logik läuft nur auf abgeschlossenen Kerzen, genau wie die ursprüngliche Implementierung.
