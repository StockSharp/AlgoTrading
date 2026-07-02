# Martingale Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Martingale Breakout Strategy** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters `MartinGaleBreakout.mq5`. Das System
Wartet auf ungewöhnlich große Ausbruchskerzen und platziert eine einzelne Marktorder in Ausbruchsrichtung. Während das Original EA
Verfolgt eine „magische Zahl“, um seine Positionen zu verwalten. Die StockSharp-Implementierung basiert auf dem Strategiekontext, also dem Verhalten
ist praktisch derselbe, wenn die Strategie isoliert ausgeführt wird.

Der Algorithmus konzentriert sich auf zwei Kernideen:

1. **Breakout-Erkennung** – die Strategie untersucht die Größe jeder fertigen Kerze und vergleicht sie mit der durchschnittlichen Spanne der Kerze
vorherigen zehn Kerzen. Wenn die aktuelle Spanne dreimal größer als der Durchschnitt ist und die Kerze stark schließt
Je nach Richtung des Ausbruchs wird ein Handelssignal erzeugt.
2. **Erholung im Martingale-Stil** – die Strategie verfolgt schwankende Gewinne und Verluste. Immer wenn der nicht realisierte PnL den erreicht
Bei konfigurierter Verlustschwelle werden sofort alle offenen Positionen geschlossen und das nächste Gewinnziel, also der folgende Trade, erhöht
versucht, den Schaden auszugleichen. Sobald das erhöhte Ziel erreicht ist, werden die Schwellenwerte auf die ursprünglichen Werte zurückgesetzt.

Der Port behält alle Geldverwaltungsparameter aus dem MQL5-Code bei, einschließlich des für die Marge reservierten Saldoprozentsatzes
prozentuale Gewinn- und Verlustziele und der Multiplikator, der die Take-Profit-Distanz während der Erholungsphase erweitert.

## Handelslogik

1. Abonnieren Sie die konfigurierte Kerzenserie und warten Sie auf fertige Kerzen.
2. Berechnen Sie den Kerzenbereich (`High - Low`) und pflegen Sie einen Puffer fester Größe mit den vorherigen zehn Bereichen, um den zu bestimmen
Referenzdurchschnitt, der zur Ausbruchserkennung verwendet wird.
3. Berechnen Sie den variablen PnL, indem Sie die durchschnittlichen Einstiegspreise für die Long- und Short-Seiten verfolgen. Wenn der nicht realisierte PnL den überschreitet
Wenn das Gewinnziel erreicht ist oder die Stop-Loss-Schwelle überschritten wird, schließen Sie sofort alle Positionen und setzen Sie den Wiederherstellungsstatus wie in der Abbildung zurück
ursprünglicher Fachberater.
4. Überspringen Sie die Auftragserteilung, während die Strategie bereits eine Position hält oder wenn der Verbindungsstatus den Handel nicht zulässt.
5. Wenn eine zinsbullische Ausbruchskerze erscheint, legen Sie die Größe der Order so fest, dass der erwartete Gewinn dem aktuellen Ziel entspricht. Der Take-Profit
Die Entfernung in Preisschritten wird während der Wiederherstellung multipliziert, genau wie der Parameter `TP_Points_Multiplier` aus dem Parameter EA.
6. Validieren Sie das berechnete Volumen anhand der Gerätegrenzen (Minimum, Maximum und Schritt) und stellen Sie sicher, dass die erforderliche Marge eingehalten wird
die konfigurierte Guthabenzuteilung oder die verfügbaren freien Mittel nicht überschreitet. Wenn die Einschränkungen eingehalten werden, reichen Sie eine ein
Marktkaufauftrag.
7. Wiederholen Sie den gleichen Vorgang für rückläufige Ausbrüche und erteilen Sie stattdessen einen Marktverkaufsauftrag.

Die Kombination dieser Regeln stellt das Verhalten des ursprünglichen MetaTrader-Systems wieder her, einschließlich des Übergangs hinein und heraus
des Erholungsmodus nach einem Stop-Loss-Ereignis.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `TakeProfitPoints` | Abstand zwischen Einstiegspreis und Take-Profit-Preis, ausgedrückt in Preisschritten. | `50` |
| `BalancePercentAvailable` | Maximaler Prozentsatz des Kontostands, der für die Marge bei einem einzelnen Trade reserviert werden kann. | `50` |
| `TakeProfitPercentOfBalance` | Zielgewinn ausgedrückt als Prozentsatz des aktuellen Saldos. | `0.1` |
| `StopLossPercentOfBalance` | Stop-Loss-Größe, ausgedrückt als Prozentsatz des aktuellen Saldos. | `10` |
| `RecoveryStartFraction` | Bruchteil des Stop-Loss, der vor dem Wechsel in den Wiederherstellungsmodus verwendet wurde. | `0.1` |
| `RecoveryPointsMultiplier` | Multiplikator, der während der Erholung auf die Take-Profit-Distanz angewendet wird. | `1` |
| `CandleType` | Von der Strategie verwendete Kerzendatenquelle (Zeitrahmen, Tick-Kerzen usw.). | `15-minute time frame` |

## Zusätzliche Hinweise

- Die Volumenberechnung repliziert den MetaTrader-Helfer `CalcLotWithTP`. Daraus wird die Losgröße abgeleitet, die erforderlich ist, um den aktuellen Wert zu erreichen
Gewinnziel für eine bestimmte Preisbewegung und normalisiert das Ergebnis dann auf den Volumenschritt des Instruments.
- Margin-Prüfungen werden nach dem gleichen Prinzip wie `CheckVolumeValue` und dem in MQL verwendeten Balance-Prozentsatz-Filter durchgeführt.
Version. Aufträge werden abgelehnt, wenn die erforderliche Marge den zulässigen Anteil des Guthabens oder die von gemeldeten freien Mittel übersteigt
Das Portfolio.
- Die Strategie storniert alle aktiven Orders, bevor die Positionen reduziert werden, sodass das Verhalten dem `CloseAllOrders`-Helfer von entspricht
der ursprüngliche Fachberater.
- Der interne Bereichspuffer speichert nur zehn Werte und entspricht der Iteration über `iHigh`/`iLow` in der Quelle EA. Nein
Es sind historische Daten erforderlich, die über die letzten zehn Kerzen hinausgehen.
