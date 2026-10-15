# Nevalyashka-Breakdown-Level-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Nevalyashka Breakdown Level-Strategie ist eine direkte Umsetzung des MT4-Expertenberaters *Nevalyashka_BreakdownLevel*. Das System erstellt eine Eröffnungsspanne zwischen zwei konfigurierbaren Zeitpunkten und handelt mit Ausbrüchen dieser Spanne. Wenn ein Ausbruch fehlschlägt und der Handel gestoppt wird, kehrt die Strategie sofort die Richtung um und verwendet einen Martingal-Multiplikator, um den Verlust auszugleichen. Profitable Trades blockieren alle weiteren Einträge für den Rest des Handelstages, was dem ursprünglichen EA-Verhalten entspricht.

## Schlüsselkonzepte
- **Eröffnungsbereich:** Das höchste Hoch und das niedrigste Tief, die zwischen `RangeStart` und `RangeEnd` gedruckt werden, definieren den Ausbruchskanal für den aktuellen Tag.
- **Breakout-Einträge:** Eine Long-Position wird eröffnet, wenn der Schlusskurs das Hoch der Spanne überschreitet; Eine Short-Position wird eröffnet, wenn sie unter das Range-Tief fällt.
- **Schutzaufträge:** Der Stop-Loss wird immer auf der gegenüberliegenden Seite der Spanne platziert. Der Take-Profit wird in einem Abstand positioniert, der der Range-Breite entspricht.
- **Breakeven-Bewegung:** Wenn diese Option aktiviert ist, wird der Stop auf den Einstiegspreis verschoben, sobald sich der Handel auf halbem Weg zum Ziel bewegt.
- **Martingale Erholung:** Nach einem Stop-Loss kehrt die Strategie die Richtung um, multipliziert das Ordervolumen mit `MartingaleMultiplier` und verwendet eine symmetrische Ziel-/Stoppgröße, um den vorherigen Verlust auszugleichen.
- **Tägliche Sperre:** Jeder profitable Abschluss (Take-Profit oder manueller Ausstieg über Null) verhindert neue Geschäfte, bis sich der Handelstag ändert.
- **Erzwungene Flatrate:** Wenn `OrdersCloseTime` später als `RangeEnd` ist, werden alle offenen Positionen zu diesem Zeitpunkt geschlossen und neue Einträge werden für den Rest des Tages blockiert.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `RangeStart` | Startzeit (einschließlich) des Referenzbereichs. | `04:00` |
| `RangeEnd` | Endzeit (einschließlich) des Referenzbereichs. | `09:00` |
| `OrdersCloseTime` | Tageszeit für die erzwungene Schließung von Positionen. Wenn dieser Zeitpunkt nach `RangeEnd` liegt, werden auch danach neue Trades blockiert. | `23:30` |
| `OrderVolume` | Für jeden Breakout-Trade verwendetes Volumen. | `0.1` |
| `MartingaleMultiplier` | Der Multiplikator wird auf die nächste Order nach einem Stop-Loss angewendet, um den vorherigen Verlust auszugleichen. | `2` |
| `UseBreakeven` | Ermöglicht das Verschieben des Stops auf die Gewinnschwelle, sobald der Trade die Hälfte der Zieldistanz zurückgelegt hat. | `true` |
| `CandleType` | Kerzentyp, der zum Aufbau der Reichweite und zur Generierung von Signalen verwendet wird. | `1 hour` Kerzen |

## Handelsregeln
1. **Range-Berechnung**: Für jeden neuen Handelstag zeichnet die Strategie die Höchst- und Tiefstwerte der fertigen Kerzen zwischen `RangeStart` und `RangeEnd` (einschließlich) auf.
2. **Eintrittsbedingungen**:
   - Gehen Sie long, wenn der Schlusskurs der aktuellen Kerze über dem aufgezeichneten Hoch der Spanne liegt.
   - Gehen Sie short, wenn der Schlusskurs der aktuellen Kerze unter dem aufgezeichneten Tiefstkurs liegt.
   - Einträge werden übersprungen, wenn eine Martingalumkehr ansteht, am selben Tag bereits ein profitabler Handel stattgefunden hat oder die aktuelle Zeit nach `OrdersCloseTime` liegt (wenn `OrdersCloseTime > RangeEnd`).
3. **Risikomanagement**:
   - Stop-Loss ist auf der gegenüberliegenden Seite der Eröffnungsspanne verankert.
   - Der Take-Profit wird auf den Einstiegspreis plus/minus der Eröffnungsspannenbreite festgelegt.
   - Wenn `UseBreakeven` aktiviert ist, bewegt sich der Stop auf den Einstiegspreis, nachdem die Hälfte der Zieldistanz zurückgelegt wurde.
4. **Martingale Umkehrung**:
   - Wenn der Stop-Loss erreicht wird, wird die Position geschlossen, das Volumen mit `MartingaleMultiplier` multipliziert und eine sofortige Marktorder in die entgegengesetzte Richtung gesendet.
   - Der neue Stop und das neue Ziel werden beide in einem Abstand platziert, der dem Verlust pro Lot dividiert durch den Multiplikator entspricht, was der Wiederherstellungslogik des ursprünglichen EA entspricht.
5. **Tägliche Handelssperre**:
   - Wenn ein Trade mit einem nicht negativen Gewinn endet oder das Ziel erreicht wird, sind keine neuen Trades zulässig, bis sich das Handelsdatum ändert.
6. **Zwangsausgang**:
   - Wenn `OrdersCloseTime` nach dem Bereichsfenster liegt und die aktuelle Zeit diesen Wert erreicht, werden alle offenen Positionen abgeflacht und der Tag gesperrt.

## Notizen
- Die Strategie verwendet das übergeordnete StockSharp API (`Strategy.SubscribeCandles().Bind(...)`), um nahe an den Rahmenkonventionen zu bleiben.
- Alle zustandsbehafteten Berechnungen (Bereichsgrenzen, ausstehende Martingalbefehle, Breakeven-Zustand) werden in der Strategieklasse gespeichert, um historische Suchvorgänge zu vermeiden.
- Bei der Konvertierung bleibt das ursprüngliche Verhalten von EA erhalten, bei dem Handelstage nach Kalenderdatum gezählt und Martingalschritte unmittelbar nach einem Stopp verwaltet werden.
