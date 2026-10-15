# Trickerless RHMP-Strategie (StockSharp Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den Expertenberater **Trickerless RHMP** von MetaTrader auf das hohe Niveau von StockSharp API. Es bleibt die Mehrstufigigkeit erhalten
Eingabelogik des ursprünglichen Roboters – Kombination aus Bestätigung des durchschnittlichen Richtungsindex, geglätteter gleitender Durchschnittsstruktur und
Volatilitätsgesteuertes Positionsmanagement – unter Einhaltung der in `AGENTS.md` dokumentierten Rahmenkonventionen.

## Handelslogik

1. **Indikatoren**
   - Durchschnittlicher wahrer Bereich (ATR) mit konfigurierbarem Zeitraum für die Volatilitätsgröße.
   - Durchschnittlicher Richtungsindex (ADX) mit vollständigen +DI/-DI-Komponenten zur Qualifizierung der Trendstärke.
   - Zwei geglättete gleitende Durchschnitte (SMMA), die die schnellen und langsamen Trendfilter darstellen.

2. **Trendauswertung**
   - Die langsame SMMA-Steigung muss innerhalb des `MinSlopePips`…`MaxSlopePips`-Korridors liegen (gemessen in Instrumenten-Pips).
   - ADX muss `AdxThreshold` überschreiten und im Vergleich zur vorherigen Kerze steigen.
   - Der Preis muss mindestens `TrendSpacePips` vom schnellen SMMA entfernt bleiben, um eine Überlastung zu vermeiden.
   - Eine bullische Tendenz erfordert den schnellen SMMA über dem langsamen SMMA, +DI ≥ -DI und einen steigenden schnellen Durchschnitt. Die bärische Tendenz spiegelt dies wider
Schecks.

3. **Primäre Einträge**
   - Wenn die bullische (oder bärische) Tendenz aktiv ist, eröffnet die Strategie eine Long- (oder Short-)Order mit einem Volumen von `OrderVolume`
`MaxNetPositions` und mindestens `SleepInterval` Wartezeit zwischen den Einträgen.
   - Wenn eine entgegengesetzte Nettoposition vorhanden ist, wird diese zunächst abgeflacht, um die Absicherung deaktiviert zu halten.

4. **Spike-Einträge**
   - Wenn die aktuelle Kerzenspanne das `CandleSpikeMultiplier`-fache der vorherigen Spanne überschreitet, kann die Strategie eine Hilfskerze auslösen
Position in Richtung des Kerzenkörpers, wenn die ADX-Komponenten übereinstimmen. Die Position verwendet `OrderVolume * SpikeVolumeMultiplier`.

## Risikomanagement

- ATR-basierter Stop-Loss, Take-Profit und optionaler Trailing-Stop (`StopLossAtrMultiplier`, `TakeProfitAtrMultiplier`, `TrailingAtrMultiplier`).
- Sitzungsweiter Schutz: Sobald der realisierte PnL `DailyProfitTarget` (Bruchteil des Startkapitals) erreicht, werden neue Einträge blockiert.
- Der globale Notschalter `EmergencyExit` schließt beim Umschalten sofort alle Positionen.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Für alle Berechnungen verwendeter Zeitrahmen. | 5-Minuten-Kerzen |
| `OrderVolume` | Basisvolumen für jeden Eintrag. | 0,03 |
| `AtrPeriod` | ATR Lookback-Länge. | 14 |
| `AdxPeriod` | ADX Lookback-Länge. | 14 |
| `AdxThreshold` | Mindestwert von ADX, um den Handel zu ermöglichen. | 10 |
| `FastMaPeriod` | Schnelle geglättete gleitende Durchschnittsperiode. | 60 |
| `SlowMaPeriod` | Langsam geglätteter Zeitraum des gleitenden Durchschnitts. | 120 |
| `MinSlopePips` / `MaxSlopePips` | Zulässiger Steigungskorridor für den langsamen SMMA. | 2 / 9 |
| `TrendSpacePips` | Minimaler Preisabstand zum schnellen SMMA (in Pips). | 5 |
| `CandleSpikeMultiplier` | Wie viel größer muss die Kerzenspanne sein, um Spitzeneinträge auszulösen? | 7 |
| `TakeProfitAtrMultiplier` | ATR Vielfache für Take-Profit. | 1,0 |
| `StopLossAtrMultiplier` | ATR Vielfache für Stop-Loss. | 1.5 |
| `TrailingAtrMultiplier` | ATR Vielfache für Trailing-Stop (0 deaktiviert). | 0 |
| `MaxNetPositions` | Maximale Anzahl gleichzeitiger Nettopositionseinheiten. | 1 |
| `SleepInterval` | Mindestzeit zwischen aufeinanderfolgenden Einträgen. | 24 Minuten |
| `DailyProfitTarget` | Bruchteil des anfänglichen Eigenkapitals, der den Handel blockiert, sobald er erreicht ist. | 0,045 |
| `AllowNewEntries` | Hauptschalter zum Aktivieren/Deaktivieren von Einträgen. | wahr |
| `SpikeVolumeMultiplier` | Volumenmultiplikator für Spitzeneinträge. | 1,0 |
| `EmergencyExit` | Schließt alle Positionen sofort, wenn wahr. | falsch |

## Notizen

- Der StockSharp-Port konzentriert sich auf die saubere, hohe Ebene API anstelle der Ticket-für-Ticket-Mikroverwaltung von MetaTrader. Alle
Die Geldverwaltungslogik wird über `Volume`- und ATR-basierte Ebenen implementiert.
- Das Original EA hatte mehrere Saldo- und Margenprüfungen. Diese werden mit `DailyProfitTarget`, `MaxNetPositions` angenähert.
und ATR Größenparameter, damit das Verhalten ohne direkte MT4-Kontoaufrufe angepasst bleibt.
- Da die Strategie geglättete Durchschnittswerte verwendet, stellen Sie sicher, dass eine ausreichende Aufwärmphase vorhanden ist, bevor Sie Trades bewerten.
