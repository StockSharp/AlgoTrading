# Reversing-Martingal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Reversing-Martingal-Strategie** ist ein direkter C#-Port des MetaTrader-Expert-Advisors "Reversing Martingale EA". Sie hält kontinuierlich eine einzelne Marktposition und wechselt nach jedem geschlossenen Deal die Handelsrichtung. Verlusttrades lösen eine Martingal-Volumenprogression aus, während profitable Trades den Zyklus auf die anfängliche Lotgröße zurücksetzen. Alle Positionen werden durch symmetrische Stop-Loss- und Take-Profit-Niveaus in Preispunkten geschützt.

Die Strategie verlässt sich nicht auf Indikatoren oder Marktstruktur. Sie reagiert schlicht auf abgeschlossene Positionen und hält die Kapitalexposure jederzeit aktiv (sofern Handel nicht deaktiviert ist).

## Kernlogik
1. **Anfangskonfiguration**
   - Beim Start sendet die Strategie sofort eine Marktorder mit dem Parameter `Start Volume` und der konfigurierten `First Trade Side`.
   - Schutzorders für Stop-Loss und Take-Profit werden mit der in `Target (points)` angegebenen Distanz angehängt.
2. **Positionsverwaltung**
   - Es kann nur eine Position gleichzeitig offen sein. Die Strategie wartet, bis die aktuelle Position durch Schutzorders oder externe Aktionen vollständig geschlossen ist.
   - Nach jedem Ausstieg dreht die Strategie die Handelsrichtung (Kauf -> Verkauf oder Verkauf -> Kauf).
   - Wenn der letzte Trade einen Verlust realisiert hat, entspricht das nächste Ordervolumen der vorherigen Positionsgröße multipliziert mit `Lot Multiplier`. Andernfalls wird das Volumen auf `Start Volume` zurückgesetzt.
3. **Zyklusfortsetzung**
   - Sobald neues Volumen und Richtung feststehen, wird die nächste Marktorder sofort gesendet, sodass der alternierende Martingal-Zyklus weiterläuft.

## Parameter
| Name | Beschreibung |
| --- | --- |
| **Start Volume** | Anfangsvolumen am Beginn jedes Gewinnzyklus. |
| **Lot Multiplier** | Volumenmultiplikator nach einem Verlusttrade. Muss größer als 1 sein. |
| **First Trade Side** | Richtung des allerersten Trades beim Start der Strategiesitzung. |
| **Target (points)** | Distanz in Preisschritten für Stop-Loss- und Take-Profit-Orders. |
| **Order Comment** | Optionaler Text-Tag, der jeder erzeugten Marktorder zugewiesen wird. |

## Zusätzliche Hinweise
- Die Preisschritt-Distanz wird in `UnitTypes.Step` konvertiert und an `StartProtection` übergeben, sodass Stop-Loss und Take-Profit immer aktiv sind.
- Volumenanpassungen respektieren Volumenschritt, Minimum und Maximum der Security über den Helper `NormalizeVolume`.
- Die Strategie erwartet Ausführungsereignisse vom Connector; wenn der Handel pausiert oder der Connector offline ist, wird der Martingal-Zyklus fortgesetzt, sobald Handel wieder erlaubt ist.
