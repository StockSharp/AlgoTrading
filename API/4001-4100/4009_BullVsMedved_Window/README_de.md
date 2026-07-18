# Fensterstrategie „Bull vs. Medved“.
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Bull vs Medved-Strategie ist eine StockSharp-Umsetzung des MetaTrader 4-Experten *Bull_vs_Medved.mq4*. Das System versucht es
Geben Sie Pullbacks innerhalb eines starken bullischen oder bärischen Impulses ein, indem Sie innerhalb von sechs vordefinierten Fünf-Minuten-Pending-Limit-Orders platzieren
Die Fenster sind über den Handelstag verteilt. Die StockSharp-Version behält die Idee bei, nur einmal pro Fenster zu handeln, und bricht veraltet ab
ausstehende Aufträge und nutzt die Körpergröße der Signalkerze, um dynamische Stop-Loss- und Take-Profit-Abstände abzuleiten.

## Handelslogik
1. Abonnieren Sie den durch `CandleType` definierten Kerzenstream und verarbeiten Sie nur fertige Kerzen.
2. Behalten Sie die letzten beiden abgeschlossenen Kerzen bei, also die aktuelle Kerze (`shift1`), die vorherige Kerze (`shift2`) und die Kerze
davor (`shift3`) replizieren Sie die in MetaTrader verwendeten `Close[1..3]`-Referenzen.
3. Überprüfen Sie während jedes Handelsfensters (`EntryWindowMinutes` Minuten ab `StartTime0..5`) die folgenden Muster:
   - **Bulle**: `shift3` schließt über dem Eröffnungskurs von `shift2`, der Körper von `shift2` beträgt mindestens 10 Brokerpunkte und der Körper von
`shift1` beträgt mindestens `CandleSizePoints` Punkte. Wenn `IsBadBull` falsch ist (drei lange Körper hintereinander), legen Sie ein Kauflimit fest.
   - **Cool Bull**: `shift2` ist ein Pullback von mindestens 20 Punkten, der unter dem Eröffnungskurs von `shift1` schließt, der wiederum darüber schließt
die `shift2` öffnen sich mit einem Körper von mindestens 40 % des Schwellenwerts; Legen Sie ein Kauflimit fest.
   - **Bär**: Der Körper von `shift1` beträgt mindestens `CandleSizePoints` Punkte, ist aber bärisch; Legen Sie ein Verkaufslimit fest.
4. Kauflimits liegen bei `ask - BuyIndentPoints * PriceStep`, Verkaufslimits bei `bid + SellIndentPoints * PriceStep`. Nur einer
Eine ausstehende Order oder Position kann zu einem bestimmten Zeitpunkt vorhanden sein, daher überspringt die Strategie neue Signale, wenn ein Trade innerhalb der bereits aktiven Position ist
Fenster.
5. Stopps und Ziele sind in der Strategie verborgen. Wenn eine Einstiegsorder ausgeführt wird, wird der Kerzenkörper von `shift1` mit multipliziert
`StopLossMultiplier` und `TakeProfitMultiplier`, normalisiert auf `PriceStep` und als Ausstiegspreise gespeichert.
6. Bei jeder abgeschlossenen Kerze bewertet die Strategie, ob das Hoch/Tief den gespeicherten Stop oder das gespeicherte Ziel durchbrochen hat. Das Level erreichen
Schließt die offene Position mit einer Marktorder und löscht die Schutzflaggen.
7. Ausstehende Bestellungen, die älter als 230 Minuten sind, werden storniert, um die Bereinigungsroutine von MetaTrader nachzuahmen, und `_orderPlacedInWindow` ist es
zurückgesetzt, wenn der Preis das Handelsfenster verlässt.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | Für jede Limit-Order verwendetes Volumen. |
| `CandleSizePoints` | `decimal` | `75` | Minimale bullische/bärische Körpergröße (in Brokerpunkten) für die Signalkerze. |
| `StopLossMultiplier` | `decimal` | `0.8` | Auf den Signalkerzenkörper angewendeter Multiplikator, um den Stoppabstand zu bilden. |
| `TakeProfitMultiplier` | `decimal` | `0.8` | Auf den Körper der Signalkerze angewendeter Multiplikator, um die Zielentfernung zu ermitteln. |
| `BuyIndentPoints` | `decimal` | `16` | Anzahl der Broker-Punkte, die bei der Platzierung von Kauflimits vom Brief abgezogen werden. |
| `SellIndentPoints` | `decimal` | `20` | Anzahl der Brokerpunkte, die beim Setzen von Verkaufslimits zum Gebot hinzugefügt werden. |
| `EntryWindowMinutes` | `int` | `5` | Dauer jeder Sitzung in Minuten. |
| `CandleType` | `DataType` | 5-Minuten-Kerzen | Von der Strategie verarbeitete Kerzenserie. |
| `StartTime0..5` | `TimeSpan` | `00:05`, `04:05`, `08:05`, `12:05`, `16:05`, `20:05` | Startzeit jedes Handelsfensters. |

## Unterschiede zum ursprünglichen Experten
- Der MetaTrader-Experte weist der ausstehenden Order selbst Stop-Loss und Take-Profit zu. Der StockSharp-Port simuliert das
Verhalten, indem verborgene Niveaus gespeichert und die Nettoposition mit Marktaufträgen geschlossen werden, wenn Kerzen diese durchbrechen.
- Preisschwellenwerte verwenden `Security.PriceStep`, sodass die Umrechnung sowohl bei 4- als auch bei 5-stelligen Forex-Kursen ohne zusätzliche Kosten funktioniert
Parameter.
- Zur Auswertung der Stop-/Zielregeln werden nur fertige Kerzen verwendet, wohingegen MetaTrader Stops intrabar durch ausgelöst werden können
Handelsserver.
- Akustische Warnungen und Kommentarfelder aus dem ursprünglichen EA wurden weggelassen; Stattdessen liefern die StockSharp-Protokolle Feedback.

## Anwendungstipps
- Die Strategie ist für Forex-Symbole konzipiert, die fraktionierte Pip-Preise verwenden. Überprüfen Sie `PriceStep`, um dies punktbasiert zu bestätigen
Filter passen zum vorgesehenen Pip-Abstand.
- Da Stop und Take Profit verborgen sind, sollten Sie erwägen, die Strategie in einer speziellen Umgebung auszuführen oder sie durch eine zu schützen
Brokerseitiges Risikomodul für den Fall, dass die Verbindung abbricht.
- Passen Sie die `StartTime`-Werte an, wenn Ihre Broker-Sitzung vom ursprünglichen GMT-basierten Zeitplan abweicht. Jedes Fenster kann per deaktiviert werden
Legen Sie die Startzeiten außerhalb Ihres Handelstages fest.
- Hängen Sie die Strategie an ein Diagramm an, um die Limit-Orders zu visualisieren und sicherzustellen, dass in jedem Fenster nur ein Eingabeversuch unternommen wird.
