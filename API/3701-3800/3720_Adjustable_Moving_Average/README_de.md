# Anpassbare gleitende Durchschnittsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erstellt den Expert Advisor „Adjustable Moving Average“ von MetaTrader unter Verwendung des übergeordneten API von StockSharp neu. Zwei gleitende Durchschnitte desselben Typs, aber unterschiedlicher Länge überwachen ihren Abstand. Wenn die schnellere Kurve die langsamere um mindestens eine konfigurierbare Lücke kreuzt, schließt die Strategie jede entgegengesetzte Position und eröffnet optional einen Handel in die neue Richtung. Zusätzliche Sitzungsfilter, Schutzausgänge und ein optionaler Trailing Stop bieten die gleiche betriebliche Flexibilität wie der Originalroboter.

## Handelslogik

- Zwei gleitende Durchschnitte (schnell und langsam) verwenden dieselbe Berechnungsmethode. Die schnellere Periode wird automatisch auf den kleineren Eingang eingestellt, die langsamere Periode auf den größeren Eingang.
- Ein Signal wird erst erzeugt, wenn beide gleitenden Durchschnitte vollständig gebildet sind und ihr absoluter Abstand den in Preiseinheiten umgerechneten Schwellenwert `MinGapPoints` überschreitet.
- Wenn der schnelle MA um die erforderliche Lücke über dem langsamen MA liegt, wird der interne Signalzustand bullisch. Ein rückläufiger Zustand wird registriert, wenn der langsame MA über dem schnellen MA liegt.
- Ein Statuswechsel schließt jede vorhandene Position, wenn `CloseOutsideSession` aktiviert ist oder die aktuelle Zeit innerhalb des Sitzungsfensters liegt. Neue Aufträge folgen dem ausgewählten `Mode` (nur Kauf, nur Verkauf oder beides) und verwenden entweder eine feste Losgröße oder die automatische Losgrößenregel.
- Bei jeder fertigen Kerze wird die Schutzlogik überprüft:
  - Stop-Loss- und Take-Profit-Abstände werden in Instrumentenpunkten gemessen und anhand der Kerzenspanne bewertet.
  - Der Trailing Stop wird aktiviert, sobald sich der Preis um mindestens `TrailStopPoints` Punkte zugunsten der Position bewegt. Der Stopp wird nur verschärft, wenn der Sitzungsfilter Trailing zulässt oder `TrailOutsideSession` aktiviert ist. Sobald der Stop gesetzt ist, bleibt er auch außerhalb der Handelszeiten aktiv.

## Positionsgrößenbestimmung

- Mit `EnableAutoLot = false` sendet die Strategie das Volumen von `FixedLot` (nach Anwendung von Instrumentschritt-, Mindest- und Höchstgrenzen).
- Mit `EnableAutoLot = true` wird das Volumen aus dem verfügbaren Portfoliowert angenähert: `(PortfolioValue / 10,000) * LotPer10kFreeMargin`, gerundet auf eine Dezimalstelle. Das berechnete Volumen wird auch an die Wechselkursbeschränkungen angepasst.

## Parameter

| Name | Typ/Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | `TimeFrame` = 5-Minuten-Kerzen | Zeitrahmen, der für die Berechnung des gleitenden Durchschnitts verwendet wird. |
| `FastPeriod` | `int` = 3 | Kurze gleitende Durchschnittslänge. Muss sich von `SlowPeriod` unterscheiden. |
| `SlowPeriod` | `int` = 9 | Lange gleitende Durchschnittslänge. Muss sich von `FastPeriod` unterscheiden. |
| `MaMethod` | `MovingAverageMethod` = Exponentiell | Algorithmus für gleitenden Durchschnitt (einfach, exponentiell, geglättet, gewichtet). |
| `MinGapPoints` | `decimal` = 3 | Mindestabstand zwischen den schnellen und langsamen Mittelwerten in Instrumentenpunkten. Umgerechnet anhand der Instrumentenpreisstufe. |
| `StopLossPoints` | `decimal` = 0 | Schutzanschlagabstand in Instrumentenpunkten. Zum Deaktivieren auf Null setzen. |
| `TakeProfitPoints` | `decimal` = 0 | Gewinnen Sie die Zielentfernung in Instrumentenpunkten. Zum Deaktivieren auf Null setzen. |
| `TrailStopPoints` | `decimal` = 0 | Trailing-Stop-Distanz in Instrumentenpunkten. Zum Deaktivieren auf Null setzen. |
| `Mode` | `EntryMode` = Beide | Zulässige Richtung für neue Trades (Both, BuyOnly, SellOnly). |
| `SessionStart` | `TimeSpan` = 00:00 | Startzeit der Sitzung (Plattformuhr). |
| `SessionEnd` | `TimeSpan` = 23:59 | Endzeit der Sitzung (Plattformuhr). Unterstützt Nachtsitzungen, wenn `SessionEnd < SessionStart`. |
| `CloseOutsideSession` | `bool` = wahr | Bei „true“ werden entgegengesetzte Positionen auch außerhalb des Sitzungsfensters geschlossen. |
| `TrailOutsideSession` | `bool` = wahr | Wenn „true“, wird der Trailing Stop nach dem Schließen der Sitzung weiter aktualisiert. |
| `FixedLot` | `decimal` = 0,1 | Verwendetes Volumen, wenn die automatische Größenanpassung deaktiviert ist. |
| `EnableAutoLot` | `bool` = falsch | Aktivieren Sie die Volumenschätzung anhand des Portfoliowerts. |
| `LotPer10kFreeMargin` | `decimal` = 1 | Im Auto-Lot-Modus werden Lots pro 10.000 Einheiten des Portfoliowerts zugewiesen. |
| `MaxSlippage` | `int` = 3 | Der Vollständigkeit halber beibehalten; StockSharp Marktaufträge stellen keinen direkten Slippage-Parameter bereit. |
| `TradeComment` | `string` = "AdjustableMovingAverageEA" | Text, der in Protokollnachrichten enthalten ist, wenn Trades ausgeführt werden. |

## Notizen

- Die ursprüngliche MetaTrader-Version wendete Stop-Loss, Take-Profit und Trailing-Stops über Auftragsänderungen an. Der StockSharp-Port emuliert das Verhalten, indem er Kerzenbereiche auswertet und gegensätzliche Marktaufträge sendet.
- Der Portfoliowert wird als Näherungswert für die freie Marge verwendet, da `AccountFreeMargin()` von MetaTrader in StockSharp nicht verfügbar ist.
- Wenn dem Instrument ein gültiger `PriceStep` fehlt, bleiben punktbasierte Berechnungen (Lücke, Stopps, Nachlauf) inaktiv.
