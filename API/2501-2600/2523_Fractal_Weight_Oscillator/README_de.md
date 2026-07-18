# Fraktaler Gewichts-Oszillator Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den "Exp_Fractal_WeightOscillator"-Expertenberater durch Aggregation von vier Oszillatoren (RSI, Money Flow Index, Williams %R und DeMarker) in ein einzelnes geglättetes zusammengesetztes Signal. Der Oszillator wird mit zwei horizontalen Niveaus (`HighLevel`/`LowLevel`) verglichen, um Long- oder Short-Trades im Trendfolge- oder Gegentrend-Modus auszulösen. Alle Berechnungen werden auf dem ausgewählten Kerzen-Zeitrahmen durchgeführt und nutzen die Standard-StockSharp-High-Level-API.

## Indikator-Stack
- **Relative Stärke Index** – angewendet auf die konfigurierte Preisquelle.
- **Money Flow Index** – berechnet aus dem gewählten angewendeten Preis und dem Kerzenvolumen.
- **Williams %R** – berechnet aus Kerzen-Hoch/Tief/Schlusskurswerten.
- **DeMarker** – aus Kerzen-Hochs und -Tiefs mit einem einfachen Durchschnittsglätter neu erstellt.
- **Gleitender Durchschnitts-Glätter** – optionale Nachbearbeitung der gewichteten Summe (SMA, EMA, SMMA oder LWMA).

Der zusammengesetzte Oszillatorwert ist ein gewichteter Durchschnitt der vier Komponenten. `HighLevel` und `LowLevel` definieren Überkauft-/Überverkauft-Zonen. `SignalBar` steuert, wie viele abgeschlossene Bars bei der Suche nach einem Kreuzungspunkt inspiziert werden, sodass Sie die Ausführung relativ zur neuesten fertigen Kerze verzögern können.

## Handelslogik
### TrendMode = Direct
- **Long-Einstieg / Short-Ausstieg** – wenn der Oszillator von über `LowLevel` auf unter oder gleich `LowLevel` fällt (`BuyOpenEnabled` und `SellCloseEnabled` müssen wahr sein).
- **Short-Einstieg / Long-Ausstieg** – wenn der Oszillator von unter `HighLevel` auf über oder gleich `HighLevel` steigt (`SellOpenEnabled` und `BuyCloseEnabled` müssen wahr sein).

### TrendMode = Counter
- **Long-Einstieg / Short-Ausstieg** – ausgelöst durch einen Aufwärtsausbruch von `HighLevel`.
- **Short-Einstieg / Long-Ausstieg** – ausgelöst durch einen Abwärtsausbruch von `LowLevel`.

Signale werden auf dem durch `SignalBar` angegebenen Bar ausgewertet. Positionsumkehrungen verwenden `Volumen + |Position|`, um jede bestehende Exposure zu neutralisieren.

## Risikomanagement
Wenn eine neue Position eröffnet wird, berechnet die Strategie feste Preisstopp-Loss- und Take-Profit-Niveaus mit `StopLossPoints` und `TakeProfitPoints`. Die Werte werden mit dem `MinPriceStep` des Instruments multipliziert. Bei jeder abgeschlossenen Kerze werden Tief/Hoch gegen diese Ziele geprüft; wenn getroffen, wird die Position sofort geschlossen und interne Risikoverfolgungsparameter werden zurückgesetzt.

## Parameter
| Name | Beschreibung |
| ---- | ------------ |
| `TrendMode` | Direktes (Trendfolge) oder Gegentrend-Verhalten auswählen. |
| `SignalBar` | Anzahl der geschlossenen Bars zurück für die Signalauswertung. |
| `Period` | Basislänge für RSI, MFI, Williams %R und DeMarker. |
| `SmoothingLength` | Fenster für den Gleitenden Durchschnittsgläter. |
| `SmoothingMethod` | Art des gleitenden Durchschnitts (`None`, `Sma`, `Ema`, `Smma`, `Lwma`). |
| `RsiPrice`, `MfiPrice` | Angewendete Preisquelle in Komponenten-Oszillatoren. |
| `MfiVolume` | Volumentyp für MFI (Tick und Real verwenden beide Kerzenvolumen). |
| `RsiWeight`, `MfiWeight`, `WprWeight`, `DeMarkerWeight` | Relative Gewichte im zusammengesetzten Oszillator. |
| `HighLevel`, `LowLevel` | Obere und untere Schwellenwerte für Niveaukreuzungen. |
| `BuyOpenEnabled`, `SellOpenEnabled` | Long- oder Short-Einstiege aktivieren. |
| `BuyCloseEnabled`, `SellCloseEnabled` | Schließen bestehender Positionen bei entgegengesetzten Signalen erlauben. |
| `StopLossPoints`, `TakeProfitPoints` | Schutzabstände in Preisschritten (0 deaktiviert das Niveau). |
| `CandleType` | Zeitrahmen der Kerzen, die an die Strategie übergeben werden. |
| `Volume` *(Strategie-Eigenschaft)* | Handelsgröße für Einstiege (Positionsumkehrungen addieren die absolute Position). |

## Verwendungshinweise
- `SignalBar = 1` reproduziert das ursprüngliche Expertenverhalten durch Verwendung des zuletzt vollständig geschlossenen Bars. Erhöhung des Wertes verzögert Reaktionen um zusätzliche Bars.
- `SmoothingMethod` ermöglicht das Deaktivieren der Glättung (`None`) oder die Anpassung an die verschiedenen in der MQL-Version verfügbaren Gleitenden-Durchschnitt-Stile.
- Die Money Flow Index-Implementierung arbeitet immer mit dem gesamten Kerzenvolumen, das vom Datenfeed geliefert wird. Beide `Tick`- und `Real`-Optionen beziehen sich daher auf denselben aggregierten Wert, da StockSharp-Kerzen standardmäßig keine separaten Tick-Zähler exponierten.
- Alle Kommentare im C#-Quellcode sind auf Englisch geschrieben, wie erforderlich.
