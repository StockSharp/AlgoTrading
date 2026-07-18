# Diff TF MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters "Diff_TF_MA_EA".
- Handelssignale entstehen durch den Vergleich eines einfachen gleitenden Durchschnitts, der auf einem höheren Zeitrahmen berechnet wird, mit einem anderen gleitenden Durchschnitt, der auf den Handelszeitrahmen umskaliert wird.
- Der Code behält nur abgeschlossene Kerzen, spiegelt die ursprünglichen Überkreuzungsregeln wider und schließt jede entgegengesetzte Exposition, bevor eine neue Position eröffnet wird.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `MaPeriod` | Länge des einfachen gleitenden Durchschnitts, der auf dem höheren Zeitrahmen berechnet wird. |
| `CandleType` | Handelszeitrahmen für die Auftragsgenerierung. |
| `HigherCandleType` | Höherer Zeitrahmen, der den Referenz-gleitenden Durchschnitt liefert. |
| `ReverseSignals` | Kehrt die Überkreuzungsregeln um (Kauf bei bärischem Kreuz und Verkauf bei bullischem Kreuz). |
| `Volume` | Strategievolumen für `BuyMarket`/`SellMarket`-Aufrufe (über die `Strategy.Volume`-Eigenschaft gesetzt). |

## Handelslogik
1. Sowohl den Handelszeitrahmen (`CandleType`) als auch den höheren Zeitrahmen (`HigherCandleType`) abonnieren.
2. Einen einfachen gleitenden Durchschnitt mit Länge `MaPeriod` auf dem höheren Zeitrahmen erstellen.
3. Die Länge des höheren Zeitrahmens in den Handelszeitrahmen umrechnen, indem mit dem Verhältnis der Zeitrahmen-Dauern multipliziert wird, und einen weiteren gleitenden Durchschnitt auf den Handelskerzen berechnen.
4. Die letzten zwei abgeschlossenen Werte für beide gleitende Durchschnitte speichern und bei jeder abgeschlossenen Handelskerze auf Kreuzungen prüfen.
5. Eine Long-Position eröffnen oder umkehren, wenn der höhere Zeitrahmen-MA den Handels-MA von unten nach oben kreuzt (es sei denn, `ReverseSignals` ist `true`).
6. Eine Short-Position eröffnen oder umkehren, wenn der höhere Zeitrahmen-MA den Handels-MA von oben nach unten kreuzt (es sei denn, `ReverseSignals` ist `true`).
7. Positionen werden abgeflacht und gedreht, indem genug Volumen gesendet wird, um jede bestehende Exposition zu kompensieren.

## Verwendungshinweise
- Kompatible Zeitrahmen wählen: der höhere Zeitrahmen sollte in der Regel größer als der Handelszeitrahmen sein, damit die umskalierte Länge sinnvoll ist.
- Das Standardvolumen ist `1`. `Strategy.Volume` vor dem Start der Strategie anpassen, wenn eine andere Größe benötigt wird.
- Stops und Take-Profits der MetaTrader-Version werden nicht reproduziert; das Risikomanagement kann bei Bedarf über StockSharp-Schutzfunktionen angehängt werden.
- Wenn `ReverseSignals` aktiviert ist, werden bullische und bärische Aktionen vertauscht, während der Rest der Logik unverändert bleibt.
