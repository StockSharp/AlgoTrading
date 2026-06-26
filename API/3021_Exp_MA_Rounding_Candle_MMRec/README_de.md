# Exp MA Rounding Candle MMRec Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Exp MA Rounding Candle MMRec Strategie** ist der StockSharp-Port des MQL5-Expertenberaters `Exp_MA_Rounding_Candle_MMRec`. Das Originalsystem basiert auf einem benutzerdefinierten „MA Rounding Candle"-Indikator, der jede Marktkerze in eine geglättete synthetische Kerze umwandelt und deren Farbwechsel verfolgt. Die C#-Version reproduziert dasselbe Verhalten, indem sie die Indikatorlogik on-the-fly neu aufbaut und auf den resultierenden Farbstrom reagiert.

## Aufbau des MA Rounding Candle
1. Jede eingehende Kerze wird durch vier identische gleitende Durchschnitte (Open, High, Low, Close) verarbeitet. Die unterstützten Glättungstypen sind **Simple**, **Exponential**, **Smoothed (RMA/SMMA)** und **Weighted**.
2. Die rohe gleitende Durchschnittsausgabe wird durch den ursprünglichen „Rounding"-Filter geleitet. Der Filter akzeptiert einen neuen Wert nur, wenn er sich vom vorherigen Ausgabewert um mehr als `RoundingFactor * PriceStep` unterscheidet. Andernfalls wird der vorherige gerundete Wert beibehalten. Dies reproduziert das MQL5-Verhalten, bei dem das Signal bei kleinen Schwankungen flach bleibt.
3. Ein Gap-Filter verankert den gerundeten Open am vorherigen gerundeten Close, wenn die absolute Differenz zwischen dem realen Open und Close kleiner als `GapSize * PriceStep` ist. Dies verhindert, dass kleine Doji-Kerzen die Farbe der synthetischen Kerze ändern.
4. Nach dem Runden wird die Indikatorfarbe definiert als:
   * `2` – bullische synthetische Kerze (`open < close`)
   * `0` – bärische synthetische Kerze (`open > close`)
   * `1` – neutrale Kerze (`open == close`)

Die Strategie speichert nur die letzten wenigen Farbwerte (genug für den konfigurierten Look-back) und führt keine lange Historie, entsprechend dem Original-Experten.

## Signallogik
Signale werden auf abgeschlossenen Kerzen mit einem konfigurierbaren `SignalBar`-Offset ausgewertet:

* `SignalBar` gibt an, wie viele geschlossene Kerzen zurück als Trigger-Bar behandelt werden sollen (`0` = aktuelle geschlossene Bar, `1` = die zuletzt vollständig geschlossene Bar, etc.).
* Die Strategie prüft auch die Farbe der unmittelbar vorhergehenden Bar (`SignalBar + 1`).
* Ein **bullisch-zu-nicht-bullischer** Übergang (`color[SignalBar + 1] = 2` und `color[SignalBar] != 2`) erzeugt:
  * optionales Schließen bestehender Short-Positionen (`EnableShortExits`), und
  * optionales Öffnen einer neuen Long-Position (`EnableLongEntries`).
* Ein **bärisch-zu-nicht-bärischer** Übergang (`color[SignalBar + 1] = 0` und `color[SignalBar] != 0`) erzeugt:
  * optionales Schließen bestehender Long-Positionen (`EnableLongExits`), und
  * optionales Öffnen einer neuen Short-Position (`EnableShortEntries`).

Das Positionsmanagement folgt dem Original-EA: Ausstiege werden vor neuen Einstiegen ausgeführt, und beim Richtungswechsel fügt die Strategie den Absolutwert der bestehenden Position zum Basis-Handelsvolumen hinzu, sodass die Nettogröße mit der gewünschten Richtung übereinstimmt.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 1-Stunden-Zeitrahmen | Kerzenserie zur Steuerung der Strategie. |
| `SmoothingMethod` | `Simple` | Gleitender Durchschnittstyp für alle gerundeten Preisreihen. |
| `MaLength` | `12` | Anzahl der Perioden, die vom gewählten gleitenden Durchschnitt verwendet werden. |
| `RoundingFactor` | `50` | Multiplikator, der auf den `PriceStep` des Instruments angewendet wird, um den Rundungsschwellenwert zu erstellen. Größere Werte lassen die gerundete Reihe seltener ändern. |
| `GapSize` | `10` | Multiplikator, der auf den `PriceStep` für den Gap-Filter angewendet wird, der den gerundeten Open bei kleinen Kerzen am vorherigen gerundeten Close verankert. |
| `SignalBar` | `1` | Wie viele geschlossene Kerzen zurück für das Signal analysiert werden. |
| `TradeVolume` | `1` | Basis-Positionsvolumen für neue Einstiege. Der Parameter wird mit der eingebauten `Strategy.Volume`-Eigenschaft synchronisiert. |
| `EnableLongEntries` / `EnableShortEntries` | `true` | Schalter für Long/Short-Einstiege. |
| `EnableLongExits` / `EnableShortExits` | `true` | Schalter zum Schließen bestehender Positionen. |

## Implementierungshinweise
* Nur die in StockSharp verfügbaren Glättungsmodi werden bereitgestellt. Exotische MQL5-spezifische Glätter (JJMA, JurX, VIDYA, AMA, etc.) sind in diesem Port nicht vorhanden.
* Der komplexe Money-Management-Rekalkulierer des Original-EA wird durch einen einzigen `TradeVolume`-Parameter ersetzt. Dies hält die Strategie deterministisch und einfacher innerhalb von StockSharp zu optimieren.
* Alle preisbasierten Schwellenwerte (`RoundingFactor`, `GapSize`) werden in Preisschritten interpretiert, indem der Wert bei jeder Kerzenverarbeitung mit `Security.PriceStep` multipliziert wird.
* Die Strategie verwendet die High-Level-Kerzenabonnement-API (`SubscribeCandles`) und arbeitet strikt auf abgeschlossenen Kerzen, genau wie der MQL5-Experte, der auf `IsNewBar` wartet, bevor er Aufträge ausgibt.
* Long/Short-Schutz, Trailing Stops und andere Ausstiege wurden absichtlich weggelassen, da sie nicht Teil der ursprünglichen Implementierung waren.

## Verwendung
1. Hängen Sie die Strategie an das gewünschte Instrument an und weisen Sie über `CandleType` eine geeignete Kerzenserie zu (z. B. `TimeSpan.FromHours(1).TimeFrame()`).
2. Konfigurieren Sie die Glättungsmethode, die Länge des gleitenden Durchschnitts, den Rundungsfaktor und den Gap-Filter, um die Einstellungen des Original-EA oder Ihre eigenen Optimierungsergebnisse zu replizieren.
3. Setzen Sie `TradeVolume` auf die Lotgröße, die Sie handeln möchten. Die Strategie synchronisiert automatisch die interne `Volume`-Eigenschaft mit diesem Parameter.
4. Aktivieren oder deaktivieren Sie Long/Short-Einstiege und -Ausstiege je nach gewünschtem Verhalten.
5. Starten Sie die Strategie. Trades werden generiert, wenn die MA Rounding Candle-Farbe die konfigurierten Übergänge durchführt.

Die README spiegelt die in `CS/ExpMaRoundingCandleMmrecStrategy.cs` enthaltene C#-Implementierung wider und sollte als Referenzdokumentation für diesen Port verwendet werden.
