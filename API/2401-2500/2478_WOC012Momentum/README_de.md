# WOC 0.1.2 Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp High-Level-Port des MetaTrader Expert Advisors "WOC.0.1.2". Sie hört auf Level-1-Best-Bid/Ask-Updates und sucht nach schnellen Preisserien auf der Ask-Seite. Wenn der Ask-Preis eine konfigurierbare Anzahl aufeinanderfolgender höherer oder niedrigerer Ticks innerhalb eines begrenzten Zeitfensters druckt, öffnet die Strategie eine Marktposition in Ausbruchsrichtung. Nur eine Position kann zu einem Zeitpunkt offen sein, was das Einzelpositions-Verhalten des ursprünglichen Codes widerspiegelt.

## Daten und Ausführung
- **Marktdaten**: Level-1-Best-Bid und Best-Ask. Der Algorithmus benötigt keine Kerzen oder Indikatoren.
- **Ausführung**: Marktorders. Schutzausstiege werden innerhalb der Strategie durch Überprüfung von Bid/Ask-Updates emuliert.

## Signallogik
1. Den letzten Ask-Preis verfolgen und messen, wie viele aufeinanderfolgende neue Hochs (Up-Streak) oder neue Tiefs (Down-Streak) gedruckt wurden.
2. Wenn ein Up-Streak oder Down-Streak `SequenceLength` erreicht, prüfen, dass die Streak-Dauer kleiner oder gleich `SequenceTimeoutSeconds` Sekunden ist.
3. Wenn der Down-Streak länger als der Up-Streak ist, eine Verkaufsorder senden; sonst eine Kauforder. Die Prüfung reproduziert die ursprüngliche MetaTrader-Logik, bei der der Streak mit dem höchsten Zähler die Richtung definiert.
4. Alle Streak-Zähler nach jedem Eintrittversuch zurücksetzen, um sicherzustellen, dass das nächste Signal von vorne beginnt.

## Positionsmanagement
- **Initialer Stop**: Nach einem Einstieg zeichnet die Strategie sofort einen Stop-Loss-Preis auf, der `StopLossTicks` Preisschritte vom aktuellen Bid (für Longs) oder Ask (für Shorts) entfernt ist.
- **Trailing Stop**: Wenn sich der Preis um mehr als `TrailingStopTicks` Preisschritte zugunsten des Trades bewegt, wird der Stop auf `TrailingStopTicks` hinter dem letzten Bid/Ask gestrafft, solange der Stop mindestens doppelt so weit wie der Trailing-Abstand vom aktuellen Preis entfernt bleibt. Dies reproduziert die zweistufige Trailing-Bedingung aus dem MQL-Expert.
- **Exit-Ausführung**: Wenn das verfolgte Bid/Ask das gespeicherte Stop-Level kreuzt, wird die Position über eine Marktorder geschlossen. Nach dem Exit wird der interne Zustand zurückgesetzt, um neue Streaks zu akzeptieren.

## Volumenverwaltung
Zwei Positionsgrößen-Modi werden unterstützt:
- **Festes Los**: Den Parameter `LotSize` als absolutes Ordervolumen verwenden.
- **Auto-Lots**: `UseAutoLotSizing` aktivieren, um das Kontosaldo auf Volumentiers zu mappen. Das Saldo wird aus `Portfolio.CurrentValue` entnommen und fällt auf `Portfolio.BeginValue` zurück, wenn der aktuelle Wert nicht verfügbar ist.

| Saldo (größer als) | Volumen |
| ------------------- | ------- |
| 0 (Standard)        | `LotSize`
| 200                 | 0.04
| 300                 | 0.05
| 400                 | 0.06
| 500                 | 0.07
| 600                 | 0.08
| 700                 | 0.09
| 800                 | 0.10
| 900                 | 0.20
| 1 000               | 0.30
| 2 000               | 0.40
| 3 000               | 0.50
| 4 000               | 0.60
| 5 000               | 0.70
| 6 000               | 0.80
| 7 000               | 0.90
| 8 000               | 1.00
| 9 000               | 2.00
| 10 000              | 3.00
| 11 000              | 4.00
| 12 000              | 5.00
| 13 000              | 6.00
| 14 000              | 7.00
| 15 000              | 8.00
| 20 000              | 9.00
| 30 000              | 10.00
| 40 000              | 11.00
| 50 000              | 12.00
| 60 000              | 13.00
| 70 000              | 14.00
| 80 000              | 15.00
| 90 000              | 16.00
| 100 000             | 17.00
| 110 000             | 18.00
| 120 000             | 19.00
| 130 000             | 20.00

## Parameter
- `StopLossTicks` – Stop-Loss-Abstand gemessen in Preisschritten.
- `TrailingStopTicks` – Trailing-Abstand gemessen in Preisschritten (kann null sein, um Trailing zu deaktivieren).
- `SequenceLength` – Anzahl der aufeinanderfolgenden Ask-Bewegungen, die vor dem Einstieg in einen Trade erforderlich sind.
- `SequenceTimeoutSeconds` – maximale Dauer des Streaks in Sekunden.
- `LotSize` – feste Ordergröße, die verwendet wird, wenn automatische Losgröße deaktiviert ist.
- `UseAutoLotSizing` – aktiviert die oben gezeigte saldobasierte Volumentabelle.

## Verwendungshinweise
- Funktioniert am besten bei schnellen Instrumenten, bei denen der Best-Ask häufig aktualisiert wird; erwägen Sie das Testen auf Tick-Level-Datenfeeds.
- Die Strategie erfordert Hedging-Konten, da sie niemals gleichzeitig entgegengesetzte Positionen hält.
- Stellen Sie sicher, dass `Security.PriceStep` konfiguriert ist; sonst fallen Stop-Loss- und Trailing-Berechnungen auf einen Abstand von 1 Währungseinheit pro Tick zurück.
- Es wird nur eine offene Position gleichzeitig unterstützt, was das ursprüngliche MQL-Verhalten widerspiegelt.
