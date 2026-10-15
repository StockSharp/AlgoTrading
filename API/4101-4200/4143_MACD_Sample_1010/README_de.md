# MACD Beispiel für eine 1010-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Dieses Modul portiert den MetaTrader Expert Advisor **macd_sample_1010.mq4** auf den StockSharp High-Level API. Das ursprüngliche Skript kombinierte Bollinger-Bänder mit einfachen Money-Management-Regeln: Wenn der Schlusskurs über dem oberen Band plus einem konfigurierbaren Puffer endete, wurde eine Verkaufsorder eröffnet, während ein Schlusskurs unter dem unteren Band minus dem Puffer eine Kauforder auslöste. Positionen wurden geschlossen, sobald ein fester Gewinn- oder Verlustbetrag (ausgedrückt in Pips) erreicht wurde. Die StockSharp-Version reproduziert dieselbe Logik, indem sie die angeforderte Kerzenserie abonniert, einen `BollingerBands`-Indikator bindet und Marktaufträge und Positionsverwaltungsaufrufe aus dem Kerzenrückruf ausgibt.

Die Konvertierung behält das Verhalten des alten Experten für fertige Kerzen bei. Jede Auswertung erfolgt, wenn eine Kerze vollständig geformt ist, um sicherzustellen, dass die Ausbruchs- und Ausstiegsentscheidungen mit der Bar-Close-Verarbeitung der MetaTrader-Plattform übereinstimmen. Eine optionale saldobasierte Skalierung des Handelsvolumens ist ebenfalls implementiert, um das Flag `LotIncrease` aus dem Code MQL4 zu emulieren.

## Konvertierungshinweise
- Verwendet den übergeordneten Workflow `SubscribeCandles` + `Bind`, um den Indikator `BollingerBands` ohne benutzerdefinierte Puffer zu füttern.
- Nutzt die StockSharp `StrategyParam<T>`-Infrastruktur, sodass alle Eingaben in der Benutzeroberfläche sichtbar und zur Optimierung bereit sind.
- Ruft `BuyMarket` und `SellMarket` mit berechneten Offsets auf, die den `PriceStep` des Instruments berücksichtigen und mit den Pip-basierten Berechnungen in MetaTrader übereinstimmen.
- Implementiert die optionale Losskalierung durch `Portfolio.CurrentValue` (mit `BeginValue` als Fallback) und begrenzt das resultierende Volumen auf 500 Lose, genau wie der ursprüngliche Experte.
- Funktioniert ausschließlich mit fertigen Kerzen, um die Abwanderung von Tick zu Tick zu vermeiden, die das ursprüngliche Skript über Barzähler steuerte.
- Fügt beschreibende englische Kommentare hinzu, um die Absicht jedes Verarbeitungsblocks zu verdeutlichen.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `ProfitTargetPips` | `decimal` | `3` | Anzahl der Pips mit günstiger Bewegung, die erforderlich sind, um eine Position mit Gewinn zu schließen. Auf `0` setzen, um die Take-Profit-Regel zu deaktivieren. |
| `LossLimitPips` | `decimal` | `20` | Anzahl der Pips mit nachteiliger Bewegung, die toleriert werden, bevor die Position liquidiert wird. Auf `0` setzen, um die Stop-Loss-Regel zu deaktivieren. |
| `BandDistancePips` | `decimal` | `3` | Puffer (in Pips), der oberhalb des oberen Bandes und unterhalb des unteren Bandes hinzugefügt wird, bevor ein Ausbruch bestätigt wird. |
| `BollingerPeriod` | `int` | `4` | Anzahl der Kerzen, die zur Berechnung der Bollinger-Bänder verwendet werden. |
| `BollingerDeviation` | `decimal` | `2` | Vom Bollinger Bands-Indikator angewendeter Standardabweichungsmultiplikator. |
| `BaseVolume` | `decimal` | `1` | Anfängliche Handelsgröße, ausgedrückt in Lots. Dieser Wert wird auch als Basis für die Skalierungslogik verwendet. |
| `LotIncrease` | `bool` | `true` | Wenn diese Option aktiviert ist, wird das Handelsvolumen für jede Kerze neu berechnet, sodass es dem Verhältnis zwischen dem aktuellen Portfoliosaldo und dem Startsaldo folgt. |
| `OneOrderOnly` | `bool` | `true` | Verhindert, dass die Strategie eine neue Position eröffnet, wenn bereits eine aktiv ist. Wenn die Option deaktiviert ist, wird die Nettoposition weiterhin verwaltet, da StockSharp aggregierte Positionen verwendet. |
| `CandleType` | `DataType` | `TimeFrame(15m)` | Kerzenserien, die sowohl für Indikatorberechnungen als auch für Handelsentscheidungen verwendet werden. |

## Handelslogik
1. `OnStarted` erstellt den Bandindikator Bollinger mit dem konfigurierten Zeitraum und der konfigurierten Abweichung, abonniert den Datenstrom `CandleType` und bindet die Methode `ProcessCandle`.
2. Jede fertige Kerze löst `ProcessCandle` aus, das vor der Auswertung der Signale das aktuelle Handelsvolumen neu berechnet (sofern `LotIncrease` aktiv ist).
3. Wenn der Schlusskurs über dem oberen Band plus `BandDistancePips` liegt (umgerechnet in Preiseinheiten mit `PriceStep`), sendet die Strategie einen Marktverkaufsauftrag. Wenn der Schlusskurs unter dem unteren Band abzüglich des Puffers liegt, wird eine Marktkauforder gesendet. Wenn `OneOrderOnly` den Wert `true` hat, werden neue Einträge nur dann versucht, wenn die Nettoposition Null ist.
4. Nachdem potenzielle Eingaben verarbeitet wurden, prüft die Strategie die aktuelle Position:
   - Long-Positionen werden geschlossen, sobald die Gewinndistanz `ProfitTargetPips` erreicht oder wenn der Verlust `LossLimitPips` erreicht.
   - Short-Positionen werden geschlossen, wenn sich der Schlusskurs um `ProfitTargetPips` zu seinen Gunsten oder um `LossLimitPips` nach unten bewegt.
5. Alle Gewinn- und Verlustvergleiche werden in Preiseinheiten durchgeführt, die vom `PriceStep` des Symbols abgeleitet sind, wodurch die Pip-basierten Prüfungen im MetaTrader-Experten originalgetreu nachgebildet werden.

## Logik zur Positionsgrößenbestimmung
- Wenn `LotIncrease` deaktiviert ist, handelt die Strategie bei jedem Signal mit dem konstanten Wert `BaseVolume`.
- Wenn `LotIncrease` aktiviert ist, speichert die erste Kerze den Startsaldo pro Lot (`initial balance / BaseVolume`). Nachfolgende Kerzen berechnen das Verhältnis zwischen dem aktuellen Saldo und dieser Basislinie, runden es auf eine Dezimalstelle (imitieren `NormalizeDouble(..., 1)` von MQL4) und begrenzen das Ergebnis auf maximal 500 Lots. Der berechnete Wert wird dann als Ordervolumen für den nächsten Trade verwendet.
- Wenn keine Portfolioinformationen verfügbar sind, greift die Strategie ordnungsgemäß auf den statischen `BaseVolume` zurück.

## Nutzungsrichtlinien
1. Hängen Sie die Strategie an das gewünschte Instrument an und bestätigen Sie, dass der `Security.PriceStep` die Pip-Größe widerspiegelt, mit der Sie handeln möchten.
2. Wählen Sie den Kerzenzeitraum in `CandleType` aus. Das ursprüngliche Skript wurde normalerweise in Intraday-Zeiträumen (5–15 Minuten) ausgeführt, es kann jedoch jede beliebige Balkengröße verwendet werden.
3. Passen Sie die Bandeinstellungen, Pip-Offsets und Risikokontrollen an Ihre Handelspräferenzen an.
4. Entscheiden Sie, ob die Positionsgröße mit dem Kontostand (`LotIncrease`) skalieren oder fest bleiben soll.
5. Starten Sie die Strategie. Überwachen Sie das Protokoll, um sicherzustellen, dass Ein- und Ausstiege bei abgeschlossenen Kerzen auf dem erwarteten Preisniveau erfolgen.

## Unterschiede zur MetaTrader-Version
- StockSharp funktioniert mit aggregierten Positionen. Selbst wenn `OneOrderOnly` deaktiviert ist, ist das Ergebnis eine einzelne Nettoposition und nicht mehrere unabhängige Tickets.
- Die Take-Profit- und Stop-Loss-Regeln werden direkt in der Strategie implementiert, anstatt ausstehende Aufträge mit bestimmten Preisniveaus zu registrieren. Das resultierende Verhalten ist jedoch gleichwertig, da bei jeder fertigen Kerze eine Überprüfung erfolgt.
- Protokollierungsflags (`logging`, `logerrs`, `logtick`) vom ursprünglichen Experten werden weggelassen; Die integrierte Protokollierung von StockSharp zeichnet bereits Auftrags- und Handelsereignisse auf.
- Dateibasierte Protokollierung und Statistiken aus der Version MetaTrader werden nicht neu erstellt, da StockSharp umfangreichere Analysen über Portfolios und Strategien bereitstellt.
