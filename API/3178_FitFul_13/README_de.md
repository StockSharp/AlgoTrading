# FitFul 13-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der FitFul 13 Expert Advisor arbeitet rund um wöchentliche Pivot-Niveaus, die aus der vorherigen Handelswoche abgeleitet werden. Er wartet darauf, dass die aktuelle H1-Kerze (Standard-Zeitrahmen) auf eines der Pivot-Bänder reagiert und bestätigt die Bewegung mit zwei älteren Kerzen aus einer M15-Bestätigungsserie. Wenn die Bestätigung vorhanden ist, öffnet die Strategie eine Position mit vorberechneten Stop-Loss- und Take-Profit-Niveaus, die aus der gleichen Pivot-Struktur abgeleitet werden. Ein Trailing-Stop schützt profitable Trades, sobald sich der Preis weit genug bewegt.

## Ursprüngliche Logik
1. Den typischen Preis und die Pivot-Struktur der Vorwoche berechnen: `PriceTypical`, `R1`, `S1`, intermediate Halbstufen (`R0.5`, `S0.5`, `R1.5`, etc.) und die Erweiterungen zweiter/dritter Ordnung.
2. Die jüngste H1-Kerze beobachten. Bei bullishem Schluss im Körper der vorherigen Kerze nach einem Aufwärtsdurchbruch eines Pivot-Niveaus suchen. Wenn ein solcher Durchbruch auftritt, Long-Parameter vorbereiten: Stop unter dem relevanten Support, Take-Profit über dem gepaarten Widerstand. Für bearishe Schlüsse bereitet die gespiegelte Logik Short-Parameter vor.
3. Wenn der H1-Kerzenkörper mit keinem Pivot interagiert hat, zwei frühere M15-Kerzen prüfen. Zwei aufeinanderfolgende Tiefs, die dasselbe Niveau durchbrechen, bestätigen Long-Setups; zwei Hochs, die durch ein Niveau fallen, bestätigen Shorts. Jede Kombination wird auf ihr eigenes Stop-/Take-Paar abgebildet.
4. Eine Marktorder mit dem konfigurierten Netto-Volumen senden. Der StockSharp-Port arbeitet mit Nettopositionen, daher wird entgegengesetztes Exposure vor dem Öffnen des neuen Trades geflättet. Stop-Loss- und Take-Profit-Preise werden intern gespeichert und über virtuelle Ausstiege auf neuen Kerzen erzwungen.
5. Einen virtuellen Trailing-Stop anwenden: Sobald der offene Gewinn `TrailingStopPips + TrailingStepPips` überschreitet, den Stop auf `close - TrailingStopPips` (Long) oder `close + TrailingStopPips` (Short) verschieben. Der Stop bewegt sich nie zurück und wird nur enger gezogen, wenn der Preis um mindestens den Trailing-Schritt vorrückt.
6. Neue Signale ignorieren, wenn die absolute Nettoposition bereits `Volume × MaxPositions` entspricht.

## Parameter
| Name | Typ | Standard | Beschreibung |
|------|-----|----------|-------------|
| `CandleType` | `DataType` | H1 | Haupt-Zeitrahmen zur Bewertung von Pivot-Reaktionen. |
| `ConfirmationCandleType` | `DataType` | M15 | Niedrigerer Zeitrahmen, der die Zwei-Bar-Bestätigung liefert. |
| `Volume` | `decimal` | 0.1 | Netto-Order-Volumen für jeden Einstieg. |
| `MaxPositions` | `int` | 3 | Maximales Netto-Exposure, ausgedrückt als Vielfache von `Volume`. |
| `IndentPips` | `decimal` | 3 | Versatz für pivot-basierte Stop-Loss- und Take-Profit-Berechnungen. |
| `TrailingStopPips` | `decimal` | 150 | Trailing-Stop-Abstand in Pips. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPips` | `decimal` | 5 | Minimale zusätzliche Preisbewegung (in Pips) bevor der Trailing-Stop enger gezogen wird. |

## Hinweise zum Port
- StockSharp verwaltet eine einzelne Nettoposition. Die ursprüngliche Absicherungsfähigkeit wird durch Flättung des entgegengesetzten Exposures bei einem neuen Einstieg emuliert.
- Stop-Loss-, Take-Profit- und Trailing-Logik sind virtuell implementiert. Die Strategie schließt Positionen bei Kerzen-Updates, wenn der Preis die gespeicherten Niveaus kreuzt.
- Wöchentliche Pivots werden jedes Mal neu berechnet, wenn eine neue wöchentliche Kerze empfangen wird. Die Standard-Bestätigung verwendet H1/M15, aber beide Zeitrahmen können über Parameter angepasst werden.
- Alle Kommentare im Quellcode sind gemäß den Konvertierungsrichtlinien auf Englisch verfasst.
