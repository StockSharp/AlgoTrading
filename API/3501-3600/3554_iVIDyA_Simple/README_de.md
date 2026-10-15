# iVIDyEine einfache Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine High-Level-StockSharp-Portierung des MetaTrader-Experten **„iVIDyA Simple“**. Es handelt ein einzelnes Symbol, indem es einen dynamischen variablen Indexdurchschnitt (VIDYA) verfolgt, der sich über den Chande Momentum Oscillator (CMO) an die Marktdynamik anpasst. Immer wenn die zuletzt beendete Kerze die verschobene VIDYA-Linie kreuzt, eröffnet die Strategie eine Marktposition in Richtung des Ausbruchs und fügt optional schützende Stop-Loss- und Take-Profit-Orders hinzu.

## Handelslogik
1. Kerzendaten werden aus dem konfigurierten Zeitrahmen (`CandleType`) gelesen.
2. Der CMO mit der Periode `CmoPeriod` ist an die Kerzenserie gebunden. Sein absoluter Wert skaliert dynamisch den Glättungsfaktor von VIDYA. Der Basisfaktor entspricht `2 / (EmaPeriod + 1)`, genau wie bei der ursprünglichen MQL-Implementierung.
3. Ein rollierender VIDYA-Wert wird beibehalten. Bei jeder fertigen Kerze der Algorithmus:
   - Wählt den angewendeten Preis (`AppliedPrice`) aus der Kerze aus (Schlusskurs, Eröffnungskurs, Mediankurs usw.).
   - Aktualisiert VIDYA mit dem adaptiven Glättungskoeffizienten.
   - Speichert historische Werte, um die Option `MA shift` von MetaTrader zu emulieren.
4. Die Kerze wird mit dem verschobenen VIDYA-Wert (`MaShift` Balken zurück) verglichen:
   - Wenn die Kerze unterhalb von VIDYA öffnet und darüber schließt, wird ein **Kaufsignal** generiert.
   - Wenn die Kerze über VIDYA öffnet und darunter schließt, wird ein **Verkaufssignal** generiert.
5. Vor der Eröffnung einer neuen Position glättet die Strategie jegliches gegenteilige Risiko, indem sie das gesamte für die Umkehrung erforderliche Volumen handelt.
6. Nach jeder Eingabe werden `SetStopLoss` und `SetTakeProfit` aufgerufen, wenn die jeweiligen Abstände positiv sind.

Dies spiegelt den ursprünglichen Expertenberater wider, der Aufträge ausschließlich für neue Balken auslöste, einen VIDYA verwendete, der aus CMO und EMA-Zeiträumen berechnet wurde, und optionale Stopps in Punkten anfügte.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `Volume` | `1` | Basishandelsvolumen, das für Aufträge verwendet wird. Bei der Umkehrung von Positionen wird das bestehende Exposure automatisch saldiert. |
| `StopLossPoints` | `150` | Stop-Loss-Distanz in Preisschritten. Zum Deaktivieren auf `0` setzen. |
| `TakeProfitPoints` | `460` | Take-Profit-Distanz in Preisschritten. Zum Deaktivieren auf `0` setzen. |
| `CmoPeriod` | `15` | Länge des Chande-Momentum-Oszillators, der das adaptive VIDYA-Gewicht bestimmt. |
| `EmaPeriod` | `12` | EMA Länge, die den Basisglättungskoeffizienten in der VIDYA-Formel definiert. |
| `MaShift` | `1` | Anzahl der abgeschlossenen Kerzen, die verwendet werden, um die VIDYA-Linie nach vorne zu verschieben, entsprechend der Eingabe `ma_shift` des Indikators MetaTrader. |
| `AppliedPrice` | `Close` | An die VIDYA-Berechnung übergebene Preisquelle (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `CandleType` | `TimeSpan.FromMinutes(5)` | Kerzentyp und Zeitrahmen, die für alle Berechnungen und Signale verwendet werden. |

## Zusätzliche Hinweise
- Schutzanordnungen werden über den integrierten High-Level-API (`SetStopLoss`/`SetTakeProfit`) verwaltet, während der ursprüngliche MQL-Code manuelle Prüfungen anhand der Einfrierstufen durchführte.
- Die Strategie abonniert nur fertige Kerzen und repliziert die Ausführungsbeschränkung „neuer Balken“ von MetaTrader.
- Der VIDYA-Verlauf wird automatisch gekürzt, sodass der Speicherbedarf auch dann gering bleibt, wenn `MaShift` groß ist.
- Alle Kommentare im Code sind auf Englisch verfasst, um den Projektanforderungen zu entsprechen.
