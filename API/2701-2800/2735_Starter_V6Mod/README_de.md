# Starter V6 Mod-Strategie (StockSharp-Konvertierung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Starter V6 Mod**-Strategie ist eine Konvertierung des MetaTrader 5 Expert Advisors `Starter_v6mod` auf die StockSharp High-Level-API. Das ursprüngliche System kombiniert einen Laguerre-RSI-Oszillator, zwei exponentielle gleitende Durchschnitte, einen Commodity-Channel-Index-Filter und ein gitterbasiertes Positionsverwaltungsmodul. Dieser Port bewahrt die mehrschichtige Bestätigungslogik und passt die Positionsverwaltung, das Geldmanagement und die Schutzmaßnahmen an die StockSharp-Umgebung an.

## Handelslogik

### Indikatoren

* **Laguerre-RSI-Proxy** – modelliert über einen normalisierten 14-Perioden-RSI zur Emulation der 0-1-Skala des ursprünglichen Laguerre-Oszillators. Das Pegelpaar `LevelDown` / `LevelUp` (Standard 0,15 / 0,85) definiert überverkaufte und überkaufte Zonen.
* **Langsamer EMA (120)** und **Schneller EMA (40)** – beide auf dem medianen Kerzenkurs berechnet. Ihre relative Verschiebung dient als Trendrichtungsfilter. Der Parameter `AngleThreshold` wandelt den EMA-Abstand in eine Tick-Distanz um, die die Handelsrichtungen steuert.
* **Commodity Channel Index (14)** – bestätigt die Impulsrichtung, indem negative Werte für Long-Einstiege und positive für Short-Einstiege gefordert werden.

### Einstiegskriterien

1. Trendrichtung aus dem EMA-Abstand bestimmen:
   * Wenn der langsame EMA minus dem schnellen EMA kleiner als `-AngleThreshold` Ticks ist, dürfen nur Long-Positionen eingegangen werden.
   * Wenn der Abstand größer als `AngleThreshold` ist, dürfen nur Short-Positionen eingegangen werden.
   * Andernfalls gilt der Markt als seitwärts und es werden keine neuen Positionen eröffnet.
2. Wenn der Trendwert eine Richtung erlaubt, werden Oszillator- und Impulsfilter geprüft:
   * Long-Setup – Laguerre-Proxy unter `LevelDown`, langsamer EMA < vorheriger langsamer EMA, schneller EMA < vorheriger schneller EMA, und CCI < 0.
   * Short-Setup – Laguerre-Proxy über `LevelUp`, langsamer EMA > vorheriger langsamer EMA, schneller EMA > vorheriger schneller EMA, und CCI > 0.
3. Gitter-Abstände – beim Stapeln von Positionen in dieselbe Richtung muss der aktuelle Preis mindestens `GridStepPips` unter dem niedrigsten Long-Einstieg oder über dem höchsten Short-Einstieg liegen. Dies repliziert die Mittelungslogik des ursprünglichen EA.
4. Positionsanzahl – die Gesamtzahl der gleichzeitigen Gitter-Einstiege darf `MaxOpenTrades` nicht überschreiten.

### Ausstiegskriterien

* **Laguerre-Ausstiege** – Longs schließen, wenn der Oszillator über `LevelUp` kreuzt; Shorts schließen, wenn er unter `LevelDown` fällt.
* **Stop-Loss / Take-Profit** – in Pips ausgedrückt, in Instrument-Preisschritte umgewandelt. Die Umrechnung berücksichtigt die ursprüngliche Anpassung für Symbole mit 3/5-Dezimalpreisen.
* **Trailing Stop** – aktiviert sich, nachdem der Preis um `(TrailingStopPips + TrailingStepPips)` vorgerückt ist, und folgt dem Preis mit einem Abstand von `TrailingStopPips`.
* **Freitags-Schutz** – nach 18:00 Uhr (Terminalzeit) werden keine neuen Trades erlaubt und alle offenen Positionen werden nach 20:00 Uhr liquidiert.

### Geldmanagement

* **Volumenbestimmung** – entweder fest (`UseManualVolume = true`) oder risikobasiert. Im Risikomodus entspricht das Volumen `(Eigenkapital * RiskPercent) / (StopLoss-Distanz in Preiseinheiten)`.
* **Eigenkapitalgrenze** – der Handel stoppt, wenn das aktuelle Eigenkapital unter `EquityCutoff` fällt.
* **Tagesverlustlimit** – wenn die Strategie an diesem Datum `MaxLossesPerDay` verlierende Ausstiege verzeichnet, werden keine weiteren Positionen eröffnet.
* **Verlustwiederherstellung** – nach jedem verlierenden Ausstieg wird die nächste Positionsgröße durch `DecreaseFactor^heutigeLosses` dividiert, was die ursprüngliche Positionsskalierungslogik widerspiegelt.

## Implementierungshinweise

* Die Konvertierung nutzt die StockSharp High-Level `SubscribeCandles().Bind(...)`-Pipeline, um fertige Kerzen und Indikatorwerte in die Entscheidungslogik zu streamen.
* StockSharp enthält keinen nativen Laguerre-RSI, daher wird ein normalisierter RSI als Proxy verwendet. Die Schwellenwerte entsprechen dem 0-1-Laguerre-Bereich.
* Der EMA-Winkelfilter wird reproduziert, indem der Abstand zwischen den langsamen und schnellen EMA-Werten in Ticks gemessen wird, was ein Richtungs-Gate ähnlich dem ursprünglichen benutzerdefinierten `emaangle`-Indikator bietet.
* Manuelle Stop- und Trailing-Verwaltung werden innerhalb der Kerzenverarbeitungsroutine durchgeführt, um Parität mit den MQL-Trailing-Modifikationen zu wahren.
* Die Gitter-Buchhaltung verfolgt den durchschnittlichen Einstiegspreis, den niedrigsten/höchsten Füllpreis und Trailing-Level, um den MQL-Multi-Positions-Workflow zu emulieren, während innerhalb des aggregierten StockSharp-Positionsmodells gearbeitet wird.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `UseManualVolume` | `false` | Umschalten zwischen fester und risikobasierter Positionierung. |
| `ManualVolume` | `1` | Volumen bei aktivierter manueller Positionierung oder wenn risikobasierte Berechnung nicht möglich ist. |
| `RiskPercent` | `5` | Prozentsatz des Eigenkapitals pro Trade bei aktivierter automatischer Positionierung. |
| `StopLossPips` | `35` | Stop-Loss-Distanz in Pips. |
| `TakeProfitPips` | `10` | Take-Profit-Distanz in Pips. |
| `TrailingStopPips` | `0` | Trailing-Stop-Distanz in Pips (0 deaktiviert Trailing). |
| `TrailingStepPips` | `5` | Mindestvorschub bevor der Trailing-Stop dem Preis zu folgen beginnt. |
| `DecreaseFactor` | `1.6` | Faktor zur Größenreduzierung nach jedem Verlust. |
| `MaxLossesPerDay` | `3` | Maximal erlaubte verlierende Ausstiege pro Kalendertag. |
| `EquityCutoff` | `800` | Eigenkapitalschwelle, die neue Trades stoppt. |
| `MaxOpenTrades` | `10` | Maximale Anzahl gleichzeitiger Gitter-Einstiege. |
| `GridStepPips` | `30` | Mindestabstand zwischen gestapelten Einstiegen in dieselbe Richtung. |
| `LongEmaPeriod` | `120` | Periode des langsamen EMA-Filters. |
| `ShortEmaPeriod` | `40` | Periode des schnellen EMA-Filters. |
| `CciPeriod` | `14` | Commodity-Channel-Index-Periode. |
| `AngleThreshold` | `3` | EMA-Abstands-Schwellenwert in Ticks. |
| `LevelUp` | `0.85` | Oberer Laguerre-Pegel. |
| `LevelDown` | `0.15` | Unterer Laguerre-Pegel. |
| `CandleType` | `15m` | Für Berechnungen verwendeter Kerzen-Zeitrahmen. |

## Verwendungshinweise

1. Konfigurieren Sie den `CandleType`-Parameter passend zum Zeitrahmen des ursprünglichen MT5-Setups (der EA wird oft auf 15-Minuten-Charts eingesetzt).
2. Richten Sie Risikoeinstellungen auf Kontospezifikationen aus. Bei risikobasierter Positionierung stellen Sie sicher, dass `StopLossPips` die Volatilität des Instruments widerspiegelt, da es das berechnete Volumen direkt beeinflusst.
3. Überprüfen Sie die Handelszeiten der Börse. Der eingebaute Freitags-Schutz setzt voraus, dass die Serveruhr mit dem gewünschten Sitzungsende übereinstimmt.
4. Aktivieren Sie die Chart-Zeichnung (über `CreateChartArea`), um EMA, RSI-Proxy, CCI und ausgeführte Trades zur Fehlersuche oder Optimierung zu visualisieren.
5. Beim Übertragen von Parametersätzen aus MT5-Backtests beachten Sie, dass der RSI-Proxy den Laguerre-Oszillator annähert; leichte Schwellenwertanpassungen können nötig sein, um das ursprüngliche Signal-Timing zu erreichen.

## Dateien

* `CS/StarterV6ModStrategy.cs` – StockSharp-Strategie-Implementierung.
* `README.md` – Englische Dokumentation (diese Datei).
* `README_zh.md` – Vereinfachte chinesische Dokumentation.
* `README_ru.md` – Russische Dokumentation.
