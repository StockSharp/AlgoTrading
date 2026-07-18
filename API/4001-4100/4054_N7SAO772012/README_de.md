# N7S AO 772012 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Umsetzung des MetaTrader-Expertenberaters **N7S_AO_772012**. Der ursprüngliche Roboter kombiniert Perzeptron-ähnliche Filter des Awesome Oscillator (AO) über mehrere Zeitrahmen hinweg mit einem Preismuster-Gate und einem konfigurierbaren „Neuro“-Modus, der die Basislogik außer Kraft setzen kann. Die konvertierte Version behält den Entscheidungsbaum bei und stellt alle Abstimmknöpfe als Strategieparameter bereit.

Der Bot arbeitet mit dem in der Strategie ausgewählten primären Instrument und verwendet:

- **M1-Kerzen** für den Einstiegszeitpunkt und die Preiswahrnehmung.
- **H1-Kerzen** zur Versorgung mehrerer AO-basierter Perzeptrone.
- **H4-Kerzen** zur Berechnung des AO-Momentum-Deltas, das vom Basis-Kauf/Verkauf-Selektor (BTS) verwendet wird.

## Handelslogik

1. Bei jeder abgeschlossenen M1-Kerze aktualisiert die Strategie die Preismusterhistorie, verwaltet bestehende Positionen und bewertet, ob der Handel zulässig ist (kein Handel am Montag vor 02:00 Uhr oder am Freitag ab 18:00 Uhr und später, Ortszeit der Plattform).
2. Stündliche AO-Werte werden in fünf Perzeptronen zusammengefasst:
   - `Perceptron X/Y` – Basis-BTS-Filter, die mit dem Preisperzeptron und dem H4-AO-Delta zusammenarbeiten.
   - `Neuro X/Y` – erweiterte Lang-/Kurzfilter, die verwendet werden, wenn der Neuromodus ihnen Priorität einräumt.
   - `Neuro Z` – Gating-Perzeptron, das Neuro X im Modus 4 aktiviert.
3. Das Preisperzeptron bewertet die gewichteten Unterschiede zwischen den jüngsten M1-Eröffnungen und dem letzten Schlusskurs.
4. Der Parameter **Neuro-Modus** steuert, wie die Großbuchstaben-Perzeptrone eingreifen:
   - `4`: Wenn Neuro Z > 0, kann nur Neuro X ein langes Signal erzeugen; Andernfalls kann Neuro Y einen Kurzschluss auslösen. Wenn keines von beiden ausgelöst wird, greifen Sie auf BTS zurück.
   - `3`: Neuro Y kann Kurzschlüsse auslösen; Andernfalls greifen Sie auf BTS zurück.
   - `2`: Neuro X kann Long-Positionen auslösen; Andernfalls greifen Sie auf BTS zurück.
   - Jeder andere Wert überspringt die Neuroschicht und wertet BTS direkt aus.
5. Der **BTS**-Block verwendet das Preisperzeptron und das H4-AO-Delta als Tore:
   - Langes Setup: Preisperzeptron > 0 (außer `BtsMode = 0`), Neuro/BTS X > 0 und H4 AO-Delta > 0. Stop-Loss ist `BaseStopLossPointsLong`, Take-Profit ist `BaseTakeProfitFactorLong × BaseStopLossPointsLong`.
   - Kurzer Aufbau: Preisperzeptron < 0 (außer `BtsMode = 0`), Neuro/BTS Y > 0 und H4 AO-Delta < 0. Stop-Loss ist `BaseStopLossPointsShort`, Take-Profit ist `BaseTakeProfitFactorShort × BaseStopLossPointsShort`.
6. Nachdem ein Signal akzeptiert wurde, eröffnet die Strategie eine Marktorder (unter Berücksichtigung der aktivierten Richtung). Schutzpreise werden intern verfolgt; Jede abgeschlossene M1-Kerze prüft anhand der Kerzenhochs/-tiefs, ob der Stopp oder das Ziel erreicht wurde, und schließt die Position gegebenenfalls. Entgegengesetzte Signale schließen zunächst die bestehende Position und warten auf die nächste Kerze, bevor sie wieder eintreten.

## Parameter

### Handel
- **OrderVolume** – Basisvolumen für alle Marktaufträge.
- **AllowLongTrades / AllowShortTrades** – Long- oder Short-Einträge aktivieren oder deaktivieren.
- **BtsMode** – Bei Einstellung auf `0` wird das Preiswahrnehmungstor in BTS ignoriert; andernfalls muss sein Vorzeichen mit dem Handel übereinstimmen.
- **NeuroMode** – Wählt aus, wie die erweiterten Perzeptrone teilnehmen (siehe Abschnitt „Logik“).

### Basis-BTS-Perzeptrone
- **BaseStopLossPointsLong / BaseTakeProfitFactorLong** – Stop-Distanz (Punkte) und Multiplikator für Long-Take-Profit.
- **BaseStopLossPointsShort / BaseTakeProfitFactorShort** – Analoge Einstellungen für Short-Trades.
- **PerceptronPeriodX / Y** – AO-Verschiebung (in H1-Balken), die vom jeweiligen Perceptron verwendet wird.
- **PerceptronWeightX1..4 / Y1..4** – Gewichte (0–100) der Perceptron-Eingaben; Intern werden sie durch Subtraktion von 50 zentriert.
- **PerceptronThresholdX / Y** – Minimale absolute Perceptron-Ausgabe, die erforderlich ist, bevor sie als gültig gilt.

### Preisfilter
- **PricePatternPeriod** – Anzahl der M1-Kerzen, die jede Verzögerung im Preisperzeptron bilden.
- **PriceWeight1..4** – Gewichtungen (zentriert um 50), die auf Preisunterschiede innerhalb des Perzeptrons angewendet werden.

### Neuroperzeptrone
- **NeuroStopLossPointsLong / NeuroTakeProfitFactorLong** – Stop- und TP-Multiplikator, der von Neuro-X-Signalen verwendet wird.
- **NeuroStopLossPointsShort / NeuroTakeProfitFactorShort** – Stop- und TP-Multiplikator, der von Neuro-Y-Signalen verwendet wird.
- **NeuroPeriodX / Y / Z** – AO-Verschiebung (H1-Kerzen) für die drei Neuroperzeptrone.
- **NeuroWeightX1..4 / NeuroWeightY1..4 / NeuroWeightZ1..4** – Perceptron-Gewichte.
- **NeuroThresholdX / NeuroThresholdY / NeuroThresholdZ** – Minimaler absoluter Wert für jedes Neuroperzeptron.

### Daten
- **CandleType** – Zeitrahmen, der für die primären Handelskerzen verwendet wird (Standard 1 Minute).

## Handelsmanagement

- Stop-Loss- und Take-Profit-Distanzen werden mithilfe der Instrumentenpreisstufe von Punkten in absolute Preise umgerechnet. Wird ein Abstand auf Null gesetzt, ist der entsprechende Schutz deaktiviert.
- Schutzniveaus werden bei abgeschlossenen M1-Kerzen überwacht, indem die Kerzenhochs/-tiefs mit den gespeicherten Preisen verglichen werden.
- Die Strategie funktioniert im Netting-Modus: Es werden niemals sowohl Long- als auch Short-Positionen gleichzeitig gehalten. Ein entgegengesetztes Signal schließt zuerst die aktuelle Position.

## Hinweise zur Umstellung

- Hochrangige StockSharp-Bindungen (`SubscribeCandles().Bind(...)`) werden zum Streamen von AO-Werten ohne direkte Indikatorabfragen verwendet.
- Historische Puffer werden als Listen fester Größe geführt, um die ursprüngliche schichtbasierte Indizierung zu emulieren und gleichzeitig direkte Indikatorsuchen zu vermeiden.
- Wie gewünscht wird keine Python-Version bereitgestellt.
- Tests wurden nicht geändert.
