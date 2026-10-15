# Pendelschwungstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Pendulum Swing Strategy** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters *Pendulum 1_01*. Das ursprüngliche System hält zwei ausstehende Stop-Orders rund um den aktuellen Preis und erhöht deren Volumen nach jeder Ausführung schrittweise. Diese C#-Version reproduziert das gleiche „Swing“-Verhalten mithilfe von StockSharp-Helfern auf hoher Ebene.

Schlüsselideen:

- Behalten Sie symmetrische Buy-Stop- und Sell-Stop-Orders in einem konfigurierbaren Abstand von der letzten geschlossenen Kerze bei.
- Nach jeder Füllung vervielfacht der nächste Stopp auf derselben Seite sein Volumen und implementiert die Martingal-Progression aus der MQL-Quelle.
- Schließen Sie die Position, wenn ein kurzfristiges Pip-Ziel erreicht ist oder wenn das Kontokapital die globalen Gewinn-/Verlustschwellen überschreitet.

## Wie es funktioniert
1. Wenn die Strategie startet, abonniert sie eine benutzerdefinierte Kerzenserie (Standard: 15 Minuten) und optional tägliche Kerzen. Die Tagesspanne steuert den Abstand zwischen dem Marktpreis und den ausstehenden Stopps.
2. Bei jeder fertigen Handelskerze der Algorithmus:
   - Aktualisiert globale aktienbasierte Limits.
   - Überprüft, ob die aktuelle Position das lokale Gewinnziel erreicht hat.
   - Berechnet die Stop-Distanz entweder aus der letzten Tagesspanne oder aus der manuellen Pip-Eingabe und platziert/aktualisiert dann die Buy-Stop- und Sell-Stop-Orders.
3. Wenn eine Stop-Order ausgeführt wird, erhöht sich die entsprechende Fortschrittsstufe, sodass der nächste Stop auf dieser Seite das multiplizierte Volumen verwendet. Sobald `MaxLevels` erreicht ist, werden keine neuen Aufträge für diese Richtung erstellt, bis die Position wieder auf Null zurückkehrt.
4. Globale Take-Profit-/Stop-Loss-Prüfungen laufen nach jeder Kerze und liquidieren das Portfolio, wenn die konfigurierten Aktienschwellen überschritten werden.

## Parameter
| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `BaseVolume` | `decimal` | `0.1` | Volumen des ersten ausstehenden Stopps. |
| `VolumeMultiplier` | `decimal` | `2` | Faktor, der nach jeder gefüllten Ebene auf derselben Seite angewendet wird. |
| `MaxLevels` | `int` | `8` | Maximal zulässige Anzahl an Füllungen in eine Richtung. |
| `ManualStepPips` | `int` | `50` | Stoppdistanz in Pips, wenn die Tagesspanne nicht verfügbar ist. |
| `UseDynamicRange` | `bool` | `true` | Wenn aktiviert, wird der Schritt von der zuletzt abgeschlossenen Tageskerze abgeleitet. |
| `RangeFraction` | `decimal` | `0.2` | Bruchteil der täglichen Reichweite, der als Basis-Stopp-Distanz verwendet wird. |
| `TakeProfitPips` | `int` | `10` | Lokales Pip-Ziel, das die aktuelle Position schließt. Stellen Sie `0` auf Deaktivieren ein. |
| `SlippagePips` | `int` | `3` | Zusätzlicher Puffer zur ausstehenden Distanz hinzugefügt, um einen Schlupf von MetaTrader nachzuahmen. |
| `UseGlobalTargets` | `bool` | `true` | Ermöglicht eigenkapitalbasierte Liquidationsprüfungen. |
| `GlobalTakePercent` | `decimal` | `1` | Eigenkapitalwachstum (in Prozent), das globale Take-Profits auslöst. |
| `GlobalStopPercent` | `decimal` | `2` | Aktienrückgang (in Prozent), der einen globalen Stop-Loss auslöst. |
| `CandleType` | `DataType` | `15m` Kerzen | Zeitrahmen, der für die primäre Handelslogik verwendet wird. |

## Notizen
- Die Positionsgröße berücksichtigt den Lautstärkeschritt des Instruments sowie die minimalen und maximalen Lautstärkeeinstellungen.
- Stoppen Sie die Preisanpassung an die Preisstufe des Instruments und vermeiden Sie ständige Auftragsersetzungen durch Einhaltung einer Preistoleranz.
- Globale Ziele basieren auf `Portfolio.CurrentValue` (oder `BeginValue` als Fallback), daher muss das ausgewählte Portfolio diese Informationen offenlegen.
- Die Strategie verwendet `StartProtection()`, um den integrierten Positionsschutz von StockSharp einmal beim Start zu aktivieren.

## Umrechnungsunterschiede
- Die Zeichnung der UI-Beschriftung und die Kontostandstabellen aus dem ursprünglichen MQL-Skript werden weggelassen.
- Globale Take-Profit-Niveaus folgen prozentualen Aktienschwellenwerten anstelle der in MQL verwendeten rohen Tick-Wert-Arithmetik, wodurch das Verhalten bei allen Brokern konsistent bleibt.
- MetaTrader-spezifische Funktionen wie `OrderModify` werden durch StockSharp-Routinen zur Stornierung und erneuten Übermittlung von Bestellungen ersetzt.
