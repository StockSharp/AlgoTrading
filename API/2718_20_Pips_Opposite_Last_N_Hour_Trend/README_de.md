# 20-Pips-Gegenteil-des-Letzten-N-Stunden-Trends-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie ist eine High-Level-Portierung des MetaTrader-Experten
**«20 Pips Opposite Last N Hour Trend»**. Sie beobachtet stündliche Kerzen, misst
wie sich der Preis während der vorherigen `N` Stunden verhalten hat, und öffnet dann eine Position in
der entgegengesetzten Richtung, wenn die konfigurierte Handelsstunde endet. Der Trade wird
mit einem festen 20-Pip-Take-Profit-Ziel und einem stündlichen Timeout verwaltet, während
eine Martingale-artige Volumenstaffel nach aufeinanderfolgenden Verlusten angewendet wird.

Die Implementierung verwendet StockSharp's Kerzenabonnements, das Parametersystem,
und Order-Helfer (`BuyMarket`, `SellMarket`), sodass sie unverändert innerhalb von
Designer, API, Runner oder Shell ausgeführt werden kann.

## Handelslogik

- Die Strategie abonniert den ausgewählten Kerzentyp (Standard: 1-Stunden-Bars).
- Für jede fertige Kerze speichert sie den Schlusskurs in einem internen Verlauf.
- Wenn eine Kerze mit `OpenTime.Hour == TradingHour` abgeschlossen ist und genügend
  Verlauf vorhanden ist:
  - Vergleichen Sie den Schluss, der `HoursToCheckTrend` Bars zurück lag, mit dem
    vorherigen Schluss (1 Bar zurück).
  - Wenn der Preis in diesem Fenster gefallen ist (bärische Drift) kauft die Strategie;
    wenn der Preis gestiegen ist (bullische Drift) verkauft sie. Gleiche Schlüsse überspringen den Trade.
- Nur ein Trade wird pro Tag geöffnet und ausschließlich zur konfigurierten Handelsstunde.
  Alle anderen Kerzen werden ausschließlich für die Verwaltung verwendet.

## Positionsverwaltung

- Ein 20-Pip-Ziel (angepasst für 3/5-stellige Symbole) wird direkt nach dem
  Einstieg berechnet. Wenn eine fertige Kerze zeigt, dass das Hoch/Tief das Ziel berührt hat, wird die
  Position auf diesem Level geschlossen.
- Wenn das Ziel nicht während der nächsten Stunde erreicht wird, wird die Position am
  Ende der folgenden Kerze geschlossen, um Übernacht-Exposition zu vermeiden.
- Tageszähler werden automatisch zurückgesetzt, wenn ein neuer Handelstag beginnt, damit
  das nächste berechtigte Signal in der folgenden Sitzung auslösen kann.

## Geldmanagement

- `Volume` setzt die Basisordergröße. `MaxVolume` begrenzt die resultierende Größe jedes
  Martingale-Schritts.
- Nach einem verlierenden Ausstieg erhöht die Strategie die nächste Position um den
  entsprechenden Multiplikator: erster Verlust → `FirstMultiplier`, zweiter Verlust →
  `SecondMultiplier`, usw. Verlustreihen über fünf Trades hinaus verwenden den fünften
  Multiplikator. Jeder profitable oder Breakeven-Schluss setzt die Sequenz zurück.
- Volumenberechnungen basieren auf dem zuletzt ausgeführten Positionspreis, sodass die Gewinn/Verlust-
  Erkennung auch ohne vollständige Broker-PnL-Daten deterministisch bleibt.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|--------------|
| `MaxPositions` | 9 | Maximale Trades erlaubt pro Tag. Auf 0 setzen zum Deaktivieren. |
| `Volume` | 0.1 | Basisvolumen für den ersten Trade einer Reihe. |
| `MaxVolume` | 5 | Hartes Limit für das angepasste Volumen nach Multiplikatoren. |
| `TakeProfitPips` | 20 | Take-Profit-Abstand in Pips. 0 deaktiviert den TP. |
| `TradingHour` | 7 | Stunde des Tages (0-23), die für das Öffnen einer Position berechtigt ist. |
| `HoursToCheckTrend` | 24 | Anzahl der stündlichen Schlüsse zur Messung des vorherigen Trends. |
| `FirstMultiplier` | 2 | Multiplikator nach dem ersten aufeinanderfolgenden Verlust. |
| `SecondMultiplier` | 4 | Multiplikator nach dem zweiten aufeinanderfolgenden Verlust. |
| `ThirdMultiplier` | 8 | Multiplikator nach dem dritten aufeinanderfolgenden Verlust. |
| `FourthMultiplier` | 16 | Multiplikator nach dem vierten aufeinanderfolgenden Verlust. |
| `FifthMultiplier` | 32 | Multiplikator ab dem fünften Verlust aufwärts. |
| `CandleType` | H1 | Kerzendatentyp für Signalgenerierung und Verwaltung. |

## Zusätzliche Hinweise

- Die Pip-Größe wird aus `Security.PriceStep` und der Anzahl der Dezimalstellen berechnet, damit
  das 20-Pip-Ziel auf 4- und 5-stelligen FX-Symbolen korrekt funktioniert.
- `StartProtection()` wird beim Start der Strategie aufgerufen und aktiviert die eingebauten
  StockSharp-Schutzmaßnahmen (Auto-Stop für ungebundene Positionen, Portfolio-Resets).
- Die Logik verwendet nur fertige Kerzen und liest keine Indikatorwerte
  direkt, was den Richtlinien aus `AGENTS.md` entspricht.

> **Risikohinweis:** Martingale-artiges Positions-Sizing kann zu erheblichen
> Drawdowns führen. Testen Sie die Parameter immer an historischen Daten und verwenden Sie umsichtige
> Risikolimits, bevor Sie im Live-Trading einsetzen.
