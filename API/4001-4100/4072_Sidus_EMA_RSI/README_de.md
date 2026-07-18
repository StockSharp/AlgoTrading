# Sidus EMA RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4 Expert Advisors **Exp_Sidus.mq4**. Es reproduziert die ursprüngliche Logik
kombiniert einen schnellen/langsamen EMA-Crossover mit einem 50-stufigen RSI-Filter. Signale werden nur für abgeschlossene Kerzen ausgewertet, und jede Kerze kann dies tun
spawnen höchstens eine Reihenfolge, entsprechend der Timing-Disziplin des Quellroboters.

## Handelslogik

- **Indikatorstapel**
  - Schneller exponentieller gleitender Durchschnitt (Standardzeitraum 5)
  - Langsamer exponentieller gleitender Durchschnitt (Standardzeitraum 12)
  - Relative-Stärke-Index (Standardzeitraum 21)
- **Bulles Setup**
  1. Der schnelle EMA war kleiner oder gleich dem langsamen EMA der vorherigen Signalkerze.
  2. Der schnelle EMA liegt über dem langsamen EMA der aktuellen Signalkerze.
  3. RSI auf derselben Kerze ist unbedingt größer als 50.
- **Bearisches Setup**
  1. Der schnelle EMA lag über oder gleich dem langsamen EMA der vorherigen Signalkerze.
  2. Der schnelle EMA liegt unter dem langsamen EMA der aktuellen Signalkerze.
  3. RSI auf derselben Kerze ist unbedingt kleiner als 50.
- **Signalverschiebung** – der Parameter `SignalShift` (Standard `1`) definiert, welche geschlossene Kerze als „aktueller“ Signalbalken gilt.
Ein Wert von `1` verwendet die letzte geschlossene Kerze, `0` verwendet die gerade geschlossene Kerze, `2` blickt zwei Balken zurück und so weiter. Der Vorherige
Kerze für die Crossover-Erkennung wird automatisch als `SignalShift + 1` berechnet.
- **Doppelter Schutz** – die Strategie speichert die Öffnungszeit der Signalkerze und eröffnet nie eine andere damit verbundene Position
Dieselbe Leiste, die den `LastTime`-Check im Original EA originalgetreu nachahmt.

## Positionsmanagement

- Es existiert immer nur eine Position.
- Wenn ein entgegengesetztes Signal erscheint, während eine Position offen ist, schließt die Strategie zunächst die bestehende Position und wartet dann auf das
nächsten Verarbeitungsdurchgang, um einen Trade in die neue Richtung zu eröffnen, genau wie die Version MQL.
- `StartProtection` fügt optionale Take-Profit- und Stop-Loss-Klammern hinzu, ausgedrückt in Preispunkten (Preisschritten). Entfernungen sind
abgeleitet aus den Eingaben der ursprünglichen EA: Standard-Take-Profit-`80`-Punkte und Stop-Loss-`20`-Punkte.

## Parameter

| Name | Beschreibung | Standard | Notizen |
| ---- | ----------- | ------- | ----- |
| `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten. | `80` | Legen Sie `0` fest, um das Ziel zu deaktivieren. |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. | `20` | Legen Sie `0` fest, um den Schutz zu deaktivieren. |
| `TradeVolume` | Auftragsvolumen (Lose/Verträge). | `0.1` | Wird beim Start der Basiseigenschaft `Volume` zugewiesen. |
| `FastPeriod` | Schnelle Länge von EMA. | `5` | Optimierbar. |
| `SlowPeriod` | Langsame Länge von EMA. | `12` | Optimierbar. |
| `RsiPeriod` | RSI Länge. | `21` | Optimierbar. |
| `SignalShift` | Anzahl der geschlossenen Kerzen, die für Signalberechnungen verwendet werden. | `1` | Spiegelt die `shif`-Eingabe des MT4 EA. |
| `CandleType` | Kerzenquelle für das Abonnement. | `1h` Zeitrahmen | Kann auf jedes von der Umgebung unterstützte `DataType` eingestellt werden. |

## Implementierungshinweise

- Kerzendaten werden über `SubscribeCandles(CandleType)` abonniert und in `ProcessCandle` erst verarbeitet, nachdem die Kerze erreicht ist
der `Finished`-Zustand.
- Die Indikatorwerte werden in einer kurzen Warteschlange zwischengespeichert, sodass die Strategie auf die aktuellen und vorherigen von angegebenen Balken zugreifen kann
`SignalShift`, ohne Indikatormethoden wie `GetValue` aufzurufen, unter Einhaltung der Repository-Richtlinien.
- Die Handelsausführung verwendet `BuyMarket`/`SellMarket`, sobald die Strategie flach ist; wenn eine Position in der entgegengesetzten Richtung existiert,
`ClosePosition` wird zuerst ausgegeben, sodass der Bestellablauf mit dem des ursprünglichen Roboters identisch bleibt.
- Alle Laufzeitprotokolle sind auf Englisch verfasst, um einen klaren Prüfpfad zu gewährleisten.

## Konvertierungshinweise

- Die Take-Profit- und Stop-Loss-Abstände vervielfachen das Instrument `PriceStep` und reproduzieren das Verhalten von MetaTrader `Point`.
- Die Lautstärke ist standardmäßig auf `0.1` eingestellt, genau wie die Eingabe `Lots` in der Quelle MQL.
- RSI-Schwellenwerte sind bei 50 fest codiert, um die ursprüngliche Implementierung widerzuspiegeln.
