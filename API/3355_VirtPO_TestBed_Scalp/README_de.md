# VirtPO TestBed-Kopfhautstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **VirtPOTestBed_ScalpM1** MetaTrader 4 Expertenberater auf die StockSharp hohe Ebene API. Es bleibt bei der ursprünglichen Idee, *virtuelle Pending Orders* zu erstellen, die durch Stochastic Oszillatorkreuzungen aktiviert und ausgeführt werden, sobald die Preisdynamik die Bewegung bestätigt. Alle Filter, Geldverwaltungsregeln und Planungskontrollen aus der MQL-Version wurden mit StockSharp-Indikatoren und Bestellmethoden repliziert.

## Kernlogik

1. **Virtuelle ausstehende Aufträge** – Wenn keine Position offen ist, überprüft die Strategie den Filterblock bei jeder abgeschlossenen Kerze:
   * Der Spread muss unter `SpreadMaxPips` bleiben (bester Bid/Ask, der von Level1 abgerufen wurde).
   * Das durchschnittliche Tick-Volumen der letzten drei Balken muss `VolumeLimit` überschreiten.
   * Die absolute Preisvolatilität (durchschnittliche Körpergröße für `VolatilityPeriod` Balken) muss über `VolatilityLimit` liegen.
   * Die Bandbreite Bollinger (Zeitraum `BollingerPeriod`, Breite 2) muss zwischen `BollingerLowerLimit` und `BollingerUpperLimit` bleiben.
   * Die Handelszeit muss innerhalb des konfigurierten Fensters (`EntryHour` + `OpenHours`) und außerhalb der deaktivierten Wochentage (`Day1`, `Day2`, Freitagsschluss) liegen.
   * SMA-Trendfilter – die Differenz zwischen dem schnellen (`SmaFastPeriod`) und dem langsamen (`SmaSlowPeriod`) SMA in Pips muss in beide Richtungen größer als `SmaDifferencePips` sein.
   * Der Körper des vorherigen Balkens muss kleiner als `LastBarLimitPips` sein, um eine Jagd nach langen Kerzen zu vermeiden.

Wenn die Filter erfolgreich sind, werden Stochastic Crossovers ausgewertet:
   * Ein zinsbullischer Crossover durch `StochasticSetLevel` aktiviert einen **virtuellen Kaufstopp** über dem Gebot um `PoThresholdPips`.
   * Ein rückläufiger Crossover durch `100 - StochasticSetLevel` aktiviert einen **virtuellen Verkaufsstopp** unterhalb des Gebots um denselben Schwellenwert.
Jede virtuelle ausstehende Order merkt sich ihren Ablauf (`PoTimeLimitMinutes`) und die Stop-Loss-/Take-Profit-Abstände von `StopLossPips` und `TakeProfitPips`.

2. **Ausführungsphase** – Wenn `TickLevel` aktiviert ist, hört die Strategie auf eingehende Trades, um virtuelle Aufträge auszuführen, sobald der letzte Preis den Schwellenwert überschreitet. Wenn `TickLevel` deaktiviert ist, wird die Triggerprüfung beim Schließen jeder abgeschlossenen Kerze ausgeführt. Sobald der Preis den virtuellen Stop überschreitet, wird eine Marktorder gesendet und die virtuelle Order gelöscht.

3. **Risikomanagement** – Nach einer Füllung verfolgt die Strategie:
   * Anfängliche Stop-Loss- und Take-Profit-Werte, gemessen in Pips vom Einstiegspreis.
   * Optionaler Trailing Stop (`TrailingStopPips`), der dem Extrempreis seit dem Einstieg folgt.
   * Maximale Haltezeit (`CloseTimeMinutes`). Abhängig von `ProfitType` kann es nach Ablauf des Timers alle Positionen schließen (0), nur die profitablen (1) oder nur die verlierenden (2).

Alle Preisabstände werden mithilfe des Wertpapiers `PriceStep` und des Ziffernmultiplikators aus Pips umgerechnet, wodurch die fünfstellige Broker-Handhabung in der MQL-Implementierung reproduziert wird. Der Standardwert `OrderVolume` wird auf jede Marktorder angewendet. Die Strategie setzt ihren internen Zustand automatisch zurück, wenn die Positionen abflachen.

## Wichtige Hinweise

* Zur genauen Berechnung von Spreads und Triggerniveaus sind Level1-Daten erforderlich. Ohne Aktualisierung der besten Gebote/Briefe blockieren die Filter den Handel.
* Die Ausführung auf Tick-Ebene spiegelt das `TickLevel`-Flag des ursprünglichen EA wider; Wenn die Option deaktiviert ist, wartet die Ausführung auf das Schließen der Kerze, was konservativer ist, sich aber leichter rücktesten lässt.
* Die Strategie behält nur eine einzige Nettoposition bei, genau wie die MQL-Version, die die Anzahl der aktiven Marktaufträge begrenzte.

## Parameter

| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Allgemein | Kerzentyp | Für das Kerzenabonnement verwendeter Zeitrahmen (Standard: 1 Minute). |
| Ausführung | Tick-Level | Verwenden Sie Handelsticks, um virtuelle Aufträge sofort auszuführen. |
| Ausführung | PO-Schwellenwert (Pips) | Abstand in Pips zwischen dem Geldkurs und dem virtuellen Stop-Level. |
| Ausführung | PO-Lebensdauer (min.) | Ablaufzeit für jede virtuelle ausstehende Bestellung. |
| Filter | Maximaler Spread (Pips) | Maximal zulässige Streuung vor Scharfschaltbefehlen. |
| Filter | Lautstärkebegrenzung | Minimales durchschnittliches Tick-Volumen über die letzten drei Balken. |
| Filter | Volatilitätszeitraum | Anzahl der Balken, die zur Mittelung der absoluten Kerzenkörper verwendet werden. |
| Filter | Volatilitätslimit | Minimale durchschnittliche Kerzenkörpergröße (in Pips). |
| Filter | Bollinger Zeitraum | Bollinger Bandberechnungszeitraum. |
| Filter | Bollinger Unten / Oben | Zulässiger Bandbreitenbereich in Pips. |
| Filter | Letztes Taktlimit | Maximale Körpergröße der vorherigen Kerze in Pips. |
| Trend | Schnell SMA / Langsam SMA | Perioden für den Trendfilter des gleitenden Durchschnitts. |
| Trend | SMA Differenz | Mindestabstand von SMA in Pips zur Bestätigung eines Trends. |
| Stochastic | %K / %D / Glatt | Standard-Oszillatorperioden Stochastic. |
| Stochastic | Stochastic Eingestellt | Ebene, mit der virtuelle Pending-Orders aktiviert werden. |
| Stochastic | Stochastic Los | Schwelle, die zur Ausführung des bewaffneten Befehls verwendet wird. |
| Handel | Bestellvolumen | Basis-Market-Order-Volumen. |
| Risiko | Take-Profit / Stop-Loss / Trailing-Stop | Ausgangsentfernungen in Pips. |
| Zeitplan | Deaktivierungstage, erster/zweiter No-Trade-Tag | Wochentagsfilter (zum Deaktivieren 99 verwenden). |
| Zeitplan | Einlasszeit / Öffnungszeiten | Beginn und Dauer des Handelsfensters. |
| Zeitplan | Freitagsschluss | Stunde, nach der der Freitagshandel endet. |
| Risiko | Maximale Lebensdauer | Zeitbasierter Ausstieg in Minuten (zum Deaktivieren auf ≥5000 einstellen). |
| Risiko | Gewinnfilter | 0 – Unabhängig davon schließen, 1 – Nur Gewinner schließen, 2 – Nur Verlierer schließen, wenn der Timer ausgelöst wird. |

## Unterschiede zum Original EA

* Die Hilfsklasse MQL `CPO` wird durch interne Zustandsvariablen ersetzt, die `BuyMarket` / `SellMarket` direkt aufrufen, sobald der Preis das virtuelle Niveau überschreitet.
* Die Stop-Loss- und Take-Profit-Ausführung verwendet Kerzenhochs/-tiefs (für Backtests) oder Tick-Updates, sofern verfügbar. Teilfüllungen oder abgesicherte Positionen aus der ursprünglichen MT4-Umgebung werden nicht unterstützt.
* Die kontobasierte Geldverwaltung (`GLots`) wird nicht portiert; Die Strategie StockSharp verwendet den festen Parameter `OrderVolume`.

Diese Anpassungen bewahren die Handelsidee und passen gleichzeitig zum Single-Position-High-Level-Programmiermodell von StockSharp.
