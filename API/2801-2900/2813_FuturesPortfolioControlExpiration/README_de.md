# Futures-Portfolio-Steuerung-Ablauf-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie rekonstruiert den MetaTrader 5-Expertenberater *Futures Portfolio Control Expiration* auf der StockSharp High-Level-API. Sie verwaltet ein Drei-Leg-Futures-Portfolio, behält die gewünschte Long/Short-Exposition für jedes Leg und rollt automatisch jeden Kontrakt zum nächsten Verfallstermin, wenn die verbleibende Laufzeit unter einen konfigurierbaren Schwellenwert fällt.

Die Implementierung repliziert den ursprünglichen Workflow:
1. Den aktuell handelbaren Kontrakt für jede Futures-Familie anhand eines Kurzzeichens identifizieren (z. B. `MXI` oder `BR`).
2. Die Position öffnen oder anpassen, damit das tatsächliche Portfolio-Volumen dem konfigurierten Lot-Wert entspricht (positiv = Long, negativ = Short).
3. Die Verfallszeit bei jeder abgeschlossenen Kerze einer Heartbeat-Subscription überwachen.
4. Den ablaufenden Kontrakt schließen, den nächsten Verfall in derselben Familie ermitteln und die Zielexposition auf dem neuen Kontrakt wiederherstellen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `BoardCode` | Exchange-Board, das an Futures-Kennungen angehängt wird (z. B. `FORTS`). Leer lassen, wenn der Anbieter kein Board-Suffix benötigt. | `FORTS` |
| `Symbol1`, `Symbol2`, `Symbol3` | Kurzcodes der drei Futures-Familien. Die Strategie iteriert Futures-Verfall durch Aufbau von Kennungen wie `CODE-M.YY`. | `MXI`, `BR`, `SBRF` |
| `Lot1`, `Lot2`, `Lot3` | Ziel-Positionsgröße pro Leg. Positive Werte erzeugen Long-Exposition, negative Werte erzeugen Short-Exposition. | `-4`, `-1`, `5` |
| `HoursBeforeExpiration` | Anzahl der Stunden vor Kontraktverfall, wann das Rollen beginnen soll. | `25` |
| `MonitoringCandleType` | Kerzentyp, der nur als Heartbeat zur Auslösung von Verfalls-Checks verwendet wird (z. B. Stundenkerzen). | Zeitrahmen `1H` |

## Rollen und Positionsverwaltung
- **Kontrakt-Entdeckung.** Für jedes Leg scannt die Strategie bis zu zwölf aufeinanderfolgende Kalendermonate. Sie versucht mehrere Kennungsformate (`CODE-M.YY`, `CODE-MM.YY`, `CODEMMYY`, `CODEMYY`) und hängt optional den konfigurierten `BoardCode` an. Nur Wertpapiere mit einem Verfallsdatum nach der Referenzzeit sind zugelassen.
- **Heartbeat-Updates.** Eine Kerzen-Subscription auf jedem aktiven Kontrakt liefert einen Fertig-Kerzen-Callback, der Verfalls-Timer neu bewertet und die Portfolio-Exposition synchronisiert.
- **Roll-Logik.** Wenn die verbleibende Laufzeit kleiner oder gleich `HoursBeforeExpiration` ist, schließt die Strategie offene Positionen auf dem aktuellen Kontrakt, lokalisiert den nächsten Future mit einem späteren Verfall, abonniert Heartbeat-Kerzen neu und stellt das Ziel-Lot auf dem neuen Kontrakt wieder her.
- **Positions-Synchronisation.** Nach jedem Heartbeat wird die tatsächliche Position mit dem Ziel-Lot verglichen. Die Strategie erhöht oder verringert die Exposition mit Market-Orders, damit die Live-Position immer dem angeforderten Volumen entspricht (einschließlich null).

## Verwendungshinweise
1. Stellen Sie sicher, dass der `SecurityProvider` alle Futures-Symbole für die ausgewählten Familien kennt. Konfigurieren Sie `BoardCode`, wenn Ihre Datenquelle Kennungen wie `Si-9.23@FORTS` erfordert.
2. Starten Sie die Strategie mit den gewünschten Portfolio-Parametern. Positionen werden nur eröffnet, wenn die Strategie online ist und der Handel erlaubt ist.
3. Die Strategie protokolliert jede Zuweisung, Anpassung und jedes Roll-Ereignis. Verwenden Sie diese Meldungen, um die Zuordnung zwischen Kurzzeichen und tatsächlichen Futures zu überprüfen.
4. Da die Heartbeat-Subscription nur ein Timer ist, können Sie jeden Kerzentyp wählen, der für die gehandelten Instrumente konsistent verfügbar ist.

## Implementierungsdetails
- High-Level-API-Komponenten (`SubscribeCandles`, `StrategyParam`, `BuyMarket`/`SellMarket`) halten den Code prägnant und entsprechen den Projektrichtlinien.
- Keine benutzerdefinierten Sammlungen historischer Daten werden gespeichert; die Strategie arbeitet nur mit dem letzten Kerzen-Ereignis und dem Positionszustand.
- Englische Kommentare im Code beschreiben jeden wichtigen Schritt für eine einfachere Wartung.
