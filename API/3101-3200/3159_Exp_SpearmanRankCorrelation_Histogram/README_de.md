# Exp Spearman Rank Correlation Histogram-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie portiert den MetaTrader-Experten **Exp_SpearmanRankCorrelation_Histogram**. Sie abonniert eine konfigurierbare Kerzenserie, berechnet das Spearman-Rang-Korrelations-Histogramm für jede abgeschlossene Kerze und reagiert wenn sich der farbcodierte Zustand ändert. Je nach Handelsmodus kann der Algorithmus entgegengesetzte Positionen schließen, in einen neuen Trade umkehren oder auf Extremwerte warten bevor er handelt.

## Indikator-Pipeline

1. Ein `RankCorrelationIndex`-Indikator (Spearman-Rang-Korrelation skaliert auf ±100) wird mit Kerzenschlusskursen gespeist. Das Lookback-Fenster ist durch `MaxRange` begrenzt und beträgt standardmäßig 14 Kerzen.
2. Die rohe Korrelation wird auf das Intervall `[-1, 1]` normiert. Wenn `InvertCorrelation` aktiviert ist, wird das Vorzeichen umgekehrt um das MQL-`direction`-Flag zu emulieren.
3. Der normierte Wert wird mit `HighLevel` und `LowLevel` verglichen um einen Farbzustand zuzuweisen:
   * `4` – stark bullische Zone (`value > HighLevel`).
   * `3` – moderat bullische Zone (`0 < value ≤ HighLevel`).
   * `2` – neutral (`value == 0`).
   * `1` – moderat bearische Zone (`LowLevel ≤ value < 0`).
   * `0` – stark bearische Zone (`value < LowLevel`).
4. Die neuesten Farben werden in einem serienartigen Puffer gespeichert, sodass Index `0` die zuletzt geschlossene Kerze darstellt, Index `1` die vorherige, und so weiter.

## Handels-Workflow

* Signale werden nur auf abgeschlossenen Kerzen ausgewertet (`CandleStates.Finished`).
* Der Parameter `SignalBar` definiert welche abgeschlossene Kerze inspiziert wird (Standard eine Kerze zurück). Die Strategie betrachtet auch die unmittelbar ältere Kerze und repliziert damit die Doppelpuffer-Suche des Expert Advisors.
* Order-Schalter (`AllowBuyEntries`, `AllowSellEntries`, `AllowBuyExits`, `AllowSellExits`) entscheiden ob Long-/Short-Positionen geöffnet oder geschlossen werden dürfen.
* Handelsmodi reproduzieren den MetaTrader-Schalter:
  * **Modus 1** – entgegengesetzte Position schließen sobald die ältere Farbe bullisch/bearisch ist (`> 2` oder `< 2`). Falls erlaubt in die neue Richtung öffnen wenn die neuere Farbe die bullische (`< 3`) oder bearische (`> 1`) Zone verlässt.
  * **Modus 2** – nur auf Extremfarben reagieren. Bullisches Extrem (`4`) ermöglicht der Strategie Shorts zu schließen und optional Longs zu öffnen wenn die neuere Kerze unter `4` fällt. Bearisches Extrem (`0`) schließt Longs und kann Shorts öffnen wenn die neuere Kerze über `0` steigt.
  * **Modus 3** – eine strengere Version von Modus 2: Shorts werden sofort auf `4` geschlossen, Longs auf `0`, und neue Trades sind unter denselben Bedingungen wie Modus 2 erlaubt.
* `CancelActiveOrders()` wird vor dem Senden neuer Marktorders ausgeführt um veraltete Anfragen zu vermeiden.
* Positionsumkehrungen verwenden das konfigurierte `Volume` plus die absolute aktuelle Position damit der Trade vollständig auf die entgegengesetzte Seite wechselt.
* Optionale `StopLossPoints` und `TakeProfitPoints` (Preiseinheiten) ermöglichen `StartProtection`-basiertes Risikomanagement; bei `0` werden keine Schutzorders erzeugt.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen für Indikator und Handelsentscheidungen. |
| `RangeLength` | Nominaler Spearman-Lookback-Zeitraum (begrenzt durch `MaxRange`). |
| `MaxRange` | Obergrenze für die effektive Lookback-Länge; fällt auf `10` wenn auf `0` gesetzt. |
| `HighLevel`, `LowLevel` | Schwellenwerte die bullische und bearische Histogramm-Zonen trennen. |
| `SignalBar` | Anzahl geschlossener Kerzen die vor der Histogramm-Analyse übersprungen werden. |
| `InvertCorrelation` | Kehrt das Histogrammvorzeichen um um das MQL-`direction=false`-Verhalten zu entsprechen. |
| `AllowBuyEntries`, `AllowSellEntries` | Long-/Short-Positionen öffnen aktivieren. |
| `AllowBuyExits`, `AllowSellExits` | Automatisches Schließen bestehender Long-/Short-Positionen aktivieren. |
| `TradeMode` | Wählt Modus 1-, Modus 2- oder Modus 3-Logik aus dem Original-Experten. |
| `StopLossPoints`, `TakeProfitPoints` | Optionale Schutzabstände in absoluten Preiseinheiten für `StartProtection`. |
| `Volume` (eingebaut) | Basis-Ordergröße beim Öffnen oder Umkehren von Positionen. |

## Unterschiede zum MetaTrader-Experten

* Geldverwaltungseingaben (`MM`, `MMMode`) und Slippage (`Deviation_`) werden nicht repliziert; die Positionsgrößenbestimmung basiert auf der Standard-`Volume`-Eigenschaft und der Broker-Konfiguration.
* Die MQL-Hilfsfunktionen aus `TradeAlgorithms.mqh` werden durch direkte `BuyMarket`/`SellMarket`-Aufrufe nach dem Stornieren ausstehender Orders ersetzt.
* Der `CalculatedBars`-Performance-Hinweis ist in StockSharp unnötig und wurde weggelassen.
* Das `direction`-Flag wird durch `InvertCorrelation` dargestellt, das einfach das Histogrammvorzeichen spiegelt.
* Stop-Loss- und Take-Profit-Abstände (`StopLoss_`, `TakeProfit_`) werden als absolute Preisoffsets beim Aktivieren von `StartProtection` interpretiert; keine automatische Punkt-zu-Preis-Konvertierung wird durchgeführt.
* Signalzeiten werden beim Kerzenschluss verarbeitet; es gibt keine verzögerte Planung bis zur nächsten Kerzeneröffnung.

Diese Anpassungen folgen den StockSharp High-Level-Strategie-Richtlinien während die ursprüngliche Signallogik erhalten bleibt.
