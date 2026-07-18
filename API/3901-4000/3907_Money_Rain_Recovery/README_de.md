# Strategie zur Wiederherstellung des Geldregens
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 4 Expertenberaters **MoneyRain.mq4** zum StockSharp High-Level API.
- Handelt beim Schluss fertiger Kerzen unter Verwendung eines DeMarker-Oszillatorfilters.
- Behält die ursprünglichen festen Stop-Loss-/Take-Profit-Exits und den Volumen-Recovery-Block bei, der die nächste Ordergröße nach einer Verlustsequenz erhöht.

## Handelslogik
1. Abonnieren Sie die konfigurierte `CandleType` (Standard: 1-Stunden-Kerzen) und berechnen Sie DeMarker mit dem Zeitraum `DeMarkerPeriod`.
2. Wenn keine Position aktiv ist und keine Order aussteht:
   - Kaufen Sie, wenn der aktuelle DeMarker-Wert über `Threshold` liegt.
   - Ansonsten verkaufen.
   - Die Ordergröße ist entweder das Basisvolumen oder das aus früheren Verlusten berechnete Recovery-Volumen.
3. Während eine Position offen ist, überwacht die Strategie jede abgeschlossene Kerze:
   - Long-Positionen schließen, wenn das Kerzentief das Stop-Level berührt (`StopLossPoints` unter dem Einstieg) oder das Kerzenhoch das Ziel erreicht (`TakeProfitPoints` über dem Einstieg).
   - Shorts spiegeln die gleichen Regeln mit umgekehrten Ebenen wider.
4. Nach jedem Ausstieg aktualisiert der Money-Management-Block die aufeinanderfolgenden Verlustzähler und bereitet die nächste Ordergröße vor. Wenn die Pechsträhne `LossesLimit` erreicht, stoppt die Strategie die Eröffnung neuer Positionen und protokolliert eine Warnung.

## Money-Management
- `BaseVolume` wird auf die Austauschregeln (`Security.VolumeStep`, `Security.MinVolume`, `Security.MaxVolume`) normalisiert. Wenn die normalisierte Größe unter die Mindestmenge fällt, wird die Eingabe übersprungen.
- Nach jedem Verlustgeschäft speichert die Strategie das verbrauchte Volumen (skaliert mit dem Basislos) und setzt den Zähler für fortlaufende Gewinne zurück. Der nächste profitable Trade verwendet die ursprüngliche MoneyRain-Formel `baseLot × lossesVolume × (StopLoss + spread) / (TakeProfit − spread)`, um Verluste auszugleichen. Nachfolgende Gewinne fallen auf das Basisvolumen zurück und der Verlustakkumulator wird nach zwei oder mehr aufeinanderfolgenden Gewinnen gelöscht.
- Wenn `FastOptimization` aktiviert ist, wird die Wiederherstellungssperre umgangen und jeder Eintrag verwendet das normalisierte Basisvolumen.
- Der Spread für die Erholungsformel wird anhand des letzten besten Geld-/Briefkurses der Stufe 1 geschätzt. Wenn keine Kurse verfügbar sind, fällt der Spread auf Null zurück.

## Parameter
| Parameter | Beschreibung | Standard | Notizen |
|-----------|-------------|---------|-------|
| `DeMarkerPeriod` | Länge des DeMarker-Oszillators. | `10` | Muss größer als Null sein. |
| `TakeProfitPoints` | Abstand zum Take-Profit in Preisschritten. | `50` | Umgerechnet durch Multiplikation mit `Security.PriceStep`. |
| `StopLossPoints` | Abstand zum Stop-Loss in Preisschritten. | `50` | Muss positiv bleiben, damit die Wiederherstellungsformel gültig bleibt. |
| `BaseVolume` | Grundauftragsvolumen. | `1` | Vor der Einreichung auf Instrumentengrenzen normalisiert. |
| `LossesLimit` | Maximal zulässige Verlusttrades in Folge. | `1 000 000` | Bei Erreichen werden die Eingaben angehalten, bis die Strategie zurückgesetzt wird. |
| `FastOptimization` | Deaktivieren Sie die Wiederherstellungsgröße während der Ausführung des Optimierungsprogramms. | `true` | Hält das Modell für Massentests leicht. |
| `Threshold` | DeMarker-Schwellenwert, der Kauf- und Verkaufssignale trennt. | `0.5` | Abgleich der MT4-Konstante aus dem Quellcode. |
| `CandleType` | Für Signale verwendete Kerzendatenreihen. | `1h` | Für andere Zeitrahmen oder benutzerdefinierte Aggregationen ändern. |

## Nutzungshinweise
- Legen Sie die korrekten Werte für `Security.PriceStep`, `Security.VolumeStep`, `Security.MinVolume` und `Security.MaxVolume` fest, damit Preis-/Volumenumrechnungen gültig bleiben.
- Positive `StopLossPoints` und `TakeProfitPoints` sind erforderlich. Wenn Sie sie auf Null belassen, werden Exits verhindert, die vom ursprünglichen EA abweichen.
- Die Strategie wartet auf tatsächliche Ausführungen, bevor sie ihren internen Status aktualisiert. Daher verarbeitet sie Teilfüllungen durch Verfolgung des gewichteten Ausstiegspreises.
- Wenn das Verlustlimit ausgelöst wird, wird der nächste profitable Handel nicht ausgeführt – starten Sie die Strategie neu oder setzen Sie sie zurück, um den Handel fortzusetzen.
