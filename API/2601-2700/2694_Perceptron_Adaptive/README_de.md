# Adaptive Perceptron-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist ein StockSharp-Port des MetaTrader 5-Expertenberaters *Perceptron.mq5*.  
Fünf diskrete Indikatorsignale werden durch ein zweischichtiges Perzeptron kombiniert. Jeder Trade zeichnet den Indikatorstatus auf und sobald die Position geschlossen wird, werden synaptische Gewichte je nach erzieltem Gewinn verstärkt oder bestraft. Das Verhalten ahmt die Selbstlernschleife des ursprünglichen EA nach und nutzt dabei die StockSharp-High-Level-Kerzen-API.

## Indikatorschicht

| Code | Beschreibung | Signallogik |
| --- | --- | --- |
| `IND1` | Schneller/langsamer einfacher gleitender Durchschnitt Crossover | +1 wenn die schnelle MA auf dem vorherigen Bar über die langsame MA kreuzt, −1 bei einem Abwärtskreuz, sonst 0. |
| `IND2` | Relative Strength Index (RSI) | +1 wenn RSI die überverkaufte Zone verlässt (kreuzt über 30), −1 wenn RSI die überkaufte Zone verlässt (kreuzt unter 70). |
| `IND3` | Commodity Channel Index (CCI) | +1 bei einem Kreuz über −100, −1 bei einem Kreuz unter +100. |
| `IND4` | Steigung des kurzen einfachen gleitenden Durchschnitts | +1 wenn die kurze MA zwischen den zwei vorherigen Bars gestiegen ist, −1 wenn sie gesunken ist. |
| `IND5` | Awesome Oscillator Momentum-Farbe | +1 wenn das Histogramm im Vergleich zum vorherigen Wert zunimmt (bullishe Farbe), −1 wenn es abnimmt. |

Alle Indikatoren werden auf abgeschlossenen Kerzen ausgewertet. Historische Puffer werden intern gepflegt, um das `CopyBuffer`-Windowing des MQL5-Skripts zu replizieren.

## Perzeptron-Architektur

- Fünf versteckte Neuronen (`NN1`…`NN5`) kombinieren jeweils vier Indikatoren und spiegeln die Verdrahtung im EA wider.
- Jedes Neuron hat sein eigenes Wörterbuch synaptischer Gewichte plus ein Bias-Gewicht (`NNS1`…`NNS5`).
- Die endgültige Aktivierung `brainReturn` ist die gewichtete Summe der Neuronenausgaben.  
  - `brainReturn > 0` → Long-Einstieg anfordern (wenn der vorherige Trade nicht auch Long war).  
  - `brainReturn < 0` → Short-Einstieg anfordern (wenn der vorherige Trade nicht auch Short war).
- Positionen werden nur mit Marktorders eröffnet, wenn keine Position aktiv ist.

## Positionsmanagement

- Einstiegspreis, Richtung und Indikator-/Neuronenstatus werden bei jeder Ausführung erfasst.
- Take-Profit- und Stop-Loss-Versätze werden in absoluten Preiseinheiten angewendet (z.B. 0.0004 für 4 Punkte bei einem Forex-Paar mit 5 Dezimalstellen).  
  Wenn eine neue Kerze nach dem Einstieg öffnet:
  - Bei Longs wird zuerst das Hoch mit dem Take-Profit-Preis verglichen, dann das Tief mit dem Stop-Loss.  
  - Bei Shorts wird zuerst das Tief mit dem Take-Profit-Preis verglichen, dann das Hoch mit dem Stop-Loss.  
  - Wenn beide Level innerhalb derselben Kerze überschritten werden, hat der Take-Profit Priorität, entsprechend dem optimistischen Verhalten des ursprünglichen EA.
- Sobald ein Ausstieg erkannt wird, schließt die Strategie die Position mit einer Marktorder und berechnet den realisierten Gewinn unter Verwendung des entsprechenden TP/SL-Levels.

## Adaptive Gewichtsaktualisierung

Wenn ein Trade schließt, werden die erfassten Indikator- und Neuronenzustände wiedergegeben:

1. `directionSign` (−1 für Longs, +1 für Shorts) und `outcomeSign` (Vorzeichen des realisierten PnL) werden bestimmt.
2. Bias-Gewichte werden innerhalb `[SinMin, SinMax]` angepasst:
   - Wenn `sign(neuronOutput) * directionSign` positiv ist, folgt das Bias dem Trade-Ergebnis (Erhöhung bei Gewinn, Reduzierung bei Verlust).
   - Andernfalls bewegt sich das Bias entgegen dem Ergebnis.
3. Synaptische Gewichte verhalten sich ähnlich, bleiben aber unbegrenzt: Mit der Positionsrichtung ausgerichtete Signale erhalten Verstärkung bei Gewinnen und Strafen bei Verlusten, während entgegengesetzte Signale das Inverse tun.
4. Gespeicherte Signale werden gelöscht, um versehentliche Wiederverwendung zu vermeiden.

Dies verallgemeinert die 1.500+ Zeilen bedingter Synapsenverwaltung aus dem EA in eine kompakte Verstärkungsroutine.

## Parameter

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 1-Minuten-Zeitrahmen | Kerzenabonnement der Strategie. |
| `FastMaLength` | 5 | Periode der schnellen SMA im Crossover-Signal. |
| `SlowMaLength` | 9 | Periode der langsamen SMA. |
| `RsiLength` | 14 | RSI-Berechnungsperiode. |
| `CciLength` | 14 | CCI-Berechnungsperiode. |
| `SlopeMaLength` | 5 | Periode der MA für die Steilheitserkennung. |
| `AoShortLength` | 5 | Kurze Periode des Awesome Oscillators. |
| `AoLongLength` | 34 | Lange Periode des Awesome Oscillators. |
| `StopLossOffset` | 0.001 | Stop-Loss-Abstand in absoluten Preiseinheiten (0 deaktiviert den Stop). |
| `TakeProfitOffset` | 0.0004 | Take-Profit-Abstand in absoluten Preiseinheiten (0 deaktiviert das Ziel). |
| `SinMax` | 5 | Obergrenze für Neuron-Bias-Gewichte. |
| `SinMin` | 0 | Untergrenze für Neuron-Bias-Gewichte. |
| `SinPlusStep` | 0.03 | Positiver Verstärkungsinkrement. |
| `SinMinusStep` | 0.03 | Negativer Verstärkungsdekrement. |

Alle numerischen Parameter sind als `StrategyParam<T>` exponiert und können im StockSharp Designer optimiert werden.

## Implementierungshinweise

- Verwendet die High-Level-Kerzenabonnement-API mit Multi-Indikator-Bindung.
- Manuelle Trade-Verwaltung wird eingesetzt, damit realisierte Preise beim Aktualisieren von Synapsen bekannt sind.
- Indikatoren werden mit nullbaren Feldern gespeichert, damit Signale erst nach vollständiger Ausbildung ausgelöst werden.
- Der Farb-Puffer des Awesome Oscillators im EA wird durch den Vergleich aktueller und vorheriger Histogrammwerte approximiert.
- Die Chart-Ausgabe zeichnet die Kerzenserie plus die schnellen und langsamen gleitenden Durchschnitte. Trade-Marker zeigen das adaptive Verhalten in Echtzeit.

## Einschränkungen und Annahmen

- Stops und Ziele werden einmal pro abgeschlossener Kerze ausgewertet; die Intrabar-Reihenfolge der Ereignisse ist unbekannt, daher wird dem Gewinnziel Priorität eingeräumt, wenn beide Schwellen getroffen werden.
- Indikatorgewichte sind wie im ursprünglichen EA unbegrenzt und können während verlängerter Verstärkungszyklen stark anwachsen.
- Der `LastTradeType` des ursprünglichen EA wurde nie zurückgesetzt; in diesem Port wird er nach jedem Ausstieg geleert, sodass aufeinanderfolgende Trades in dieselbe Richtung möglich bleiben.
