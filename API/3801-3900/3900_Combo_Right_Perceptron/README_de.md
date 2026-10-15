# Combo Right Perceptron-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine originalgetreue StockSharp-Portierung des MetaTrader-Expertenberaters **Combo_Right.mq4**. Es kombiniert einen Basis-Momentumfilter des Commodity Channel Index (CCI) mit drei Perzeptronen, die das Eröffnungspreismomentum über konfigurierbare Balkenschritte analysieren. Abhängig vom `PassMode` können die Perzeptrone das CCI-Signal überschreiben und den Vorgesetzten anweisen, Long- oder Short-Positionen mit ihren speziellen Risikoparametern zu eröffnen.

## Handelslogik

1. Abonnieren Sie den konfigurierten Kerzentyp und berechnen Sie den CCI für Eröffnungspreise. Die letzte abgeschlossene Kerze liefert sowohl den Schlusskurs als auch die historischen Eröffnungswerte für Perceptron-Eingaben.
2. Pflegen Sie einen zirkulären Puffer der Eröffnungspreise, damit die Perzeptrone auf die Eröffnungen von `period`, `2*period`, `3*period` und `4*period` Bars zugreifen können, ohne auf Indikator-History-Getter angewiesen zu sein.
3. Wenn eine fertige Kerze ankommt:
   - Werten Sie den Wert CCI aus. Dies fungiert als Standardsignal (`> 0` = lang, `< 0` = kurz) mit den Basisschutzabständen (`TakeProfit1` / `StopLoss1`).
   - Berechnen Sie je nach `PassMode` ein oder mehrere Perzeptrone. Jedes Perzeptron verwendet Gewichte, die aus den ursprünglichen MQL-Eingaben (`X** - 100`) und den Unterschieden zwischen dem letzten Schlusskurs und den historischen Eröffnungskursen abgeleitet wurden.
   - Wenn eine Perceptron-Bedingung erfüllt ist, überschreibt es das Standardsignal und weist seine eigenen Stop-Loss-/Take-Profit-Abstände zu, bevor eine Order gesendet wird.
4. Stornieren Sie Arbeitsaufträge, glätten Sie die gegenüberliegende Belichtung und eröffnen Sie die neue Position mit dem konfigurierten `TradeVolume`. Nachdem die Marktorder gesendet wurde, rufen Sie `SetTakeProfit` und `SetStopLoss` mit den berechneten Offsets auf, damit die Schutzorder den aktiven Perzeptronzweig widerspiegeln.

### Pass-Modi

- **Durchlauf 1** – nur der Wert CCI wird berücksichtigt. Das Signal ist proportional zum letzten Indikatorwert.
- **Pass 2** – wenn das erste Perzeptron (`Perceptron1Period`, `X12…X42`) einen negativen Output erzeugt, eröffnet die Strategie sofort einen Short-Trade mit dem zweiten Risikoprofil. Andernfalls wird auf das Ergebnis CCI zurückgegriffen.
- **Pass 3** – wenn das zweite Perzeptron positiv ist, eröffnet die Strategie einen Long-Trade mit dem dritten Risikoprofil. Andernfalls ist die Ausgabe von CCI erforderlich.
- **Durchgang 4** – Überprüfen Sie zunächst das dritte Perzeptron. Ein positiver Wert erfordert, dass auch das zweite Perzeptron positiv ist, um einen Long-Einstieg mit bullischem Risikoprofil zu ermöglichen. Wenn das dritte Perzeptron negativ ist und das erste Perzeptron unter Null liegt, eröffnet die Aufsichtsbehörde einen Short mit dem bärischen Risikoprofil. Wenn keiner der Zweige ausgelöst wird, wird die Ausgabe CCI verwendet.

In allen Modi ignoriert die Strategie Signale, bis genügend Kerzen gesammelt sind, um den tiefsten Perzeptronschritt zu unterstützen.

## Risikomanagement

Jeder Eintrag berechnet neue Preisversätze basierend auf dem Symbol `PriceStep`. Wenn das Instrument keinen Schritt bereitstellt, wird der rohe Punktabstand unverändert verwendet. `SetTakeProfit` und `SetStopLoss` erhalten die gewünschten Offsets zusammen mit der resultierenden Nettoposition, sodass die Schutzklammern mit der aktuellen Belichtung synchron bleiben.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `TakeProfit1`, `StopLoss1` | `decimal` | 50/50 | Gewinn- und Verlustdistanzen (in Punkten), wenn die Ausgabe CCI verwendet wird. |
| `CciPeriod` | `int` | 10 | Zeitraum des CCI, berechnet auf Basis der Eröffnungspreise. |
| `X12`, `X22`, `X32`, `X42` | `int` | 100 | Rohgewichte für das bärische Perzeptron; Die Strategie subtrahiert intern 100 wie im Originalcode. |
| `TakeProfit2`, `StopLoss2` | `decimal` | 50/50 | Risikoabstände (Punkte), die angewendet werden, wenn das bärische Perzeptron ausgelöst wird. |
| `Perceptron1Period` | `int` | 20 | Schrittweite zwischen den Samples für das bärische Perzeptron (in Balken). |
| `X13`, `X23`, `X33`, `X43` | `int` | 100 | Rohgewichte für das bullische Perzeptron. |
| `TakeProfit3`, `StopLoss3` | `decimal` | 50/50 | Risikoabstände (Punkte), die angewendet werden, wenn das bullische Perzeptron auslöst. |
| `Perceptron2Period` | `int` | 20 | Schrittweite zwischen den Proben für das bullische Perzeptron (in Balken). |
| `X14`, `X24`, `X34`, `X44` | `int` | 100 | Rohgewichte für das in `PassMode = 4` verwendete Bestätigungsperzeptron. |
| `Perceptron3Period` | `int` | 20 | Schrittweite zwischen den Proben für das Bestätigungsperzeptron (in Balken). |
| `PassMode` | `int` | 1 | Supervisor-Modus (1–4), der die Verzweigungslogik des MQL-Experten reproduziert. |
| `TradeVolume` | `decimal` | 0,01 | Für neue Markteintritte verwendetes Volumen. Die gegenüberliegende Ausstellung wird vor dem Betreten geschlossen. |
| `CandleType` | `DataType` | M1 | Kerzenserie, die die CCI- und Perzeptron-Eingänge speist. |

## Notizen

- Die Implementierung wartet vor dem Handel absichtlich, bis alle Perzeptrone über genügend historische Öffnungspreise verfügen, um Array-gebundene Probleme zu vermeiden, die in MetaTrader implizit waren.
- Indikatorwerte werden niemals durch wahlfreien Zugriff abgerufen. Stattdessen wird die benötigte Historie konform zu den Projektrichtlinien in einem Ringspeicher abgelegt.
- Alle Kommentare und Dokumentationen werden auf Englisch gehalten, um den Repository-Anforderungen zu entsprechen.
