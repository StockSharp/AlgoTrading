# Hans123 Trader v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hans123 Trader v2 ist eine Ausbruch-Strategie, die ausstehende Stop-Orders rund um die jüngste Handelsspanne platziert. Sie spiegelt die MetaTrader-Implementierung von Vladimir Karputov wider und ist an die StockSharp High-Level-API angepasst. Das System konzentriert sich darauf, Momentum zu erfassen, wenn der Preis die jüngste 80-Bar-Range verlässt, während es Schutzausstiege und einen Trailing Stop verwaltet.

## Grundidee

- Eine konfigurierbare Kerzenreihe überwachen (standardmäßig 1-Stunden-Bars).
- Während des aktiven Sitzungsfensters das höchste Hoch und niedrigste Tief über die letzten *N* Kerzen berechnen (Standard 80).
- Eine Buy-Stop-Order am höchsten Hoch und eine Sell-Stop-Order am niedrigsten Tief platzieren, wenn der Markt weit genug vom aktuellen Bid/Ask entfernt ist.
- Die Gesamtzahl der aktiven ausstehenden Orders begrenzen, um Überexposure zu vermeiden.
- Sobald eine Position eröffnet wird, die verbleibenden ausstehenden Orders stornieren, Stop-Loss- und Take-Profit-Abstände (in Pips gemessen) anwenden und einen Trailing Stop aktivieren.

## Trade-Management

- **Einstiege**: Stop-Orders werden nur platziert, während die Zeit der verarbeiteten Kerze zwischen den konfigurierten Start- und Endstunden liegt. Orders werden außerhalb dieses Fensters ignoriert.
- **Positionsschutz**: Wenn eine neue Position erstellt wird, registriert die Strategie sofort Schutz-Stop-Loss- und Take-Profit-Orders mit den konfigurierten Pip-Abständen.
- **Trailing Stop**: Falls aktiviert, wird die Stop-Loss-Order näher am Preis neu ausgestellt, sobald sie sich um mehr als den Trailing-Schwellenwert plus Schritt in Positionsrichtung bewegt.
- **Order-Bereinigung**: Das Verlassen einer Position storniert die Schutzorders, und jeder neue Einstieg storniert die entgegengesetzten ausstehenden Orders, was dem Verhalten der ursprünglichen MQL-Logik entspricht.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Ordergröße beim Senden von Ausbruch- und Schutzorders. |
| `StopLossPips` | Abstand in Pips zwischen dem Einstiegspreis und dem Schutz-Stop-Loss. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPips` | Abstand in Pips zwischen dem Einstiegspreis und der Take-Profit-Order. Auf `0` setzen zum Deaktivieren. |
| `TrailingStopPips` | Anfänglicher Trailing-Stop-Abstand in Pips. `0` deaktiviert Trailing. |
| `TrailingStepPips` | Minimaler zusätzlicher Gewinn in Pips, der erforderlich ist, bevor der Trailing Stop erneut bewegt wird. Muss ungleich null sein, wenn Trailing aktiviert ist. |
| `StartHour` | Sitzungseröffnungsstunde (inklusiv) für das Platzieren neuer ausstehender Orders. |
| `EndHour` | Sitzungsschlussstunde (exklusiv) für das Platzieren neuer ausstehender Orders. Muss größer als `StartHour` sein. |
| `MaxPendingOrders` | Maximale Anzahl gleichzeitiger Ausbruchsorders (Kauf + Verkauf) erlaubt. |
| `BreakoutPeriod` | Rückblicklänge (in Kerzen) für die Berechnungen des höchsten Hochs und niedrigsten Tiefs. |
| `CandleType` | Von der Strategie verarbeitete Kerzenreihe (Zeitrahmen oder anderer Kerzendatentyp). |

## Hinweise

- Die Pip-Größe wird vom Preisschritt des Wertpapiers abgeleitet. Für 3- und 5-stellige Forex-Symbole wird der Punktwert angepasst, um der MQL-Definition eines Pips zu entsprechen.
- Die Strategie verlässt sich auf `Security.BestBid`/`BestAsk`-Snapshots wenn verfügbar. Wenn keine Tiefendaten vorhanden sind, greift sie auf den aktuellen Kerzenschlusskurs zurück, um den Mindestabstand vom Markt zu bewerten.
- Schutzorders werden bei Bedarf neu erstellt, was die `PositionModify`-Logik aus dem ursprünglichen Expert Advisor widerspiegelt.
- Die Implementierung hält die Logik rein in C# ohne Python-Übersetzung, wie angefordert.
