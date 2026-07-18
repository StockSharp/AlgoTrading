# Exp Color TSI Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertierung des MetaTrader 5 Expertenberaters **Exp_ColorTSI-Oscillator** in das StockSharp-Framework.
- Rekonstruiert den ColorTSI-Oszillator: einen doppelt geglätteten True Strength Index mit einer verzögerten Trigger-Linie und mehreren Glättungsalgorithmen aus `SmoothAlgorithms.mqh`.
- Generiert Trades, wenn der Oszillator gegenüber seinem verzögerten Trigger auf- oder abwärts dreht, was den "Swing-Umkehr"-Stil des ursprünglichen EA repliziert.

## Indikatorrekonstruktion
- Der angewendete Preis wird über die Option `ColorTsiAppliedPrice` ausgewählt (Close, Open, Median, Typical, Weighted, Demark usw.).
- Preis-Momentum (`diff = price[n] - price[n-1]`) und sein Absolutwert werden in zwei Stufen geglättet:
  1. **Erste Stufe**: konfigurierbarer `ColorTsiSmoothingMethod` (`Sma`, `Ema`, `Smma`, `Lwma`, `Jjma`, `Jurx`, `Parma`, `T3`, `Vidya`, `Ama`) mit Länge `FirstLength` und Phase `FirstPhase` für Jurik-ähnliche Filter.
  2. **Zweite Stufe**: identische Methodenoptionen mit `SecondLength`/`SecondPhase`, angewendet auf die bereits geglättete Momentum-Reihe.
- Die Oszillator-Ausgabe ist `TSI = 100 * smoothMomentum / smoothAbsMomentum`. Wenn der Nenner null ist, wird der Wert ignoriert.
- Eine Trigger-Linie wird durch Verzögerung des TSI um `TriggerShift` Balken erhalten, was die MetaTrader-Buffer-Logik widerspiegelt.
- Historische Werte werden gespeichert, damit `SignalBar` dem MetaTrader-`CopyBuffer`-Zugriffsmuster entspricht (Index `SignalBar` = zuletzt geschlossener Balken, `SignalBar + 1` = vorheriger Balken, usw.).

## Handelsregeln
- Berechnungen laufen auf abgeschlossenen Kerzen, die von `CandleType` geliefert werden (Standard: 4-Stunden-Zeitrahmen).
- Sei `TSI[k]` der Oszillatorwert und `Trigger[k]` die verzögerte Reihe.
- **Bullischer Kontext**: `TSI[SignalBar + 1] > Trigger[SignalBar + 1]` ⇒ der vorherige Balken zeigte aufwärtsgerichtetes Momentum.
  - Shorts schließen, wenn `EnableShortExits` true ist.
  - Eine Long-Position eröffnen, wenn `EnableLongEntries` true ist **und** `TSI[SignalBar] ≤ Trigger[SignalBar]`, was eine Aufwärtsschwankung nach dem Rücksetzer signalisiert.
- **Bärischer Kontext**: `TSI[SignalBar + 1] < Trigger[SignalBar + 1]` ⇒ der vorherige Balken zeigte abwärtsgerichtetes Momentum.
  - Longs schließen, wenn `EnableLongExits` true ist.
  - Eine Short-Position eröffnen, wenn `EnableShortEntries` true ist **und** `TSI[SignalBar] ≥ Trigger[SignalBar]`.
- Einstiegssignale werden durch die Zeit des analysierten Balkens plus einem vollen Zeitrahmen kodiert; jedes Signal kann dank `_lastLongEntryTime` / `_lastShortEntryTime`-Wächtern höchstens einen Trade auslösen.
- Alle Aktionen werden mit Market-Orders ausgeführt. Bestehende entgegengerichtete Positionen werden vor Umkehrungen geschlossen.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Datenstrom für die Analyse. Unterstützt jeden `DataType` (Zeit-, Tick-, Volumen-Kerzen). | H4-Zeitrahmen |
| `Volume` | Feste Ordergröße, die die Geldverwaltungsblöcke des EA ersetzt. Muss > 0 sein. | 0.1 |
| `FirstMethod`, `FirstLength`, `FirstPhase` | Erste Glättungsstufe für Momentum und absolutes Momentum. | SMA, 12, 15 |
| `SecondMethod`, `SecondLength`, `SecondPhase` | Zweite Glättungsstufe. | SMA, 12, 15 |
| `PriceMode` | Angewendete Preisoption für den Oszillator. | Close |
| `SignalBar` | Balkenversatz zur Signalauswertung (1 = letzter geschlossener Balken). | 1 |
| `TriggerShift` | Verzögerung der Trigger-Linie (1 reproduziert den ursprünglichen Indikator). | 1 |
| `EnableLongEntries` / `EnableShortEntries` | Erlaubt das Eröffnen von Long-/Short-Trades. | true |
| `EnableLongExits` / `EnableShortExits` | Erlaubt das Schließen von Positionen bei entgegengesetztem Kontext. | true |
| `StopLossPoints` | Stop-Loss-Abstand in Preispunkten (umgerechnet mit dem Instrument-`PriceStep`). | 1000 |
| `TakeProfitPoints` | Take-Profit-Abstand in Preispunkten. | 2000 |

## Risikomanagement
- Der ursprüngliche EA verwendete Hilfsfunktionen aus `TradeAlgorithms.mqh` für die SL/TP-Platzierung. Die C#-Version ruft `StartProtection` mit den ausgewählten Abständen auf, die in `UnitTypes.Point` konvertiert wurden.
- Wenn ein Abstand auf 0 gesetzt wird, wird die entsprechende Schutzorder ausgelassen.
- Keine Trailing-Stops oder Positionsskalierung sind implementiert; diese entsprechen dem MetaTrader-Verhalten für diesen Experten.

## Unterschiede zur MetaTrader-Version
- Marginsbasiertes Lot-Sizing (`MM` und `MMMode`) wird durch einen festen `Volume`-Parameter ersetzt. Dies hält das Verhalten broker-übergreifend deterministisch und vermeidet die Replikation kontospezifischer Hebellogik.
- Slippage (`Deviation_`) wird nicht emuliert, da StockSharp-Market-Orders keinen Slippage-Parameter exponieren.
- Indikatorglättung wird vollständig mit StockSharp-Indikatoren rekonstruiert (einschließlich Jurik-Phasenbehandlung durch Reflection), sodass Signalwerte mit den ursprünglichen Buffern übereinstimmen.
- Python-Implementierung wird absichtlich weggelassen, wie angefordert.

## Verwendungshinweise
- Sicherstellen, dass das ausgewählte Instrument den von `CandleType` angeforderten Kerzentyp bereitstellt. Für Standard-Zeitrahmen `TimeSpan.FromHours(x).TimeFrame()` verwenden.
- `SignalBar` muss ≥ `TriggerShift` sein, um gültige Trigger-Werte zu erhalten; andernfalls werden Signale übersprungen, bis genügend Historie angesammelt ist.
- Da die Strategie auf abgeschlossenen Kerzen reagiert, Echtzeit-Order-Registrierung nur nach `IsFormedAndOnlineAndAllowTrading()` aktivieren.
- Der Chart-Bereich visualisiert Preiskerzen und ausgeführte Trades; Indikatoren werden intern rekonstruiert und nicht automatisch geplottet.
- Zum Reproduzieren der MetaTrader-Standards: alle Glättungseinstellungen auf SMA mit Länge 12 belassen, beide Einstiegs- und Ausstiegs-Toggles aktiviert lassen und Standard-Stop/Take-Abstände verwenden.
