# Vector Basket Trendstrategie (MT4-Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ordner enthält den StockSharp High-Level-API-Port des MetaTrader 4 Expert Advisor **Vector** (ursprüngliches Skript: `MQL/8305/Vector.mq4`). Die Strategie koordiniert bis zu vier wichtige Devisenpaare – EURUSD (primär), GBPUSD, USDCHF und USDJPY – und handelt sie in die gleiche Richtung, wenn eine gemeinsame geglättete Ausrichtung des gleitenden Durchschnitts auftritt. Die Konvertierung behält die Kernideen von Vector bei und passt sie gleichzeitig an idiomatische StockSharp-Muster an.

## Handelslogik

1. **Geglättete gleitende Durchschnitte (SMMA)** – jedes Instrument verfolgt einen schnellen (3-Perioden) und einen langsamen (7-Perioden) SMMA, der auf der Grundlage mittlerer Preise des konfigurierbaren Handelszeitraums (standardmäßig 15 Minuten) berechnet wird.
2. **Vektortrendfilter** – die Unterschiede zwischen jedem Schnell/Langsam-Paar werden summiert. Eine positive Summe signalisiert eine synchronisierte Aufwärtsdynamik im gesamten Korb, während eine negative Summe auf einen kollektiven Abwärtsdruck hinweist.
3. **Eingaberegeln** – die Strategie eröffnet oder tauscht Positionen mit Marktaufträgen nur dann aus, wenn:
   - Der Korbtrend ist positiv und der schnelle SMMA des Instruments bleibt über dem langsamen SMMA (Long-Einstieg).
   - Der Korbtrend ist negativ und der schnelle SMMA liegt unter dem langsamen SMMA (Short-Einstieg).
4. **Pip-Ziel aus dem H4-Bereich** – für jedes Instrument misst ein separates 4-Stunden-Kerzenabonnement den vorherigen Bereich. Ein Fünftel dieser Spanne (maximal 13 Pips) wird zum Gewinnziel pro Position und spiegelt den Ausstieg aus dem MT4-Code mit festen Pip wider.
5. **Global Equity Guard** – prozentuale Gewinn- und Drawdown-Schwellenwerte (entnommen aus den ursprünglichen `PrcProfit`- und `PrcLose`-Eingaben) schließen alle offenen Positionen, sobald sie ausgelöst werden.

## Hauptunterschiede zum Original EA

- StockSharps **High-Level-Kerzenabonnements und Indikatorbindung** ersetzen die Low-Level-Abfragen in MT4 (`SubscribeCandles().Bind(...)`).
- Der Port unterstützt **optionale Sekundärinstrumente**: Lassen Sie die GBPUSD-/USDCHF-/USDJPY-Slots leer, um nur das Hauptwertpapier zu handeln.
- Die dynamische Losgröße, die an die MT4-Kontomarge gebunden ist, wurde durch einen sauberen `BaseVolume`-Parameter ersetzt, der auf die Werte `VolumeStep`, `MinVolume` und `MaxVolume` jedes Wertpapiers normalisiert ist.
- Das Handelsmanagement speichert Einstiegspreise über `OnNewMyTrade`-Rückrufe und vermeidet so unzulässige direkte Suche nach Indikatorwerten.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromMinutes(15)` | Zeitrahmen, der für die SMMA-Berechnungen und Eingabeprüfungen verwendet wird. |
| `RangeCandleType` | `TimeSpan.FromHours(4)` | Höherer Zeitrahmen zur Ableitung des adaptiven Pip-Ziels. |
| `SecondSecurity` | `null` | Optionaler GBPUSD-Slot (legen Sie vor dem Start einen `Security` fest). |
| `ThirdSecurity` | `null` | Optionaler USDCHF-Slot. |
| `FourthSecurity` | `null` | Optionaler USDJPY-Slot. |
| `BaseVolume` | `1` | Angefordertes Handelsvolumen pro Order, normalisiert auf Börsenlimits. |
| `TakeProfitPercent` | `0.5` | Globaler Aktiengewinn (in %), der einen Portfolio-weiten Ausstieg auslöst. |
| `MaxDrawdownPercent` | `30` | Maximal zulässiger Aktienrückgang (in %), bevor alle Positionen geschlossen werden. |

## Nutzungshinweise

- Weisen Sie jedem Wertpapier, auf das sich die Parameter beziehen, denselben Connector und dasselbe Portfolio zu, bevor Sie mit der Strategie beginnen.
- Stellen Sie sicher, dass die Datenquelle sowohl den Handelszeitrahmen als auch den Spannenzeitraum für alle Instrumente liefert.
- Wenn die optionalen Sicherheiten nicht bereitgestellt werden, passt sich die Vektorberechnung automatisch an die verfügbaren Instrumente an.
- Ausstiege erfolgen immer mit Marktaufträgen, die dem ursprünglichen MT4-Verhalten entsprechen.

## Dateien

- `CS/VectorStrategy.cs` – C#-Implementierung gemäß den StockSharp-Richtlinien auf hoher Ebene.
- `README.md`, `README_ru.md`, `README_zh.md` – Strategiedokumentation in Englisch, Russisch und Chinesisch.
