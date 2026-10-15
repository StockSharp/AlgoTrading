# Zwei EMA Intraday-Filterstrategien
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den MetaTrader Expert Advisor **Expert_2EMA_ITF** unter Verwendung des StockSharp-High-Level-API. Der Handel erfolgt am Schnittpunkt zweier exponentieller gleitender Durchschnitte und verwendet den durchschnittlichen wahren Bereich (ATR), um ausstehende Limit-Orders, Schutzstopps und Ziele festzulegen. Ein zusätzlicher Intraday-Zeitfilter blockiert Einträge in unerwünschten Minuten, Stunden oder Wochentagen.

## Zusammenfassung der Logik
- Berechnen Sie schnelle und langsame EMA-Werte für die ausgewählte Kerzenserie.
- Erkennen Sie einen bullischen Crossover, wenn der schnelle EMA über den langsamen EMA steigt, und einen bärischen Crossover, wenn er darunter fällt.
- Platzieren Sie bei einem zinsbullischen Crossover eine Kauf-Limit-Order, die vom langsamen EMA um `LimitMultiplier * ATR` plus dem aktuellen Spread versetzt ist. Platzieren Sie bei einem rückläufigen Crossover eine Verkaufs-Limit-Order mit einem Offset in die entgegengesetzte Richtung.
- Speichern Sie Stop-Loss- und Take-Profit-Preise mit ATR-Multiplikatoren, damit sie sofort übermittelt werden können, sobald die Einstiegsorder ausgeführt ist.
- Stornieren Sie ausstehende Bestellungen automatisch, wenn sie länger als `ExpirationBars` Kerzen nicht ausgeführt werden.
- Überspringen Sie Signale, die den Intraday-Filter nicht bestehen (zulässige Minuten-, Stunden- und Tagesprüfungen). Bitmasken können mehrere Minuten, Stunden oder Tage gleichzeitig deaktivieren.

## Indikatoren
- **Schnell EMA** – steuert die Empfindlichkeit der Crossover-Erkennung.
- **Langsam EMA** – definiert die Trendrichtung.
- **Average True Range (ATR)** – misst die Marktvolatilität und skaliert Einstiegs-/Ausstiegspreis-Offsets.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Für Berechnungen verwendeter Zeitrahmen. | 30-Minuten-Kerzen |
| `FastEmaPeriod` | Zeitraum des Fastens EMA. | 5 |
| `SlowEmaPeriod` | Periode des langsamen EMA (muss größer sein als die schnelle Periode). | 30 |
| `AtrPeriod` | ATR Berechnungszeitraum. | 7 |
| `LimitMultiplier` | ATR-Multiplikator, der die Eintrittspreise verschiebt. | 1.2 |
| `StopLossMultiplier` | ATR-Multiplikator für Stop-Loss-Platzierung. | 5 |
| `TakeProfitMultiplier` | ATR-Multiplikator für Take-Profit-Platzierung. | 8 |
| `ExpirationBars` | Anzahl der Balken, nach denen nicht ausgeführte Aufträge storniert werden. | 4 |
| `GoodMinuteOfHour` | Spezifische erlaubte Minute für Einträge (-1 deaktiviert). | -1 |
| `BadMinutesMask` | Bitmaske blockiert Minuten (Bit *n* blockiert Minute *n*). | 0 |
| `GoodHourOfDay` | Bestimmte Stunde, die für Einträge zulässig ist (-1 deaktiviert). | -1 |
| `BadHoursMask` | Bitmaske blockiert Stunden (Bit *n* blockiert Stunde *n*). | 0 |
| `GoodDayOfWeek` | Bestimmter Tag für Einträge zulässig (-1 deaktiviert, 0 = Sonntag). | -1 |
| `BadDaysMask` | Bitmaske blockiert Tage (Bit *n* blockiert Tag *n*, 0 = Sonntag). | 0 |

## Orderverwaltung
1. **Einstiegsaufträge** – Limitaufträge werden mit einem vom langsamen EMA um den ATR-basierten Offset verschobenen Preis registriert. Der Kaufauftrag fügt auch den aktuellen Spread hinzu, wenn Geld-/Briefkurse verfügbar sind.
2. **Ablaufdatum** – Jede ausstehende Order speichert den Kerzenindex zum Zeitpunkt ihrer Erstellung. Wenn `ExpirationBars` positiv ist und die Order über diese Anzahl von Balken hinaus bestehen bleibt, wird sie automatisch storniert.
3. **Schutzaufträge** – Wenn eine Einstiegsorder ausgeführt wird, storniert die Strategie alle vorherigen Stop-/Target-Orders und platziert dann sofort einen Stop-Loss und einen Take-Profit, die aus dem ATR-Snapshot berechnet werden, der das Signal generiert hat. Gegenläufige Schutzanordnungen werden aufgehoben, wenn die Position flach ist.

## Details des Intraday-Filters
- **Einzelne zulässige Werte** – `GoodMinuteOfHour`, `GoodHourOfDay` und `GoodDayOfWeek` beschränken den Handel auf eine bestimmte Minute, Stunde oder einen bestimmten Wochentag, wenn sie nicht negativ sind.
- **Bitmasken** – `BadMinutesMask`, `BadHoursMask` und `BadDaysMask` enthalten Bits, die mehrere Zeitfenster gleichzeitig deaktivieren. Wenn Sie beispielsweise `BadMinutesMask = (1 << 0) | (1 << 30)` festlegen, wird der Handel während Minute 0 und Minute 30 jeder Stunde blockiert.
- **Kombinierte Logik** – Ein Eintrag ist nur zulässig, wenn die aktuelle Kerzenzeit alle Zulassungsbedingungen erfüllt und keine der Masken ihn blockiert.

## Unterschiede zum ursprünglichen Expert Advisor
- Die StockSharp-Version verwendet ausstehende Limit-Orders kombiniert mit expliziten Stop-Loss- und Take-Profit-Registrierungen, sobald der Eintrag ausgeführt wird, was die MQL-Signalberechnungen widerspiegelt.
- Die Spread-Kompensation für Kaufaufträge verwendet die aktuellen `Security.BestBid/BestAsk`-Kurse, sofern diese verfügbar sind, andernfalls ist der Offset Null.
- Die Zeitfilterung wird durch Bitmasken und direkte Vergleiche anstelle von MetaTrader spezifischen Zeitfilter-Hilfsklassen ausgedrückt.
- Alle Handelsaktionen nutzen StockSharp High-Level-Helfer (`BuyLimit`, `SellLimit`, `SellStop`, `BuyStop`) und eine automatische Stornierungslogik anstelle manueller Order-Arrays.

## Nutzungshinweise
- Stellen Sie sicher, dass das Strategievolumen festgelegt ist, bevor Sie mit der Strategie beginnen. Andernfalls wird eine Warnung ausgegeben und es werden keine Bestellungen gesendet.
- Für Optimierungsszenarien ermöglichen die Parametermetadaten bereits die Optimierung von EMA-Zeiträumen, ATR-Zeiträumen, Multiplikatoren und Ablauflängen.
- Die Strategie geht davon aus, dass die Schlusszeiten der Kerzen das Ende des Balkens darstellen und verwendet diese bei der Auswertung von Intraday-Filtern.
