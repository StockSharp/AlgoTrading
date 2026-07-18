# Binario-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Binario ist ein Stop-Entry-Ausbruchssystem, das den Preis mit zwei gleitenden Durchschnitts-Envelopes umgibt, die auf Kerzenhochs und -tiefs berechnet werden. Wenn der Preis zwischen den Envelopes handelt, platziert die Strategie symmetrische Stop-Orders, um die nächste direktionale Expansion zu erfassen. Orders übernehmen feste Stop-Loss- und Take-Profit-Abstände, die dem MetaTrader 5-Expertenberater entsprechen.

Der StockSharp-Port behält die Kernidee bei und nutzt gleichzeitig High-Level-API-Funktionen wie Kerzen-Abonnements, Indikator-Bindung und automatisiertes Order-Management. Level-1-Daten werden verwendet, um den aktuellen Bid/Ask-Spread zu schätzen, der zur Reproduktion der ursprünglichen Einstiegs-Abstände erforderlich ist.

## Handelslogik
1. Zwei gleitende Durchschnitte erstellen (oberer auf Hochs, unterer auf Tiefs) mit konfigurierbaren Methoden und Periode.
2. Wenn der letzte Schlusskurs zwischen den Durchschnitten liegt:
   - Eine Buy-Stop-Order über dem oberen Durchschnitt plus dem konfigurierten Differenz-Buffer und dem aktuellen Spread platzieren.
   - Eine Sell-Stop-Order unter dem unteren Durchschnitt minus demselben Buffer platzieren.
3. Jede ausstehende Order speichert ihre eigenen Stop-Loss- und Take-Profit-Levels, die aus den gleitenden Durchschnitten, `PointValue` und pip-basierten Parametern abgeleitet werden.
4. Wenn eine Order ausgeführt wird, wird die entgegengesetzte ausstehende Order storniert und neue Schutzorders (Stop-Loss und Take-Profit) für die offene Position registriert.
5. Die Trailing-Stop-Logik zieht den Stop enger, wenn der Preis sich mindestens `TrailingStopPips + TrailingStepPips` vom Einstandspreis entfernt, und entspricht dem inkrementellen Verhalten der MQL-Implementierung.
6. Wenn die Position von Long zu Short wechselt (oder umgekehrt), werden bestehende Schutzorders storniert, um Konflikte zu vermeiden.

## Parameter
- `CandleType` – Zeitrahmen für Berechnungen.
- `MaPeriod` – Länge beider gleitender Durchschnitte.
- `MaShift` – Balkenversatz für jeden gleitenden Durchschnitt (0 reproduziert das Standard-EA-Verhalten).
- `HighMaMethod` / `LowMaMethod` – Glättungsmethoden (`SMA`, `EMA`, `SMMA`, `WMA`, `LWMA`).
- `PointValue` – absoluter Preiswert, der einen Pip für das gehandelte Symbol darstellt (0.0001 für die meisten FX-Hauptpaare, 0.01 für JPY-Paare usw.).
- `DifferencePips` – Buffer zwischen den Durchschnitten und den ausstehenden Orders, ausgedrückt in Pips.
- `TakeProfitPips` – Gewinnziel-Abstand in Pips.
- `TrailingStopPips` – Trailing-Stop-Abstand in Pips (auf null setzen, um Trailing zu deaktivieren).
- `TrailingStepPips` – mindestens zusätzlicher Gewinn in Pips, der erforderlich ist, bevor der Stop erneut angezogen wird.
- `Volume` (geerbt von `Strategy`) – Basis-Ordergröße; Umkehrorders addieren automatisch den absoluten Positionsumfang, um die Exposition vollständig zu wenden.

Alle pip-basierten Parameter werden über `PointValue` in absolute Preise umgerechnet, was der `Point * digits_adjust`-Konvertierung der MT5-Version entspricht.

## Order-Management
- Ausstehende Stop-Orders bleiben nur aktiv, während die Strategie auf der jeweiligen Seite flat ist (keine Long-Position für einen neuen Buy-Stop, keine Short-Position für einen neuen Sell-Stop).
- Nach einem Einstieg sendet die Strategie passende Stop-Loss- und Take-Profit-Orders und entfernt den nicht genutzten entgegengesetzten Stop-Entry.
- Positions-Umkehrungen stornieren veraltete Schutzorders, bevor neue registriert werden, um verwaiste Stops zu verhindern.

## Trailing-Verhalten
- Long-Positionen: Sobald der Preis mindestens `TrailingStopPips + TrailingStepPips` Pips gewinnt, wird der Stop auf `close - TrailingStopPips` verschoben, solange die Bewegung den vorherigen Stop um mindestens `TrailingStepPips` überschreitet.
- Short-Positionen: Wenn der Preis um denselben Schwellenwert fällt, wird der Stop auf `close + TrailingStopPips` gesenkt, wobei auch der Schritt-Filter eingehalten wird.
- Trailing verwendet den letzten Kerzenschlusskurs als Ersatz für den MT5-`PriceCurrent()`-Wert.

## Datenanforderungen
- Kerzen für den ausgewählten `CandleType`.
- Level-1-Kurse zum Abrufen der besten Bid/Ask-Preise und zur Berechnung des Spreads. Wenn der Spread nicht verfügbar ist, fällt die Strategie auf den minimalen Preisschritt des Instruments oder `PointValue` zurück.

## Unterschiede zur MetaTrader 5-Version
- Die Positionsgröße wird über die StockSharp-`Volume`-Eigenschaft anstatt der ursprünglichen Lots/Risk-Kombination gesteuert.
- Schutzorders werden neu erstellt, wenn Trailing Preise ändert, da StockSharp-Stop-Orders nicht direkt geändert werden können.
- Die von MyTrades gemeldeten Ausführungspreise werden durch gespeicherte Order-Preise approximiert; passen Sie `PointValue` und Pip-Parameter an die Broker-Spezifikationen an.
- Die Strategie läuft auf abgeschlossenen Kerzen, was dem Aktivieren von "Experte bei jedem Tick" mit Balken-Öffnungsbewertung im MT5-Skript entspricht.

## Verwendungshinweise
1. `PointValue` entsprechend der Tick-zu-Pip-Beziehung des Instruments festlegen.
2. Gleitende Durchschnittsmethoden und -periode konfigurieren, um der MT5-Vorlage zu entsprechen.
3. Geeignete Pip-Abstände für Differenz-, Take-Profit- und Trailing-Komponenten wählen.
4. Sicherstellen, dass Level-1-Daten verfügbar sind, damit die Spread-Komponente genau angewendet werden kann.
