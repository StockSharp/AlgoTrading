# HPCS Inter4-Strategie (3518)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie portiert den MetaTrader Expertenberater „_HPCS_IntFourth_MT4_EA_V01_WE“ auf den StockSharp High-Level API. Das ursprüngliche Skript eröffnet sofort eine Long-Position, wendet schützende Stop-Loss- und Take-Profit-Werte in MetaTrader Pips an und schließt den Handel nach einer kurzen Haltedauer zwangsweise. Die C#-Version reproduziert das gleiche Verhalten, indem sie den integrierten Schutzmanager mit einem Ein-Sekunden-Timer kombiniert, der die seit der Eingabe verstrichene Zeit überwacht.

## Handelslogik

1. **Initialisierung**
   - Wenn die Strategie startet, berechnet sie die Pip-Größe von MetaTrader aus der Sicherheit `PriceStep` und der Dezimalgenauigkeit (5- und 3-stellige Symbole verwenden einen 10-fachen Multiplikator).
   - Der High-Level-Helfer `StartProtection` ist mit den angeforderten Take-Profit- und Stop-Loss-Abständen konfiguriert. Die Stop-Loss-Distanz beinhaltet den zusätzlichen Puffer, den das ursprüngliche EA mit `OrderModify` anwendet.
   - Die Lautstärke ist fest und ergibt sich aus dem Parameter `OrderVolume`.

2. **Eintrag**
   - Unmittelbar nach dem Start der Strategie wird eine Single-Market-Kauforder übermittelt. Es werden keine weiteren Einträge vorgenommen.
   - Sobald die erste Füllung gemeldet wird, speichert die Strategie die Ausführungszeit.

3. **Ausstieg**
   - Ein Timer prüft jede Sekunde die Offenstellung.
   - Wenn die Haltedauer `CloseDelaySeconds` erreicht, schließt die Strategie die Long-Position mit einer Market-Sell-Order, wenn das Exposure immer noch positiv ist.
   - Schützende Stop-Loss- und Take-Profit-Orders werden vom Schutzmanager automatisch über Marktausgänge aufrechterhalten.

Die Logik handelt nur in der langen Richtung und spiegelt das Verhalten des MetaTrader-Skripts wider.

## Parameter

| Name | Beschreibung | Standard | Optimierbar |
| --- | --- | --- | --- |
| `OrderVolume` | Festes Volumen, das beim Senden der ersten Marktkauforder verwendet wird. | `1` | Nein |
| `StopLossPips` | Basis-Pip-Distanz von MetaTrader, angewendet auf den anfänglichen Stop-Loss. | `10` | Nein |
| `ExtraStopPips` | Zusätzlicher MetaTrader Pip-Puffer, der nach dem Eintritt vom Stopp abgezogen wird. | `10` | Nein |
| `TakeProfitPips` | MetaTrader Pip Abstand vom Gewinnziel. | `10` | Nein |
| `CloseDelaySeconds` | Zeit in Sekunden, bevor die Position zwangsweise geschlossen wird. `0` deaktiviert den Timer-Exit. | `30` | Nein |

## Implementierungshinweise

- Der Pip-Size-Helfer multipliziert den gemeldeten `PriceStep` mit 10 für 3- und 5-Dezimal-Instrumente, sodass die Parameterwerte die gleiche Skalierung wie in MetaTrader behalten.
- `StartProtection` verwendet `UnitTypes.Price` Abstände, sodass Schutzanordnungen bei Marktaustritten funktionieren, genau wie es EA mit `OrderClose` tat.
- `OnNewMyTrade` zeichnet den ersten ausgeführten Kaufhandel auf, um den Countdown für die Haltedauer zu starten, und setzt den Status zurück, wenn die Position vollständig geschlossen ist.
- Der Timer läuft in Ein-Sekunden-Intervallen, um die ursprüngliche `OnTick`-Zeitprüfung zu reproduzieren und bleibt dabei unempfindlich gegenüber Marktinaktivität.
- Alle Codekommentare sind in englischer Sprache verfasst, um den Repository-Richtlinien zu entsprechen.
