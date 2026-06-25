# Exp Trendintensitätsindex Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Konvertierung des MetaTrader-Experten **Exp_Trend_Intensity_Index**. Sie handelt auf abgeschlossenen Kerzen eines konfigurierbaren Zeitrahmens und verwendet den Trend Intensity Index (TII), um zu erkennen, wenn Momentum extreme bullische oder bärische Zonen verlässt. Wenn der Indikator aus einer oberen Zone herausgeht, schließt der Algorithmus Shorts und kann einen neuen Long starten. Wenn der Indikator eine untere Zone verlässt, schließt der Algorithmus Longs und kann einen neuen Short starten.

## Wie der Indikator aufgebaut wird

1. Die Preisquelle auswählen (close, open, gewichtete Varianten, Trend-Follow-Preise, etc.).
2. Diesen Preisstrom mit einem ersten gleitenden Durchschnitt glätten (`PriceMaMethod`, `PriceMaLength`).
3. Den Unterschied zwischen Preis und geglättetem Wert in positive und negative Flüsse aufteilen.
4. Die positiven und negativen Flüsse unabhängig mit einem zweiten gleitenden Durchschnitt glätten (`SmoothingMethod`, `SmoothingLength`).
5. Den Trend Intensity Index berechnen: `TII = 100 * Positive / (Positive + Negative)`.
6. Das Ergebnis mit den Schwellenwerten `HighLevel` und `LowLevel` vergleichen, um einen Farbzustand zuzuweisen: hohe Zone (`0`), neutral (`1`) oder niedrige Zone (`2`).

Die Implementierung verwendet StockSharp-Gleitende Durchschnitte (einfach, exponentiell, geglättet, gewichtet). Erweiterte Glättungstypen aus der ursprünglichen MQL-Bibliothek sind in diesem Port nicht verfügbar.

## Handelslogik

* Signale werden nur verarbeitet, wenn eine Kerze vollständig abgeschlossen ist (`CandleStates.Finished`).
* Der Parameter `SignalBar` definiert, welcher abgeschlossene Balken analysiert wird (Standard: ein Balken zurück). Die Strategie inspiziert auch den unmittelbar davor liegenden Balken, entsprechend der doppelten Puffer-Suche im MQL-Code.
* Wenn der ältere Balken zur hohen Zone gehört (`color == 0`):
  * Jede Short-Position schließen, wenn `EnableSellExits` wahr ist.
  * Wenn der neuere Balken die hohe Zone verlassen hat und `EnableBuyEntries` wahr ist, eine Long-Position eröffnen oder umkehren.
* Wenn der ältere Balken zur niedrigen Zone gehört (`color == 2`):
  * Jede Long-Position schließen, wenn `EnableBuyExits` wahr ist.
  * Wenn der neuere Balken die niedrige Zone verlassen hat und `EnableSellEntries` wahr ist, eine Short-Position eröffnen oder umkehren.
* Orders werden mit `BuyMarket` und `SellMarket` eingereicht. Positionsumkehrungen verwenden das aktuelle Positionsvolumen plus die konfigurierte `Volume`-Eigenschaft.
* Optionaler Stop-Loss und Take-Profit-Schutz (Preiseinheiten) wird über `StopLossPoints` und `TakeProfitPoints` konfiguriert und mit `StartProtection` implementiert.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen für Indikatorberechnung und Handel. |
| `PriceMaMethod`, `PriceMaLength` | Gleitender Durchschnitt-Typ und Periode auf den Basispreisstrom angewendet. |
| `SmoothingMethod`, `SmoothingLength` | Gleitender Durchschnitt-Typ und Periode auf die positiven und negativen Flüsse angewendet. |
| `AppliedPrice` | Preisquelle für den Indikator (close, open, median, Trend-Follow-Varianten, Demark, etc.). |
| `HighLevel`, `LowLevel` | Obere und untere Schwellenwerte, die bullische und bärische Zonen definieren. |
| `SignalBar` | Anzahl abgeschlossener Balken für Signalbestätigung zurückzuschauen. |
| `EnableBuyEntries`, `EnableSellEntries` | Umschalter, die das Eröffnen von Long/Short-Positionen erlauben. |
| `EnableBuyExits`, `EnableSellExits` | Umschalter, die automatische Ausstiege erlauben, wenn der Indikator wechselt. |
| `StopLossPoints`, `TakeProfitPoints` | Optionale Schutzabstände in Preiseinheiten für `StartProtection`. |

## Unterschiede zum ursprünglichen MQL-Experten

* Money-Management-Optionen (`MM`, `MMMode`, `Deviation`) werden durch StockSharp's Standard-Volumen-Eigenschaft und Order-Ausführung ersetzt; Slippage-Management wird nicht repliziert.
* Nur die in StockSharp verfügbaren Typen von gleitenden Durchschnitten (einfach, exponentiell, geglättet, gewichtet) werden unterstützt.
* Phasenparameter aus dem MQL-Indikator werden weggelassen, da StockSharp-Indikatoren keine äquivalenten Steuerungen exponieren.
* Orders werden sofort nach Signalbestätigung auf der abgeschlossenen Kerze ausgeführt; es gibt keine explizite Planung für die nächste Balkeneröffnung.

Diese Änderungen halten die Handelsidee intakt und folgen dabei den StockSharp High-Level-Strategie-Richtlinien.
