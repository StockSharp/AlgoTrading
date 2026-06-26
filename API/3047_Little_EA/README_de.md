# Little EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Little EA ist ein ursprünglich für MetaTrader geschriebener gleitender Durchschnitt-Kreuzungs-Expert. Die Strategie beobachtet die durch den Parameter **OHLC bar index** ausgewählte Kerze und reagiert, wenn diese Kerze einen verschobenen gleitenden Durchschnitt von unten oder oben kreuzt. Die StockSharp-Portierung behält die ursprüngliche Multi-Entry-Idee bei, indem sie mehrere Tranchen pro Richtung erlaubt, während ein konfigurierbares maximales Exposure eingehalten wird.

## Handelslogik
1. Die konfigurierte Kerzenserie abonnieren und den ausgewählten gleitenden Durchschnittstyp mit der gewählten Preisquelle (Schluss, Eröffnung, Hoch, Tief, Median, Typisch oder Gewichtet) speisen.
2. Abgeschlossene Kerzen speichern, damit die Strategie die Kerze bei `OhlcBarIndex` referenzieren kann (der Standardwert `1` bedeutet die letzte vollständig abgeschlossene Kerze).
3. Das optionale `MaShift` anwenden, indem der gleitende Durchschnittswert von mehreren Balken zurück gelesen wird, um die visuelle Verschiebung von MetaTrader zu replizieren.
4. Wenn die Referenzkerze über dem verschobenen MA schließt, als bullisches Kreuz behandeln. Wenn sie darunter schließt, als bärisches Kreuz behandeln.
5. Bei einem bullischen Kreuz:
   - Wenn das netto short Exposure bereits dem konfigurierten Maximum entspricht, die gesamte Short-Position schließen.
   - Andernfalls, wenn das Long-Exposure noch unter dem Maximum liegt, eine `TradeVolume`-Tranche zur Long-Seite hinzufügen.
6. Bei einem bärischen Kreuz:
   - Wenn das Long-Exposure bereits dem Maximum entspricht, die gesamte Long-Position schließen.
   - Andernfalls, wenn das Short-Exposure unter dem Limit liegt, eine `TradeVolume`-Tranche zur Short-Seite hinzufügen.

Die Volumenbegrenzung emuliert das `Int_Max_Pos`-Limit des ursprünglichen Experts, während mit den Netto-Positionen von StockSharp gearbeitet wird.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-Minuten-Zeitrahmen | Primärer Zeitrahmen für Signale und Indikatorberechnungen. |
| `OhlcBarIndex` | `int` | `1` | Index der historischen Kerze für die Kreuzungserkennung (0 = aktuelle formende Kerze, 1 = letzte abgeschlossene Kerze). |
| `MaxPositionsPerSide` | `int` | `15` | Maximale Anzahl von `TradeVolume`-Tranchen, die pro Richtung akkumuliert werden können. |
| `MaPeriod` | `int` | `64` | Länge des gleitenden Durchschnitts. |
| `MaShift` | `int` | `0` | Anzahl der Balken, um die der MA beim Prüfen von Kreuzungen rückwärts verschoben wird. |
| `MaType` | `MovingAverageType` | `Smoothed` | Berechnungsmodus des gleitenden Durchschnitts (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `AppliedPriceType` | `Close` | Als Indikatoreingang verwendete Preisquelle. |
| `TradeVolume` | `decimal` | `1` | Ordervolumen, das mit jeder neuen Tranche gesendet wird. |

## Unterschiede zum ursprünglichen MetaTrader-Expert
- Das Geldmanagement ist vereinfacht: Nur Einträge mit festem Volumen werden unterstützt. Prozentbasiertes Risiko-Sizing aus dem ursprünglichen EA ist nicht implementiert.
- StockSharp arbeitet mit Netto-Positionen, sodass entgegengesetzte Positionen geschlossen werden, bevor neues Exposure akkumuliert wird. Das `MaxPositionsPerSide`-Limit wird auf das Netto-Exposure in Lots angewendet.
- Indikatorwerte und Kerzenhistorie werden über die High-Level-Kerzen-Abonnement-API verarbeitet, anstatt manuelle Pufferkopien zu erstellen.

## Verwendungstipps
- `TradeVolume` vor dem Start der Strategie an den Lotschritt des Instruments anpassen; der Konstruktor weist denselben Wert auch `Strategy.Volume` zu, damit Hilfsmethoden standardmäßig die gewünschte Größe verwenden.
- `MaShift` in Kombination mit `OhlcBarIndex` verwenden, um die visuelle Ausrichtung aus dem MetaTrader-Chart bei Bedarf zu recreieren.
- Die Strategie einem Chart hinzufügen, um Kerzen, die gleitende Durchschnittsüberlagerung und ausgeführte Trades zu sehen, was die Überprüfung des Kreuzungsverhaltens erleichtert.

## Indikatoren
- Ein konfigurierbarer gleitender Durchschnitt (`Simple`, `Exponential`, `Smoothed` oder `Weighted`).
