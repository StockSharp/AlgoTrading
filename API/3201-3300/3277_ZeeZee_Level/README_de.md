# ZeeZee-Level-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die ZeeZee-Level-Strategie repliziert das Verhalten des ursprünglichen MetaTrader Expert Advisors "ZeeZee Level" mit der High-Level-API von StockSharp. Die Strategie analysiert ZigZag-Schwünge auf dem ausgewählten Zeitrahmen und handelt in Richtung des jüngsten Extrems. Schutz-Stop-Loss-, Take-Profit- und Trailing-Stop-Distanzen werden in Pips ausgedrückt, und die Positionsgröße folgt nach Verlusttrades einer martingaleartigen Progression.

## Handelslogik

1. Kerzen werden mit dem durch `CandleType` definierten Zeitrahmen abonniert.
2. Ein `ZigZagIndicator` mit konfigurierbaren Parametern für Tiefe, Abweichung und Backstep verfolgt Swing-Hochs und Swing-Tiefs.
3. Wenn keine Position offen ist, vergleicht die Strategie die Aktualität des letzten bestätigten ZigZag-Hochs und -Tiefs innerhalb des Fensters `ZigZagIdInterval`:
   - Wenn das letzte Swing-Hoch aktueller ist als das letzte Swing-Tief, wird eine Short-Position eröffnet.
   - Wenn das letzte Swing-Tief aktueller ist als das letzte Swing-Hoch, wird eine Long-Position eröffnet.
4. Es wird jeweils nur eine Position gehalten. Das Einstiegsvolumen wird auf den Volumenschritt des Instruments gerundet.
5. Nach dem Eröffnen der Position werden Stop-Loss-, Take-Profit- und optionale Trailing-Stop-Niveaus mit den konfigurierten Pip-Distanzen angehängt. Der Trailing Stop folgt dem Extrempreis, während sich der Trade zu seinen Gunsten bewegt.
6. Positionen werden geschlossen, sobald entweder das Stop-Loss- oder das Take-Profit-Niveau berührt wird. Werden beide Niveaus in derselben Kerze erreicht, entscheidet das zum Einstiegspreis nähere Niveau.
7. Nach jedem Ausstieg wird das Volumen bei Gewinntrades auf den Anfangswert zurückgesetzt oder bei Verlusttrades mit dem Martingale-Faktor multipliziert.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `ZigZagDepth` | Anzahl der Kerzen, die bei der Suche nach neuen ZigZag-Pivots berücksichtigt werden. |
| `ZigZagDeviation` | Minimale Preisbewegung (in Preisschritten), die zur Bestätigung eines neuen Pivots erforderlich ist. |
| `ZigZagBackstep` | Mindestanzahl von Bars, bevor der Indikator die Richtung wechseln kann. |
| `ZigZagIdInterval` | Maximale Anzahl von Bars, die für die Suche nach den letzten ZigZag-Hochs und -Tiefs zurückgeblickt wird. |
| `StopLossPips` | Stop-Loss-Distanz in Pips. Auf null setzen, um zu deaktivieren. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. Auf null setzen, um zu deaktivieren. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. Auf null setzen, um zu deaktivieren. |
| `InitialVolume` | Basis-Handelsvolumen zu Beginn eines Martingale-Zyklus. |
| `MartingaleMultiplier` | Faktor, der nach einer Verlustposition auf das nächste Handelsvolumen angewendet wird. |
| `CandleType` | Kerzentyp und Zeitrahmen für die Analyse. |

## Geldmanagement

- Volumina werden an den Volumenschritt des Instruments angepasst und zwischen den Mindest- und Höchstgrenzen der Börse begrenzt.
- Gewinntrades setzen das Volumen auf `InitialVolume` zurück, während Verlusttrades es mit `MartingaleMultiplier` multiplizieren.

## Risikomanagement

- Stop-Loss-, Take-Profit- und Trailing-Stop-Distanzen werden auf jeder abgeschlossenen Kerze bewertet.
- Der Trailing Stop bewegt sich nur in Trade-Richtung und zieht nie zurück.
- Der Handel wird übersprungen, solange die Strategie bereits eine Position hält oder die ZigZag-Schwünge innerhalb des konfigurierten Intervalls nicht verfügbar sind.

## Hinweise

- Die Strategie verwendet nur geschlossene Kerzen, um dem Verhalten des ursprünglichen Expert Advisors zu entsprechen.
- Pip-Umrechnungen stützen sich auf den `PriceStep` des Instruments. Stellen Sie sicher, dass die Instrumentenmetadaten geladen sind, bevor die Strategie gestartet wird.
