# ASCV-Pivot-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die ASCV Pivot Breakout-Strategie ist eine High-Level-StockSharp-Portierung des MetaTrader 4 Expertenberaters „ASCV“ (Datei `Avpb.mq4`). Der ursprüngliche Roboter kombiniert zwei benutzerdefinierte Indikatoren (ASCTrend1sig und BrainTrend1Sig), einen Standardabweichungsfilter, tägliche Pivot-Levels und eine Intraday-Volumenbeschleunigung, um Breakout-Fortsetzungs-Setups innerhalb eines eingeschränkten Handelsfensters zu handeln. Da die proprietären benutzerdefinierten Indikatoren in StockSharp nicht verfügbar sind, stellt die Konvertierung ihr Verhalten durch eine Mischung aus gleitenden Durchschnitten, stochastischem Momentum und täglichen Pivot-Analysen wieder her und behält dabei die Verwaltungsregeln von EA bei.

## Handelslogik

1. **Sitzungsfilter** – Trades sind nur zwischen den konfigurierten Start- und Endzeiten zulässig (Standard 02:00–20:00 Uhr Brokerzeit). Stündliche Zurücksetzungen reproduzieren die MQL-Logik, die Eintragsflags bei jedem `Minute()==0` löscht.
2. **Volatilitätsgate** – ein auf dem ausgewählten Zeitrahmen basierender Standardabweichungsindikator muss über einem konfigurierbaren Schwellenwert liegen. Dies spiegelt den ursprünglichen `iStdDev`-Aufruf wider, der einen aktiven Markt erforderte, bevor Einträge berücksichtigt wurden.
3. **Trendbestätigung** – eine schnelle und eine langsame einfache gleitende Durchschnittsschätzung des Richtungsfilters, den ASCTrend/BrainTrend bereitgestellt hat. Ein Long-Signal erfordert, dass der schnelle Durchschnitt über dem langsamen liegt und die Kerze über dem täglichen Pivot schließt. Shorts erwarten die entgegengesetzte Konfiguration.
4. **Momentum-Bestätigung** – ein stochastischer Oszillator stellt sicher, dass bullische Ausbrüche mit einem positiven `%K-%D`-Momentum auftreten und dass bärische Gelegenheiten ein negatives Momentum haben. Die absolute Spanne zwischen `%K` und `%D` wird als adaptiver Exit-Trigger wiederverwendet, genau wie EA auf der Differenz der stochastischen Haupt-/Signallinien beruht.
5. **Volumenbeschleunigung** – das Kerzenvolumen muss das vorherige Kerzenvolumen um das konfigurierte Delta (Standard 30 Kontrakte) überschreiten, um sich dem `Volume[0]-Volume[1]`-Filter anzunähern.
6. **Auftragserteilung** – Die Strategie verwendet Marktaufträge (`BuyMarket`/`SellMarket`) mit festem Volumen. In Übereinstimmung mit dem Fachberater ist pro Stunde nur ein Trade pro Richtung zulässig.
7. **Stopps und Ziele** – Stopps werden an der nächstgelegenen Pivot-Unterstützung/Widerstand (S1/S2 oder R1/R2) platziert. Wenn diese Niveaus zu nahe beieinander liegen, werden in Preisschritten ausgedrückte Rückfallentfernungen angewendet. Gewinnziele folgen derselben Hierarchie: R2/R1/Pivot für Long-Positionen und S2/S1/Pivot für Short-Positionen. Eine Fallback-Distanz emuliert das EA-Verhalten, wenn Pivots nicht verfügbar waren.
8. **Dynamisches Management** – der stochastische Spread führt zu frühen Ausstiegen bei Verlust der Dynamik. Ein in Preisschritten gemessener Trailing Stop spiegelt die progressiven Stop-Loss-Änderungen aus der MQL-Version wider.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zeitrahmen für Indikatorberechnungen und Signalverarbeitung. | 15-Minuten-Kerzen |
| `StartHour` / `EndHour` | Inklusive Stundengrenzen der Handelssitzung. | 2 / 20 |
| `FastMaLength` | Zeitraum des schnellen SMA-Trendfilters. | 10 |
| `SlowMaLength` | Zeitraum des langsamen SMA-Trendfilters. | 40 |
| `StdDevLength` | Lookback-Länge des Standardabweichungs-Volatilitätsfilters. | 10 |
| `StdDevThreshold` | Für den Handel erforderliche Mindeststandardabweichung. | 0,0005 |
| `VolumeDeltaThreshold` | Minimale Differenz zwischen aktuellem und vorherigem Kerzenvolumen. | 30 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Perioden des stochastischen Oszillators. | 5 / 3 / 3 |
| `StochasticExitDelta` | Absoluter Spread von `%K-%D`, der Momentum-Exits auslöst. | 5 |
| `TrailingStopSteps` | Entfernung des Trailing Stops in Preisschritten. | 30 |
| `MinPivotDistanceSteps` | Minimaler Abstand (in Schritten), der für Pivot-basierte Ziele erforderlich ist. | 50 |
| `StopFallbackSteps` | Stoppdistanz, wenn keine Pivotunterstützung/Widerstand weit genug entfernt ist. | 33 |
| `TakeProfitBufferSteps` | Fallback-Gewinnentfernung in Preisschritten. | 50 |
| `OrderVolume` | Volumen für jede Marktorder. | 1 |

Alle Abstände werden in Instrumentenpreisschritten definiert, um die Kompatibilität mit den Börsenspezifikationen sicherzustellen.

## Implementierungshinweise

- Die Strategie verwendet das High-Level-Muster `SubscribeCandles().BindEx(...)`. Indikatoren werden **nicht** zu `Strategy.Indicators` hinzugefügt, entsprechend der Anleitung von StockSharp.
- Die Pivot-Level werden einmal pro Handelstag anhand der Höchst-, Tiefst- und Schlusskurse des Vortages neu berechnet. Am ersten Tag werden nur Daten erfasst und mit dem Handel begonnen, sobald der zweite Tag beginnt.
- `StartProtection()` ist aktiviert, um automatisch vor unerwarteten Verbindungsabbrüchen zu schützen und das Sicherheitsnetz von EA nachzubilden.
- XML und Inline-Kommentare im C#-Code erläutern die Zuordnung jedes Blocks zur ursprünglichen MQL-Logik.
- Stop-Loss- und Take-Profit-Werte werden über `SetStopLoss`/`SetTakeProfit` mithilfe von Preisschrittkonvertierungen festgelegt, um Broker-unabhängig zu bleiben.

## Nutzungstipps

1. Führen Sie die Strategie auf einem Instrument aus, das sowohl Kerzendaten als auch Volumen offenlegt, da der Volumenbeschleunigungsfilter unerlässlich ist.
2. Konzentrieren Sie sich bei der Optimierung zunächst auf die Filter „Volatilität“ (`StdDevThreshold`) und „Volumen“ (`VolumeDeltaThreshold`). Der ursprüngliche Filter EA reagierte sehr empfindlich auf ruhige Märkte.
3. Passen Sie die Pivot-Abstände an das Volatilitätsprofil des gehandelten Symbols an. Erhöhen Sie bei Instrumenten mit großer Tick-Größe `MinPivotDistanceSteps`, um vorzeitige Ausstiege zu vermeiden.
4. Wenn der stochastische Spread zu viele Exits erzeugt, erweitern Sie `StochasticExitDelta`, sodass der Trailing Stop zur dominanten Exit-Bedingung wird.

## Dateien

- `CS/AscvStrategy.cs` – die C#-Implementierung der Strategie.
- `README.md` – diese Dokumentation.
- `README_ru.md` – Russische Übersetzung.
- `README_zh.md` – Chinesische Übersetzung.
