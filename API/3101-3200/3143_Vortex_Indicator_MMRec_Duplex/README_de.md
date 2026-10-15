# Vortex Indicator MMRec Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertiert vom MetaTrader 5-Experten **Exp_VortexIndicator_MMRec_Duplex.mq5** (MQL ID 23180).
- Pflegt zwei unabhängige Vortex-Indikatorströme: einen für Long-Trades und einen für Short-Trades. Jeder Strom hat seinen eigenen Zeitrahmen, Länge und Balkenverschiebung, sodass die bullische und bärische Logik separat optimiert werden kann.
- Repliziert das "MMRec" Geldverwaltungs-Recovery-Modul des originalen EA. Die Strategie verfolgt die letzten Handelsergebnisse pro Richtung und wechselt vorübergehend zu einer reduzierten Ordergröße nach einer konfigurierbaren Anzahl von Verlusten.

## Signallogik
1. Den konfigurierten Kerzentyp für jeden Strom abonnieren und den Vortex-Indikator (`VI+` und `VI-`) berechnen.
2. **Long-Einstiege:** wenn die vorherige Kerze `VI+` unterhalb oder gleich `VI-` hatte und die aktuelle Kerze mit `VI+` oberhalb von `VI-` schließt (bullisches Crossover). Einstiege sind nur erlaubt, wenn `AllowLongEntries` aktiviert ist.
3. **Long-Ausstiege:** wenn `VI-` auf der bewerteten Kerze über `VI+` steigt, sofern `AllowLongExits` aktiviert ist.
4. **Short-Einstiege:** wenn die vorherige Kerze `VI+` oberhalb oder gleich `VI-` hatte und die aktuelle Kerze mit `VI+` unterhalb von `VI-` schließt (bärisches Crossover), gesteuert durch `AllowShortEntries`.
5. **Short-Ausstiege:** wenn `VI+` auf der bewerteten Kerze wieder über `VI-` klettert, gesteuert durch `AllowShortExits`.
6. Jede Richtung behält ihre eigenen Stop-Loss- und Take-Profit-Niveaus, gemessen in Preisschritten. Das Erreichen eines der beiden Niveaus schließt die Position sofort und registriert das Ergebnis für die Recovery-Zähler.

## Geldverwaltungs-Recovery
- Der originale EA prüft ein gleitendes Fenster vergangener Trades, um zu entscheiden, ob die nächste Order das normale oder reduzierte Volumen verwenden soll. Dieser Port spiegelt dasselbe Verhalten wider.
- Für Long-Trades speichert die Warteschlange bis zu `LongTotalTrigger` neueste PnL-Ergebnisse. Wenn mindestens `LongLossTrigger` davon Verlust-Trades sind, verwendet der nächste Long-Einstieg `LongSmallMoneyManagement`; andernfalls wird `LongMoneyManagement` verwendet.
- Short-Trades wiederholen dieselbe Logik mit `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement` und `ShortMoneyManagement`.
- Wenn die Triggerwerte null sind, werden die Warteschlangen geleert und das Basisvolumen wird immer verwendet.

## Margin-Modi
`MarginModeOption` beschreibt, wie der Geldverwaltungswert in ein ausführbares Volumen umgewandelt wird:
- **FreeMargin (0):** den Wert als Kapitalbrucheil behandeln (Annäherung an den originalen "freie Margin"-Modus).
- **Balance (1):** identisch mit `FreeMargin` in diesem Port; verwendet den aktuellen Portfolio-Wert.
- **LossFreeMargin (2):** einen Kapitalbrucheil riskieren unter Verwendung der konfigurierten Stop-Loss-Distanz. Fällt auf preisbasierte Größenbestimmung zurück, wenn die Stop-Distanz null ist.
- **LossBalance (3):** gleich wie `LossFreeMargin` in dieser Implementierung.
- **Lot (4):** den Wert direkt als Ordervolumen interpretieren.

Alle berechneten Größen werden mithilfe des Volumen-Schritts des Instruments sowie der Mindest- und Höchstvolumenbeschränkungen normalisiert.

## Parameter
| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `LongCandleType` | H4 | Zeitrahmen für den Long-seitigen Vortex-Indikator. |
| `ShortCandleType` | H4 | Zeitrahmen für den Short-seitigen Vortex-Indikator. |
| `LongLength` | 14 | Periode des Vortex-Indikators für Long-Signale. |
| `ShortLength` | 14 | Periode des Vortex-Indikators für Short-Signale. |
| `LongSignalBar` | 1 | Geschlossener Balken-Offset für Long-Crossovers (0 = letzter geschlossener Balken). |
| `ShortSignalBar` | 1 | Geschlossener Balken-Offset für Short-Crossovers. |
| `AllowLongEntries` | true | Long-Einstiege beim bullischen Crossover aktivieren. |
| `AllowLongExits` | true | Schließen von Long-Positionen aktivieren, wenn `VI-` über `VI+` dominiert. |
| `AllowShortEntries` | true | Short-Einstiege beim bärischen Crossover aktivieren. |
| `AllowShortExits` | true | Schließen von Short-Positionen aktivieren, wenn `VI+` über `VI-` dominiert. |
| `LongTotalTrigger` | 5 | Anzahl der letzten Long-Trades, die vom Recovery-Zähler geprüft werden. |
| `LongLossTrigger` | 3 | Verlierende Long-Trades, die vor dem Wechsel zum reduzierten Long-Volumen erforderlich sind. |
| `LongMoneyManagement` | 0.1 | Basis-Geldverwaltungswert für Long-Trades. |
| `LongSmallMoneyManagement` | 0.01 | Reduzierter Geldverwaltungswert nach einer Long-Verlustserie. |
| `LongMarginMode` | Lot | Interpretation des Long-Geldverwaltungswerts (siehe Modi oben). |
| `LongStopLossSteps` | 1000 | Schutzabstand unterhalb des Long-Einstiegs in Preisschritten. |
| `LongTakeProfitSteps` | 2000 | Take-Profit-Abstand oberhalb des Long-Einstiegs in Preisschritten. |
| `LongSlippageSteps` | 10 | Informationsslippage-Toleranz für Long-Orders (nicht für Größenbestimmung verwendet). |
| `ShortTotalTrigger` | 5 | Anzahl der letzten Short-Trades, die vom Recovery-Zähler geprüft werden. |
| `ShortLossTrigger` | 3 | Verlierende Short-Trades, die vor dem Wechsel zum reduzierten Short-Volumen erforderlich sind. |
| `ShortMoneyManagement` | 0.1 | Basis-Geldverwaltungswert für Short-Trades. |
| `ShortSmallMoneyManagement` | 0.01 | Reduzierter Geldverwaltungswert nach einer Short-Verlustserie. |
| `ShortMarginMode` | Lot | Interpretation des Short-Geldverwaltungswerts. |
| `ShortStopLossSteps` | 1000 | Schutzabstand oberhalb des Short-Einstiegs in Preisschritten. |
| `ShortTakeProfitSteps` | 2000 | Take-Profit-Abstand unterhalb des Short-Einstiegs in Preisschritten. |
| `ShortSlippageSteps` | 10 | Informationsslippage-Toleranz für Short-Orders. |

## Implementierungshinweise
- Vollständig auf der StockSharp High-Level-API aufgebaut. Kerzenabonnements treiben die Vortex-Indikatoren über `Bind`, das fertige Balken liefert, bevor eine Entscheidung getroffen wird.
- Die Trade-Recovery-Logik speichert richtungsbezogene Gewinnreihen in Warteschlangen und spiegelt die MetaTrader `BuyTradeMMRecounterS` / `SellTradeMMRecounterS`-Funktionen wider.
- Stop-Loss- und Take-Profit-Niveaus werden in Preiseinheiten (Instrument-Preisschritt × konfigurierte Schritte) neu berechnet und bei jeder eingehenden Kerze durchgesetzt.
- Ordervolumina werden über die `VolumeStep`-, `MinVolume`- und `MaxVolume`-Beschränkungen des Instruments normalisiert, um ungültige Einreichungen zu vermeiden.
- Slippage-Parameter werden zu Dokumentationszwecken beibehalten, werden aber nicht direkt von den StockSharp-Order-Handlern verwendet.
