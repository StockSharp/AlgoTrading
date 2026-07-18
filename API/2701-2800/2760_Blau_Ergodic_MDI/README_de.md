# Blau Ergodic MDI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Blau Ergodic Market Directional Indicator (MDI)-Strategie reproduziert das Verhalten des MetaTrader-Expertenberaters `Exp_BlauErgodicMDI`. Der Algorithmus arbeitet auf einem höheren Zeitrahmen-Kerzen-Stream (Standard 4H) und wendet eine dreifache Glättungspipeline auf den ausgewählten Preiseingang an, um ein Momentum-Histogramm und eine Signallinie zu erstellen. Handelsentscheidungen werden aus diesem Histogramm mithilfe eines von drei konfigurierbaren Einstiegsmodi abgeleitet:

1. **Breakdown** – handelt, wenn das Histogramm die Nulllinie kreuzt.
2. **Twist** – reagiert auf Umkehrungen in der Histogrammsteigung (Momentum ändert die Richtung).
3. **CloudTwist** – reagiert auf Histogramm/Signallinie-Kreuzungen.

Jedes Signal kann optional entgegengesetzte Positionen schließen und/oder neue Trades eröffnen, abhängig von den vom Benutzer bereitgestellten Berechtigungsflags.

## Indikatorlogik
1. Den ausgewählten angewandten Preis mit dem konfigurierten Moving-Average-Typ und `PrimaryLength` glätten, um den Basispreis zu erhalten.
2. Die Momentum-Differenz `(price - baseline) / point_value` berechnen.
3. Dieses Momentum mit `FirstSmoothingLength` und `SecondSmoothingLength` glätten, um das Histogramm zu erstellen.
4. Das Histogramm noch einmal mit `SignalLength` glätten, um die Signallinie zu erhalten.
5. Historische Werte gemäß `SignalBarShift` puffern, damit Signale auf geschlossenen Kerzen bestätigt werden können.

Unterstützte Glättungsfamilien sind **EMA**, **SMA**, **SMMA/RMA** und **WMA**. Die Auswahl des angewandten Preises spiegelt die MetaTrader-Implementierung wider (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet, einfach, Viertel, trendfolgenden Varianten).

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `Volume` | Auftragsgröße beim Eröffnen von Positionen. |
| `StopLossPoints` | Stop-Loss-Abstand in Instrument-Punkten (0 deaktiviert). |
| `TakeProfitPoints` | Take-Profit-Abstand in Instrument-Punkten (0 deaktiviert). |
| `SlippagePoints` | Maximaler Preis-Slippage in Punkten bei Marktaufträgen. |
| `AllowLongEntries` / `AllowShortEntries` | Erlaubt das Eröffnen von Positionen in der jeweiligen Richtung. |
| `AllowLongExits` / `AllowShortExits` | Erlaubt das Schließen bestehender Positionen bei entgegengesetzten Signalen. |
| `Mode` | Einstiegsmodus (Breakdown / Twist / CloudTwist). |
| `CandleType` | Zeitrahmen der für Berechnungen verwendeten Kerzen (Standard 4H). |
| `SmoothingMethods` | Moving-Average-Familie, die in allen Glättungsschritten verwendet wird. |
| `PrimaryLength` | Basis-Glättungslänge für den angewandten Preis. |
| `FirstSmoothingLength` | Erste Glättungslänge für das Momentum. |
| `SecondSmoothingLength` | Zweite Glättungslänge zur Bildung des Histogramms. |
| `SignalLength` | Glättungslänge des Histogramms zur Erstellung der Signallinie. |
| `AppliedPrices` | Preisquelle für Indikatorberechnungen. |
| `SignalBarShift` | Anzahl der geschlossenen Bars, die bei der Signalauswertung zurückgeblickt werden. |
| `Phase` | Reservierter Parameter für Kompatibilität (in der aktuellen Implementierung nicht verwendet). |

## Signalbedingungen
* **Breakdown**
  * Long: Histogramm bei `SignalBarShift` ist positiv, während die vorherige Bar es nicht ist.
  * Short: Histogramm bei `SignalBarShift` ist negativ, während die vorherige Bar es nicht ist.
* **Twist**
  * Long: Histogramm bei `SignalBarShift` steigt nach einer fallenden Periode (vorherige < neueste und zwei Bars zurück > vorherige).
  * Short: Histogramm bei `SignalBarShift` fällt nach einer steigenden Periode (vorherige > neueste und zwei Bars zurück < vorherige).
* **CloudTwist**
  * Long: Histogramm kreuzt oberhalb der Signallinie (neuestes Histogramm > neueste Signale, vorheriges Histogramm <= vorherige Signale).
  * Short: Histogramm kreuzt unterhalb der Signallinie.

Jedes Signal kann sowohl das entgegengesetzte Exposure abflachen (wenn Ausstiege erlaubt sind) als auch einen neuen Trade mit dem konfigurierten Volumen eröffnen.

## Risikomanagement
`StartProtection` wird mit den angegebenen Stop-Loss- und Take-Profit-Abständen initialisiert (von Punkten in Preiseinheiten umgerechnet unter Verwendung der Tick-Größe des Instruments). Wenn einer der Abstände null ist, wird der jeweilige Schutz weggelassen. Slippage wird ebenfalls mithilfe derselben Tick-Größe in Preiseinheiten umgerechnet.

## Hinweise
* Signale werden nur auf abgeschlossenen Kerzen verarbeitet, um das ursprüngliche MetaTrader-Verhalten zu spiegeln.
* `SignalBarShift` ermöglicht das Verzögern der Handelsbestätigung, um nicht auf der aktuellsten Bar zu handeln.
* Der `Phase`-Parameter wird für die Vollständigkeit beibehalten, hat aber keinen Effekt bei den unterstützten Glättungsmethoden.
* Alle Code-Kommentare sind auf Englisch bereitgestellt, um die zukünftige Wartung zu vereinfachen.
