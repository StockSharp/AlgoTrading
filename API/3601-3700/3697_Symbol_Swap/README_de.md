# Symbol-Swap-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Symbol-Swap-Strategie** ist der StockSharp-Port des MetaTrader 5-Dienstprogramms „Symbol Swap“. Das ursprüngliche MQL5-Programm öffnet ein Panel, in dem ein Händler einen beliebigen Ticker eingeben, das aktuelle Diagramm sofort auf dieses Symbol umstellen und ein kompaktes Datenfenster mit der neuesten Zeit, OHLC-Preisen, Tick-Volumen und Spread überwachen kann. Diese C#-Konvertierung behält die gleichen Verantwortlichkeiten bei, verlässt sich jedoch ausschließlich auf das High-Level-Abonnement API von StockSharp.

## Verhalten

1. Beim Start löst die Strategie das zu beobachtende Instrument auf. Es versucht zuerst `WatchedSecurityId`; Wenn das Feld leer ist, wird auf `Strategy.Security` zurückgegriffen, das im Launcher konfiguriert ist.
2. Kerzendaten des ausgewählten `CandleType` werden über `SubscribeCandles(...)` gestreamt. Fertige Balken liefern die Eröffnungs-, Höchst-, Tiefst-, Schluss- und Tick-Volumen, die das Panel füllen.
3. Die besten Geld-/Briefwerte in Echtzeit kommen über `SubscribeLevel1(...)` an. Der Spread wird bei jeder Kursaktualisierung neu berechnet, um das Datenfenster MQL widerzuspiegeln.
4. Der formatierte Block wird entweder in das Strategieprotokoll (`OutputMode = Log`) geschrieben oder mit `DrawText(...)` in einem Diagramm (`OutputMode = Chart`) gerendert, wodurch das schwebende Panel aus MetaTrader neu erstellt wird.
5. Durch den Aufruf von `SwapSecurity("TICKER")` während der Ausführung wird das neue Wertpapier über `SecurityProvider.LookupById` aufgelöst und sowohl die Candle- als auch die Level-1-Feeds nahtlos erneut für das angeforderte Instrument abonniert.

Die Strategie dient nur der Information; Es werden keine Bestellungen aufgegeben. Es kann eigenständig als Markt-Dashboard oder zusammen mit anderen Trading-Bots ausgeführt werden.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `CandleType` | Zeitrahmen, der das Kerzenabonnement definiert, das zum Erstellen von OHLC- und Tick-Volumendaten verwendet wird. | `TimeFrame(1 minute)` |
| `WatchedSecurityId` | Optionale Gerätekennung. Leer lassen, um `Strategy.Security` zu verwenden. | _leer_ |
| `OutputMode` | Rendering-Ziel des Informationsblocks. Wählen Sie zwischen `Chart` (Overlay in der Nähe des Preises) oder `Log` (Strategieprotokoll). | `Chart` |

## Öffentliche Methoden

| Methode | Beschreibung |
|--------|-------------|
| `SwapSecurity(string securityId)` | Löst den bereitgestellten Ticker über das aktive `SecurityProvider` auf und schaltet das Panel sofort auf dieses Symbol um. Die Methode kann mehrfach aufgerufen werden; Bei jedem Aufruf werden vorherige Candle-/Level-1-Abonnements gelöscht, bevor die neuen Feeds hinzugefügt werden. |

## Nutzungshinweise

- Stellen Sie sicher, dass der Connector die angeforderte Kennung offenlegt. andernfalls löst `SecurityProvider.LookupById` eine Ausnahme aus.
- Bei `OutputMode = Chart` erstellt die Strategie automatisch einen Diagrammbereich, zeichnet die abonnierten Kerzen und überlagert den Statusblock. Im Protokollmodus werden nur die Textaktualisierungen erstellt.
- Das Tick-Volumen entspricht dem `TotalVolume` der Kerze. Auf diese Weise meldet MetaTrader die Tick-Anzahl pro Balken.
- Der Spread wird nur angezeigt, wenn sowohl der beste Geldkurs als auch der beste Briefkurs verfügbar sind. Andernfalls zeigt das Feld `n/a` an.

## Konvertierungsdetails

- Die MetaTrader-Timerschleife wird durch StockSharp-Abonnements ersetzt. Kerzen werden einmal pro fertigem Balken ausgelöst und Kurse der Stufe 1 aktualisieren den Spread in Echtzeit.
- Die Panel-Beschriftungen MQL werden durch einen einzelnen mehrzeiligen Textblock dargestellt. Der Text verwendet die genaue Reihenfolge des Originaltools: Zeit, Periode, Symbol, Schluss, Eröffnung, Hoch, Tief, Tick-Volumen, Spread.
- Für den Austausch von Laufzeitsymbolen ist kein manuelles Market Watch-Management mehr erforderlich – die Strategie löst Instrumente direkt über den Sicherheitsanbieter StockSharp auf.
- Es werden nur API-Aufrufe auf hoher Ebene verwendet (`SubscribeCandles`, `SubscribeLevel1`, `DrawText`, `AddInfo`). Es gibt keine manuellen Indikatorberechnungen oder direkten Connector-Manipulationen, wodurch die Kodierungsregeln des Repositorys erfüllt werden.
