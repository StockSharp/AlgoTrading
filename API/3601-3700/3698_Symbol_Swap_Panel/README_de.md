# Symbol-Swap-Panel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Symbol-Swap-Panel-Strategie** ist eine StockSharp-Konvertierung des MQL-Panels *„Symbol-Swap-Panel“*. Der ursprüngliche Experte fungierte als Diagramm-Widget, das es Händlern ermöglichte, ein Symbol einzugeben, das aktive Diagramm auf dieses Symbol umzustellen und Marktinformationen wie OHLC-Werte, Tick-Volumen und Spread in Echtzeit zu überwachen. Die konvertierte Strategie erstellt denselben Workflow in der StockSharp-Umgebung neu. Es kann auf jedem Wertpapier gestartet werden und ermöglicht einen manuellen Wechsel zu einem anderen Instrument, während gleichzeitig die relevantesten Marktkennzahlen kontinuierlich protokolliert werden.

## Kernverhalten
- Abonniert Kerzendaten und Level-1-Kurse für das aktive Wertpapier.
- Protokolliert jede abgeschlossene Kerze mit Eröffnung, Höchst-, Tiefst- und Schlusskurs, Gesamtvolumen und dem zuletzt berechneten Spread.
- Speichert Bid/Ask-Kurse und leitet eine aktuelle Spanne ab, die die Anzeige im MQL-Panel widerspiegelt.
- Reagiert auf manuelle Swap-Anfragen und ersetzt die überwachte Sicherheit durch die gewählte Kennung, ohne dass die Strategie neu gestartet werden muss.
- Behält die zuvor ausgewählte Sicherheit bei, sodass redundante Swaps ignoriert werden und versehentliche Doppelaktivierungen die Abonnements nicht unterbrechen.

## Parameter
| Name | Typ | Beschreibung |
| --- | --- | --- |
| `TargetSecurityId` | `string` | Sicherheitskennung, die aktiviert werden soll, wenn die Swap-Anfrage ausgelöst wird. Leere Zeichenfolgen werden mit einer Warnung ignoriert. |
| `CandleType` | `DataType` | Kerzenaggregation für regelmäßige Aktualisierungen (standardmäßig 1-Stunden-Kerzen, repliziert den Panel-Zeitrahmen MQL). |
| `SwapRequested` | `bool` | Manuelle Markierung, die einen sofortigen Wechsel zu `TargetSecurityId` anfordert. Es wird auf `false` zurückgesetzt, nachdem der Austauschversuch verarbeitet wurde. |

## Datenabonnements
- Candle-Abonnement erstellt mit `CandleType` für das aktuell aktive Wertpapier.
- Abonnement der ersten Stufe, das zur Verfolgung von Geld-/Briefkursen und zur Berechnung eines Live-Spread-Werts verwendet wird.
- Abonnements werden bei jeder Sicherheitsänderung sicher neu gestartet, um sicherzustellen, dass veraltete Datenströme nicht weiter ausgeführt werden.

## Arbeitsablauf
1. Wenn die Strategie startet, löst sie die anfängliche Sicherheit von `Strategy.Security` oder, falls sie fehlt, von `TargetSecurityId` auf.
2. Für dieses Instrument werden Candle- und Level-One-Abonnements eröffnet.
3. Jede abgeschlossene Kerze löst eine detaillierte Protokollmeldung aus, die den in den Original-Panel-Beschriftungen angezeigten Text widerspiegelt.
4. Eingehende Aktualisierungen der Ebene 1 aktualisieren die zwischengespeicherten Geld-/Briefwerte.
5. Wenn Sie `SwapRequested` auf `true` setzen und ein gültiges `TargetSecurityId` angeben, wird die überwachte Sicherheit sofort umgeschaltet und die Abonnements neu gestartet.

## Nutzungshinweise
- Die Strategie ist auf manuelle Überwachung ausgelegt und erteilt keine Aufträge.
- Der Spread wird nur gemeldet, wenn sowohl Geld- als auch Briefwerte vorhanden und positiv sind.
- Wenn ein ungültiges oder unbekanntes Symbol angegeben wird, wird eine Warnung protokolliert und die Anfrage verworfen, ohne dass die laufenden Abonnements unterbrochen werden.
- Da das ursprüngliche Tool die Benutzeroberfläche einmal pro Sekunde aktualisierte, können Sie den Kerzenzeitraum verkürzen, wenn Sie häufigere Protokollaktualisierungen benötigen.

## Ursprüngliche MQL-Funktionen bleiben erhalten
- Manuelle Symbolumschaltung über eine Textkennung.
- Echtzeitanzeige von OHLC-Werten, Volumen und Spread für das ausgewählte Symbol.
- Schützt vor leeren Eingaben und fehlgeschlagenen Market Watch-Ergänzungen (übersetzt in StockSharp-Warnungen).

## Unterschiede zur MQL-Implementierung
- Die StockSharp-Strategie verwendet Protokollmeldungen anstelle von Beschriftungen auf dem Bildschirm. Dies entspricht dem typischen Workflow in StockSharp und stellt dennoch dieselben Informationen bereit.
- Der Kartenwechsel wird durch Neuzuweisung der Strategiesicherheit und Neuerstellung von Abonnements implementiert, anstatt ein Terminal-Kartenfenster zu ändern.
- Die zeitgesteuerte Aktualisierungslogik wird durch Kerzenabschlussereignisse ersetzt, um mit den StockSharp-APIs auf hoher Ebene in Einklang zu bleiben.

## Anforderungen
- StockSharp Connector mit Zugriff auf die gewünschten Wertpapiere.
- Datenfeed der ersten Ebene, um Geld-/Briefkurse für die Spread-Berechnung zu erhalten.
