# Befolgen Sie die Linientrendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Follow-Line-Strategie ist eine direkte Portierung des MetaTrader-Expertenberaters `FollowLineEA_v1.0`. Es reproduziert die ursprüngliche Logik, indem es einen Bollinger-Band-Breakout-Detektor mit einer adaptiven Trendlinie kombiniert, die sich an die Preisbewegung anpasst. Die Strategie hört auf fertige Kerzen und funktioniert in jedem vom Benutzer angegebenen Zeitrahmen.

Ein Ausbruch über das obere Bollinger-Band hebt die Unterstützungslinie unter dem Preis an, während ein Schlusskurs unter dem unteren Band eine Widerstandslinie über dem Preis fallen lässt. Die Linie gleitet nur in Ausbruchsrichtung und erzeugt so ein Treppenmuster, das anhaltende Trends hervorhebt. Durch optionales ATR-Auffüllen kann die Linie verbreitert werden, um zu verhindern, dass Positionen zu früh ausgelöst werden. Auf gleitenden Durchschnitten basierende Momentumfilter bestätigen Eingaben abhängig vom gewählten Pfeilmodus.

## Handelslogik
1. **Indikatorkette**
   - Bollinger-Bänder (Länge = `BollingerPeriod`, Breite = `BollingerDeviations`).
   - Optionaler ATR (Länge = `AtrPeriod`) zum Versetzen der Trendlinie, wenn `UseAtrFilter` aktiviert ist.
   - Eine Familie einfacher gleitender Durchschnitte (Länge = `MovingAveragePeriod`), die auf Höchst-, Tiefst-, Eröffnungs-, Schluss- und Medianpreise angewendet werden. Diese Durchschnittswerte generieren Bestätigungsflags, wenn `TypeOfArrows` auf `OpenCloseMedian` oder `HighLowOpenClose` gesetzt ist.
2. **Aktualisierung der Trendlinie**
   - Eine Kerze, die über dem oberen Band schließt, drückt die Trendlinie auf den Tiefststand der Kerze (minus ATR Offset, falls verwendet), senkt sie jedoch nie.
   - Eine Kerze, die unterhalb des unteren Bandes schließt, zieht die Linie zum Kerzenhoch (plus ATR Offset, falls verwendet), hebt sie jedoch nie an.
   - Die Richtung der Trendlinie definiert, ob der Markt als bullisch (>0) oder bärisch (<0) gilt.
3. **Einstiegssignale**
   - Wenn die Richtung von bärisch zu bullisch wechselt und die Pfeilfilter übereinstimmen, wird ein Kaufpfeil in die Warteschlange gestellt.
   - Wenn die Richtung von bullisch zu bärisch wechselt, steht ein Verkaufspfeil in der Warteschlange.
   - Der Parameter `IndicatorsShift` verzögert die Ausführung, sodass der Pfeil `IndicatorsShift` Balken nach seiner Bildung verarbeitet werden kann, was die MT4-Pufferverschiebung nachahmt.
4. **Ausführungsfilter**
   - Zeitfilter: Trades sind nur zwischen `TimeStartTrade` und `TimeEndTrade` zulässig, wenn `UseTimeFilter` aktiviert ist (das Fenster kann bis Mitternacht reichen).
   - Spread-Filter: Wenn der aktuelle Spread `MaxSpread` (gemessen in Preisschritten) überschreitet, werden Aufträge übersprungen.
   - Bestellobergrenze: `MaxOrders` begrenzt die absolute Positionsgröße, um die ursprüngliche Prüfung der „maximalen Bestellmenge“ zu reproduzieren.

## Risikomanagement
- **Ausfahrt bei Gegensignal**: Setzen Sie `CloseInSignal` auf `true`, um die vorhandene Belichtung sofort zu reduzieren, wenn der Gegenpfeil ausgelöst wird.
- **Korbsperren**: `CloseInProfit` und `CloseInLoss` schließen die aktuelle Position, sobald das angegebene Pip-Ziel erreicht ist. `UseBasketClose` wendet die Schwellenwerte auf den gesamten Warenkorb an, anstatt Long- und Short-Logik zu trennen (spiegelt die MQL-Implementierung wider).
- **Stopps und Ziele**: Die Strategie ruft `SetStopLoss`, `SetTakeProfit`, Trailing- und Break-even-Guards für jeden Takt auf, wenn die entsprechenden Schalter aktiviert sind (`UseStopLoss`, `UseTakeProfit`, `UseTrailingStop`, `UseBreakEven`). Alle Entfernungen werden in Preisschritten angegeben.
- **Lotgröße**: Wenn `AutoLotSize` aktiviert ist, entspricht die Positionsgröße dem ausgewählten Anteil des aktuellen Portfoliowerts (`RiskFactor` Prozent). Ansonsten wird ein fester `ManualLotSize` verwendet. Der Betrag ist auf den Volumenschritt des Instruments normiert und durch Umtauschlimits begrenzt.

## Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Allgemein | `CandleType` | Zeitrahmen oder benutzerdefinierter Kerzentyp, der für das Abonnement verwendet wird. |
| Indikator | `BarsCount` | Vom Indikator verwendete historische Tiefe. |
| Indikator | `BollingerPeriod` / `BollingerDeviations` | Bollinger-Konfiguration für die Ausbruchserkennung. |
| Indikator | `MovingAveragePeriod` | Länge der gleitenden Durchschnitte, die Pfeilfilter antreiben. |
| Indikator | `AtrPeriod` / `UseAtrFilter` | ATR Länge und Aktivierungsflag. |
| Indikator | `TypeOfArrows` | Pfeilmodus (`HideArrows`, `SimpleArrows`, `OpenCloseMedian`, `HighLowOpenClose`). |
| Indikator | `IndicatorsShift` | Verzögerung (in Balken) zwischen Pfeilbildung und -ausführung. |
| Zeit | `UseTimeFilter`, `TimeStartTrade`, `TimeEndTrade` | Sitzungslimits. |
| Filter | `MaxSpread`, `MaxOrders` | Spread-Obergrenze und Positionslimit. |
| Risiko | `CloseInSignal`, `UseBasketClose`, `CloseInProfit`, `PipsCloseProfit`, `CloseInLoss`, `PipsCloseLoss` | Regeln für die Korbverwaltung. |
| Risiko | `UseTakeProfit`, `TakeProfit`, `UseStopLoss`, `StopLoss`, `UseTrailingStop`, `TrailingStop`, `TrailingStep`, `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | Schutzauftragssuite (Werte in Preisschritten). |
| Geldmanagement | `AutoLotSize`, `RiskFactor`, `ManualLotSize` | Positionsgrößenbestimmung. |

## Nutzungshinweise
- Die Strategie gilt nur für fertige Kerzen. Daher ist es sicher, Backtests mit der gleichen Balkenkompression wie beim Live-Handel durchzuführen.
- Die benutzerdefinierte Warteschlange hinter `IndicatorsShift` sorgt dafür, dass das High-Level-Verhalten von API mit dem MT4-Indikatorpufferzugriff (`iCustom(..., shift)`) identisch bleibt.
- `TypeOfArrows = HideArrows` deaktiviert den Handel unter Beibehaltung der Indikatorzeichnungslogik, genau wie die Quelle EA.
- Um Trades zu visualisieren, hängen Sie die Strategie nach dem Aufruf von `CreateChartArea()` an einen Diagrammbereich an (wird bereits in `OnStarted` behandelt).

## Konvertierungsdetails
- Die Logik basiert ausschließlich auf integrierten StockSharp-Indikatoren und dem High-Level-Kerzenabonnement API (keine manuelle Pufferung oder `GetValue`-Aufrufe).
- Die Auftragsverwaltung erfolgt mit `BuyMarket`/`SellMarket` plus den Hilfsmethoden `SetStopLoss` und `SetTakeProfit` und spiegelt das MT4-Verhalten des Originalcodes wider.
- Die auf Portfolios basierende Losgröße berücksichtigt Umtauschbeschränkungen durch `VolumeStep`-, `VolumeMin`- und `VolumeMax`-Prüfungen vor dem Senden von Aufträgen.
- Die Strategie behält englische Codekommentare und Parameterbeschreibungen bei, um sie an die Repository-Richtlinien anzupassen.
