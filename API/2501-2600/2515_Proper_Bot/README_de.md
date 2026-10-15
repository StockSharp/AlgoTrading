# Proper Bot Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Proper Bot Strategie** ist ein gitterbasiertes Handelssystem, das aus dem originalen MetaTrader 4 "Proper Bot" Expertenberater konvertiert wurde. Die Strategie eröffnet einen gerichtet vorgespannten Korb von Orders, erweitert diesen Korb mithilfe einer konfigurierbaren Distanz-/Volumenkarte und verwaltet den gesamten Zyklus mit einer Kombination aus zeit-, volumen- und preisbasierten Filtern. Der C#-Port nutzt StockSharp-Kerzensubskriptionen und Indikatoren auf hoher Ebene, um die Implementierung nah am verwalteten Trading-Workflow zu halten.

## Funktionsprinzipien
1. **Signalerkennung**
   - Wenn der EMA-Filter aktiviert ist, verfolgt die Strategie schnelle, mittlere und langsame exponentielle gleitende Durchschnitte auf der ausgewählten Kerzenserie. Kreuzungen zwischen den schnellen und langsamen Durchschnitten erzeugen die Richtung, während der mittlere Durchschnitt Trades blockiert, die den Trend noch nicht bestätigt haben.
   - Wenn der Filter deaktiviert ist, verwendet der Algorithmus einfach die Richtung des Körpers der vorherigen abgeschlossenen Kerze.
2. **Vorhandelsfilter**
   - Ein einfacher gleitender Durchschnitt des Kerzenvolumens erzwingt eine Mindestdurchschnittsvolumenanforderung.
   - Das Trading ist nur zwischen den konfigurierbaren Session-Start- und Endzeiten erlaubt.
   - Harte obere und untere Preisniveaus verhindern Käufe zu hoch oder Verkäufe zu niedrig. Extreme Bewegungen über diese Bänder hinaus können auch einen Einstieg in die entsprechende Richtung erzwingen.
3. **Gittererweiterung**
   - Die erste Marktorder verwendet den Parameter `FirstVolume`. Nachfolgende Ergänzungen folgen dem Parameter `GridMap`, der eine Liste von `Distanz/Volumen`-Paaren enthält. Wenn sich der Preis um die konfigurierte Distanz gegen die aktuelle Position bewegt, wird eine neue Order mit dem gemappten Volumen hinzugefügt.
   - Distanzen werden in Preisschritten mithilfe des `PriceStep` des Instruments interpretiert. Wenn der Wert nicht vorhanden ist, fällt die Strategie auf `0.0001` zurück.
4. **Risikomanagement**
   - Der gesamte Korb teilt sich einen aggregierten Take-Profit und Stop-Loss-Abstand, gemessen vom gewichteten durchschnittlichen Einstiegspreis.
   - Ein Trailing-Exit überwacht die Summe des schwebenden Gewinns über alle offenen Orders. Sobald der Gewinn den Aktivierungsschwellenwert überschreitet, schließt jeder Rückgang größer als `TrailStepPoints` den gesamten Zyklus.
   - Wenn eine Ausstiegsbedingung ausgelöst wird, schließt die Strategie die gesamte Position mit einer Marktorder und setzt den Gitterstatus zurück.

## Parameter
| Parameter | Beschreibung | Standardwert |
|-----------|--------------|--------------|
| `FastMaPeriod` | Länge der schnellen EMA im Einstiegsfilter. | 10 |
| `MidMaPeriod` | Optionale mittlere EMA-Länge, die zwischen den schnellen und langsamen Linien liegen muss, um ein Signal zu bestätigen. Auf 0 setzen zum Deaktivieren. | 25 |
| `SlowMaPeriod` | Länge der langsamen EMA im Einstiegsfilter. | 50 |
| `DisableMaFilter` | Wenn aktiviert, ignoriert die Strategie die EMA-Regeln und folgt der vorherigen Kerzenrichtung. | true |
| `VolumePeriod` | Anzahl der Kerzen zur Volumenberechnung. Ein Wert von 0 deaktiviert den Filter. | 1 |
| `VolumeMinimum` | Mindestdurchschnittsvolumen für neue Einstiege. | 69 |
| `HighLevel` | Preisschwelle, die Long-Einstiege darüber blockiert und Shorts erzwingen kann. | 1.50001 |
| `LowLevel` | Preisschwelle, die Short-Einstiege darunter blockiert und Longs erzwingen kann. | 1.40001 |
| `FirstVolume` | Volumen für die erste Order jedes Gitterzyklus. | 0.08 |
| `GridMap` | Liste von `Distanz/Volumen`-Paaren (durch Leerzeichen getrennte Punkte), die definieren, wie weit sich der Preis bewegen muss, bevor die nächste Order hinzugefügt wird. | `120/0.1 ... 120/0.19` |
| `TakeProfitPoints` | Gewinnabstand (in Preisschritten) angewendet auf den gewichteten durchschnittlichen Einstiegspreis des gesamten Korbs. | 10000 |
| `StopLossPoints` | Verlustabstand (in Preisschritten) angewendet auf den gewichteten durchschnittlichen Einstiegspreis des gesamten Korbs. | 30000 |
| `TrailStartPoints` | Mindestschwebender Gewinn, bevor die Trailing-Logik sich aktivieren kann. | 52 |
| `TrailDistancePoints` | Gewinnabstand, der erreicht werden muss (minus Trailing-Schritt), bevor die Trailing-Logik aktiviert wird. | 52 |
| `TrailStepPoints` | Maximaler Gewinnrückgabe, der toleriert wird, sobald die Trailing-Logik aktiv ist. | 2 |
| `StartHour` / `StartMinute` | Beginn der Handelssession (einschließlich). | 06:00 |
| `FinishHour` / `FinishMinute` | Ende der Handelssession (einschließlich, unterstützt Übernacht-Fenster). | 21:00 |
| `CandleType` | Kerzen-Datentyp, der von der Strategie verarbeitet wird. | 1-Minuten-Zeitrahmen |

## Verwendungshinweise
- `GridMap`-Werte werden mit invarianter Kulturdezimalzahl geparst. Stellen Sie sicher, dass Distanzen in Instrumentenpunkten vor dem Schrägstrich und Volumina nach dem Schrägstrich angegeben werden.
- Alle Risikoabstände werden mit dem `PriceStep` des Instruments konvertiert. Wenn das Wertpapier eine andere Tick-Größe aufweist, konfigurieren Sie `PriceStep` entsprechend vor dem Start der Strategie.
- Die Trailing-Implementierung aggregiert den schwebenden Gewinn über alle offenen Orders (passend zum Original-EA), führt aber die Prüfung auf abgeschlossenen Kerzen durch. Schnelle Intrabar-Exits können durch Ausführung der Strategie auf kleineren Zeitrahmen aktiviert werden.
- Erzwungene Einstiege durch Überschreiten von `HighLevel` oder `LowLevel` verwenden den Kerzen-Schlusskurs als Näherungswert für Bid/Ask-Werte.
- Der StockSharp-Port schließt den gesamten Korb, wenn eine Take-Profit-, Stop-Loss- oder Trailing-Bedingung erfüllt ist. Dies unterscheidet sich von der MT4-Implementierung, wo jedes Ticket seinen eigenen Stop/Ziel trägt, vereinfacht aber das übergeordnete Order-Management.

## Unterschiede zur MT4-Version
- Der MT4-EA sendete individuelle Schutzlevels mit jeder Order. Die StockSharp-Implementierung berechnet Ausstiege gegen die kombinierte Position, um innerhalb der High-Level-API zu bleiben.
- Bid/Ask-Preise werden mit dem Kerzen-Schlusskurs angenähert, da StockSharp-Kerzensubskriptionen standardmäßig keine Per-Tick-Spreads liefern.
- Der Trailing-Block verwendet das Größere aus `TrailDistancePoints - TrailStepPoints` und `TrailStartPoints` als Aktivierungsschwelle, damit das Verhalten stabil bleibt, auch wenn sich Parameter überlappen.
- Handelszeiten hängen vom `DateTimeOffset` der eingehenden Kerze ab. Stellen Sie sicher, dass der Datenfeed Zeitstempel in der gewünschten Zeitzone liefert.

## Dateien
- `CS/ProperBotStrategy.cs` – Strategieimplementierung.
- `README.md` – englische Beschreibung.
- `README_zh.md` – chinesische Übersetzung.
- `README_ru.md` – russische Übersetzung.
