# Index-Tester-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Indices Tester Strategy** ist eine direkte Portierung des MetaTrader 5 Expertenberaters „Indices Tester“. Das System konzentriert sich auf den Intraday-Indexhandel, bei dem eine einzelne Long-Position während eines sehr engen Handelsfensters eröffnet wird. Handelsentscheidungen basieren ausschließlich auf Zeitfiltern und Betriebslimits:

- Ein einzelner konfigurierbarer Kerzenstrom steuert die interne Uhr der Strategie.
- Neue Positionen können nur zwischen den konfigurierten Start- und Endzeiten der Sitzung eröffnet werden.
- Pro Tag ist eine feste Anzahl von Trades zulässig, wodurch wiederholte Wiedereinstiege vermieden werden.
- Alle offenen Positionen werden zu einem definierten Liquidationszeitpunkt zwangsweise geschlossen.
- Die Strategie funktioniert nur auf der Long-Seite und spiegelt den ursprünglichen Expert Advisor wider.

Diese Implementierung verwendet das übergeordnete StockSharp API, abonniert Kerzendaten mit `SubscribeCandles` und verarbeitet Handelsentscheidungen im Callback `ProcessCandle`. Es sind keine Indikatoren erforderlich, sodass die Logik schlank bleibt und sich auf Timing und Risikokontrollen konzentriert.

## Handelslogik
1. **Täglicher Reset** – die Strategie verfolgt den aktuellen Handelstag. Wenn ein neuer Tag beginnt, werden alle Zähler zurückgesetzt, sodass für diesen Tag ein neues Handelsvolumen möglich ist.
2. **Eintrittsfenster** – Nur Kerzen mit einer Schließzeit, die genau innerhalb des `[SessionStart, SessionEnd)`-Intervalls liegt, können Einträge auslösen. Dies reproduziert die `TimeStart`- und `TimeEnd`-Prüfungen aus dem Originalcode.
3. **Positions- und Handelslimits** – Eingaben werden übersprungen, wenn die Anzahl der am aktuellen Tag bereits eröffneten Trades `DailyTradeLimit` erreicht hat oder wenn die Anzahl gleichzeitig offener Positionen `MaxOpenPositions` überschreitet.
4. **Auftragseinreichung** – wenn alle Bedingungen übereinstimmen, übermittelt die Strategie einen Marktkaufauftrag für `TradeVolume` Einheiten. Der Handelszähler für den Tag wird unmittelbar nach der Auftragserteilung erhöht.
5. **Erzwungener Ausstieg** – wenn eine Kerze nach `CloseTime` schließt und eine aktive Long-Position besteht, schließt die Strategie die Position mit einer Marktverkaufsorder. Dies spiegelt die `ClosePos()`-Timerlogik aus der MQL-Implementierung wider.

Die Kombination aus Handelszähler und Positionsbegrenzer gewährleistet, dass sich das System standardmäßig wie ein einfacher Einzelhandelsplaner pro Tag verhält und dennoch eine Parameteranpassung für häufigere Aktivitäten ermöglicht.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Primäre Kerzenserie, die die Strategieuhr antreibt (standardmäßig 1-Minuten-Kerzen). |
| `SessionStart` | Tageszeit, zu der neue Trades beginnen dürfen. |
| `SessionEnd` | Tageszeit, zu der neue Trades nicht mehr zulässig sind. |
| `CloseTime` | Tageszeit, zu der alle verbleibenden offenen Positionen liquidiert werden. |
| `DailyTradeLimit` | Maximal zulässige Anzahl von Einträgen pro Tag, bevor der Handel ausgesetzt wird. |
| `MaxOpenPositions` | Maximale Anzahl gleichzeitig offener Long-Positionen (gezählt in Handelseinheiten). |
| `TradeVolume` | Für jeden Eintrag verwendetes Market-Order-Volumen. |

## Hinweise und Unterschiede
- StockSharp stellt keine MetaTrader-Sitzungstabellen zur Verfügung, daher basiert die Konvertierung auf der Austauschzeit von Kerzenzeitstempeln zusammen mit dem `IsFormedAndOnlineAndAllowTrading()`-Guard.
- Der ursprüngliche Fachberater verwendete Timer der zweiten Ebene; Dieser Port nutzt Kerzenschließungen, um sowohl den Einstiegszeitpunkt als auch erzwungene Ausstiege zu steuern, was für Handelsfenster auf Minutenebene ausreicht.
- Die Handelszahlen werden zu Beginn jedes Handelstages zurückgesetzt, der anhand der Schlusszeiten der Kerzen ermittelt wird. Dadurch bleibt das Verhalten über verschiedene Zeitzonen hinweg konsistent, solange die Quelle der Kerze mit der gewünschten Börse übereinstimmt.

## Nutzungstipps
- Stellen Sie sicher, dass der konfigurierte `CandleType` mit dem gehandelten Markt übereinstimmt, damit die Zeitfilter mit der gewünschten Sitzung übereinstimmen.
- Erhöhen Sie `DailyTradeLimit`, wenn mehrere Versuche pro Tag erforderlich sind, beispielsweise bei kürzeren Zeitrahmen.
- Setzen Sie `MaxOpenPositions` nur dann über `1`, wenn eine teilweise Skalierung in Positionen gewünscht ist; Andernfalls behalten Sie die Standardeinstellung bei, um das MetaTrader-Skript genau nachzuahmen.
