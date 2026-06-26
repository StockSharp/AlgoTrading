# Exp Highs Lows Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Exp Highs Lows Signal ist ein direkter Port des MetaTrader 5-Expertenberaters `Exp_HighsLowsSignal`. Die Strategie stützt sich auf einen Musterdetektor, der nach einer konfigurierbaren Anzahl aufeinanderfolgender Kerzen sucht, die höhere Hochs und höhere Tiefs drucken (bullische Sequenz) oder niedrigere Hochs und niedrigere Tiefs (bärische Sequenz). Sobald eine Sequenz gefunden wird, verzögert die Strategie die Reaktion um die konfigurierte Anzahl geschlossener Balken, schließt jede entgegengesetzte Exposition und öffnet optional eine Position in der erkannten Richtung. Schutz-Stops werden in Preisschritten ausgedrückt, um das punktbasierte Money-Management des ursprünglichen Robots nachzubilden.

## Strategielogik
### Highs/Lows-Sequenzdetektor
* Der Detektor bewertet jede abgeschlossene Kerze auf dem gewählten Zeitrahmen.
* Ein **bullisches Signal** erfordert `SequenceLength` aufeinanderfolgende Vergleiche, bei denen sowohl das aktuelle Hoch als auch das aktuelle Tief strikt größer als die vorherige Kerze sind.
* Ein **bärisches Signal** erfordert `SequenceLength` aufeinanderfolgende Vergleiche, bei denen sowohl das aktuelle Hoch als auch das aktuelle Tief strikt kleiner als die vorherige Kerze sind.
* Signale werden eingereiht und nach `SignalBarDelay` geschlossenen Kerzen freigegeben, was der `SignalBar`-Einstellung der MQL-Implementierung entspricht.

### Einstiegsregeln
* **Long-Eintritte**
  * Ausgelöst, wenn eine bullische Sequenz aktiv wird und `AllowLongEntry` aktiviert ist.
  * Jede bestehende Short-Position wird zuerst geschlossen (wenn `AllowShortExit` wahr ist), dann wird eine Markt-Kauforder mit Volumen `OrderVolume + |Position|` gesendet, um Shorts zu decken und die gewünschte Long-Größe zu etablieren.
* **Short-Eintritte**
  * Ausgelöst, wenn eine bärische Sequenz aktiv wird und `AllowShortEntry` aktiviert ist.
  * Jede bestehende Long-Position wird zuerst geschlossen (wenn `AllowLongExit` wahr ist), dann wird eine Markt-Verkaufsorder mit Volumen `OrderVolume + |Position|` gesendet, um Longs zu decken und die gewünschte Short-Größe zu etablieren.

### Ausstiegsregeln
* Eine bullische Sequenz fordert immer `AllowShortExit` an, um offene Shorts zu schließen.
* Eine bärische Sequenz fordert immer `AllowLongExit` an, um offene Longs zu schließen.
* Wenn das relevante Flag deaktiviert ist, bleibt die entgegengesetzte Exposition unberührt, sodass der Benutzer nur in einer Richtung handeln oder den Detektor im „Nur-Alerts"-Modus betreiben kann.

### Risikomanagement
* `StopLossTicks` und `TakeProfitTicks` repräsentieren Abstände in Preisschritten (Punkten). Ein Wert von `0` deaktiviert die entsprechende Schutzorder und reproduziert das Verhalten des ursprünglichen EA.
* `StartProtection` konvertiert diese Abstände in absolute Preisoffsets, sodass alle Markteintritte automatisch passende Stop-Loss- und Take-Profit-Orders erhalten.

## Parameter
* **OrderVolume** – Basis-Ordervolumen bei einer neuen Trade-Eröffnung.
* **AllowLongEntry / AllowShortEntry** – Schalter, die Long- oder Short-Eintritte bei ihren jeweiligen Signalen aktivieren.
* **AllowLongExit / AllowShortExit** – Schalter, die der Strategie erlauben, entgegengesetzte Positionen zu schließen, wenn das Gegentrendssignal erscheint.
* **StopLossTicks / TakeProfitTicks** – Schutzabstände in Preisschritten; auf `0` setzen zum Deaktivieren.
* **SequenceLength** – Anzahl aufeinanderfolgender Vergleiche zur Qualifizierung einer bullischen oder bärischen Sequenz (entspricht `HowManyCandles` in MT5).
* **SignalBarDelay** – Anzahl geschlossener Kerzen zum Warten vor dem Handeln auf ein Signal (entspricht dem `SignalBar`-Input).
* **CandleType** – Zeitrahmen für den Highs/Lows-Detektor (Standard: 4-Stunden-Kerzen).

## Zusätzliche Hinweise
* Die Strategie speichert nur die minimale Menge an Kerzengeschichte, die für den Detektor benötigt wird, und hält das Verhalten identisch zum benutzerdefinierten MT5-Indikator.
* Da alle Order-Verwaltung über `StartProtection` erfolgt, erhalten Backtests und Live-Trading automatisch passende Stop- und Take-Profit-Orders ohne zusätzlichen Code.
* Deaktivieren Sie die entsprechenden `Allow`-Flags, um die Strategie in einen Richtungsfilter oder ein reines Signalisierungswerkzeug umzuwandeln.
* Es ist keine Python-Übersetzung vorhanden; nur die C#-Version ist in diesem Paket verfügbar.
