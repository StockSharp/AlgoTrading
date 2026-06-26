# Sprut Ausstehende-Order-Raster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Sprut Ausstehende-Order-Raster-Strategie** reproduziert den MetaTrader 5-Experten-Advisor *Sprut (barabashkakvns Ausgabe)* innerhalb von StockSharps High-Level-Strategie-Framework. Sie baut ein konfigurierbares Raster von Buy- und Sell-Ausstehend-Orders um den aktuellen Marktpreis auf und verwaltet die Lebenszeit jeder Order, die Volumenskalierung und den Post-Fill-Schutz mit StockSharps Hilfsmethoden (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`).

Die konvertierte Version behält die Philosophie des ursprünglichen Experten-Advisors bei:

* die allererste Order für jede aktivierte Richtung entweder bei einem manuellen Preis oder bei einem automatischen Versatz in Pips von der besten Kursnotierung platzieren;
* das Raster schrittweise mit unabhängigem Abstand für Stop- und Limit-Orders erweitern;
* Order-Volumina mit einem Multiplikator skalieren, der die MT5-Implementierung widerspiegelt;
* jede gefüllte Order mit eigenem Stop-Loss und Take-Profit ausstatten, ausgedrückt als Pip-Versätze vom Einstiegspreis;
* globale Gewinn- und Verlust-Checkpoints durchsetzen, die bei Erreichen sofort Positionen liquidieren und verbleibende ausstehende Orders entfernen;
* ausstehende Orders optional nach einer bestimmten Anzahl von Minuten verfallen lassen.

## Funktionsweise der Strategie
1. **Marktdaten** – Die Strategie abonniert Order-Book-Updates, um den besten Bid/Ask zu verfolgen, und Kerzen (Standard: 1 Minute) für periodische Wartung. Keine Indikatoren erforderlich.
2. **Rasterinitialisierung** – Wenn keine offene Position und keine aktive Raster-Order vorhanden ist, berechnet die Strategie den Anfangspreis für jeden der vier möglichen Order-Typen:
   * **Buy Stop**: bester Ask + `DeltaFirstBuyStop` (es sei denn, `FirstBuyStop` ist ungleich null).
   * **Buy Limit**: bester Bid − `DeltaFirstBuyLimit` (es sei denn, `FirstBuyLimit` ist ungleich null).
   * **Sell Stop**: bester Bid − `DeltaFirstSellStop` (es sei denn, `FirstSellStop` ist ungleich null).
   * **Sell Limit**: bester Ask + `DeltaFirstSellLimit` (es sei denn, `FirstSellLimit` ist ungleich null).
   Jeder Versatz wird von Pips mit dem `PriceStep` des Wertpapiers konvertiert (Fallback: 0.0001).
3. **Order-Stapeln** – Für jede aktivierte Richtung erstellt die Strategie `CountOrders` Einträge, die durch `StepStop` oder `StepLimit` getrennt sind (ebenfalls in Pips). Volumina folgen der ursprünglichen Formel: Order #0 verwendet das Basisvolumen, während Order #N `baseVolume * N * coefficient` verwendet, wenn der Koeffizient größer als 1 ist. Volumina werden angepasst, um `Security.VolumeStep`, `Security.MinVolume` und `Security.MaxVolume` zu respektieren.
4. **Ablauf** – Wenn `ExpirationMinutes` positiv ist, versieht die Strategie jede ausstehende Order mit einem Zeitstempel und storniert sie automatisch nach Ablauf der Frist.
5. **Schutz nach Füllung** – Wenn StockSharp meldet, dass eine Einstiegsorder abgeschlossen ist, registriert die Strategie die passenden Stop-Loss- und Take-Profit-Orders (`StopLoss` und `TakeProfit` in Pips). Eine Null-Distanz deaktiviert den jeweiligen Schutz.
6. **Gewinn-Checkpoint** – Realisiertes plus unrealisiertes PnL wird neu berechnet, wenn neue Daten eintreffen. Wenn `ProfitClose` positiv und erreicht ist, oder `LossClose` (typischerweise negativ) unterschritten wird, fordert die Strategie eine vollständige Liquidierung an: Marktschließung der Position, Stornierung aller Raster-Orders und Stornierung verbleibender Schutzorders. Der Handel wird automatisch fortgesetzt, nachdem alles flat ist.
7. **Kontinuierliche Wartung** – Jedes Update bereinigt fertige Orders, entfernt abgelaufene Elemente, versucht ein neues Raster zu platzieren, wenn die Bedingungen es erlauben, und vermeidet das Wiederbewaffnen während einer laufenden Liquidierung.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CountOrders` | Anzahl der Orders pro aktivierter Richtung. | 5 |
| `FirstBuyStop`, `FirstBuyLimit`, `FirstSellStop`, `FirstSellLimit` | Optionale absolute Preise für die erste Order in jeder Richtung (0 = automatischen Versatz verwenden). | 0 |
| `DeltaFirstBuyStop`, `DeltaFirstBuyLimit`, `DeltaFirstSellStop`, `DeltaFirstSellLimit` | Pip-Versätze, die beim besten Bid/Ask angewendet werden, wenn automatische Preisfindung verwendet wird. | 15 |
| `UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit` | Jede Rasterrichtung aktivieren oder deaktivieren. | false |
| `StepStop`, `StepLimit` | Abstand zwischen aufeinanderfolgenden Stop- oder Limit-Orders (Pips). | 50 |
| `VolumeStop`, `VolumeLimit` | Basisvolumen für die erste Stop-/Limit-Order. | 0.01 |
| `CoefficientStop`, `CoefficientLimit` | Multiplikator für zusätzliche Orders (>1 behält das MT5-Skalierungsverhalten bei). | 1.6 |
| `ProfitClose` | Gesamt-PnL-Schwelle, die Liquidierung auslöst (Währungseinheiten). | 10 |
| `LossClose` | Gesamt-PnL-Boden, der Liquidierung auslöst (Währungseinheiten, typischerweise negativ). | -100 |
| `ExpirationMinutes` | Ausstehende-Order-Lebenszeit in Minuten (0 = gut bis zur Stornierung). | 60 |
| `StopLoss`, `TakeProfit` | Pip-Abstände für schützende Stop-/Take-Orders, die nach einer Füllung erstellt werden (0 deaktiviert). | 50 / 0 |
| `CandleType` | Kerzenserie für periodische Wartung. | 1-Minuten-Kerzen |

## Verwendungshinweise
* Aktivieren Sie mindestens einen der vier booleschen Schalter (`UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit`), um das Erstellen des Rasters zu ermöglichen.
* Die Pip-Konvertierung hängt vom `PriceStep` des Wertpapiers ab. Instrumente mit exotischen Tick-Größen erfordern möglicherweise eine Anpassung der Versätze für gleichwertiges Verhalten.
* `ProfitClose`/`LossClose` vergleichen die Summe aus realisiertem PnL (`Strategy.PnL`) und dem unrealisierten PnL, der aus dem letzten besten Bid/Ask berechnet wird; stellen Sie sicher, dass die Preisschritt-Metadaten für das gehandelte Instrument ausgefüllt sind.
* Schutz-Stop- und Take-Orders sind unabhängige StockSharp-Orders; wenn Sie eine Position manuell außerhalb der Strategie schließen, werden die verbleibenden Schutzorders storniert, wenn die Nettoposition auf null zurückkehrt.
* Der Parameter `CandleType` steuert nur, wie oft die Wartung ausgeführt wird; die Order-Platzierung reagiert weiterhin sofort auf Order-Book-Updates.

## Unterschiede zum MT5-Experten-Advisor
* Positions-Buchhaltung wird netto geführt: StockSharp hält eine einzige Nettoposition pro Wertpapier, ähnlich dem MT5-Netting-Regime.
* Anstelle der eingebauten Stop-Loss/Take-Profit-Felder von MT5 auf ausstehenden Orders werden StockSharp-Schutzorders erst nach der Ausführung einer Einstiegsorder erstellt.
* Volumen-Normalisierung verwendet `Security.VolumeStep`, `MinVolume` und `MaxVolume`; überprüfen Sie diese Werte beim Handel mit CFDs oder Krypto-Exchanges.
* Die Strategie stellt keine separate *Alles-schließen*-Schaltfläche bereit — die Liquidierungsroutine ist vollständig automatisch durch die PnL-Schwellen, was der ursprünglichen Expertenlogik entspricht, wo `ProfitClose`/`LossClose` ein vollständiges Herunterfahren auslösten.

## Erste Schritte
1. Weisen Sie die Strategie einem Connector zu, der mindestens Order-Book-Daten und Kerzen für den gewählten `CandleType` liefert.
2. Konfigurieren Sie die vier direktionalen Schalter und Volume-Parameter entsprechend Ihrem Risikoprofil.
3. Definieren Sie Stop-Loss/Take-Profit-Abstände, wenn Schutzorders erforderlich sind (auf null setzen, um zu deaktivieren).
4. Passen Sie `ProfitClose`/`LossClose` an Werte an, die mit Ihrer Kontowährung konsistent sind.
5. Starten Sie die Strategie; sie wartet auf den ersten Order-Book-Snapshot, bevor sie das Raster aufbaut.

> **Python-Version** – nicht bereitgestellt. Nur die C#-Implementierung ist enthalten, wie angefordert.
