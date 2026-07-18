# Handelskanal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Trade Channel ist eine Channel-Reversion-Strategie, die vom Expertenberater MetaTrader „TradeChannel“ übernommen wurde. Das System zeichnet einen Preiskanal vom höchsten Hoch und niedrigsten Tief über eine konfigurierbare Anzahl abgeschlossener Kerzen. Wenn sich der Kanal nicht mehr ausdehnt und der Preis eine seiner Grenzen erneut testet, eröffnet die Strategie eine Position in die entgegengesetzte Richtung und erwartet eine Rückkehr zurück in die Spanne.

### Kernideen
- Verwenden Sie die Indikatoren **Höchster** und **Niedrigster**, um einen Donchian-ähnlichen Kanal zu bilden.
- Setzen Sie voraus, dass der Kanal flach ist (keine neuen Hochs oder Tiefs), bevor Sie einen Handel eröffnen.
- Verringern Sie den Widerstand bei Short-Positionen und den Hauch von Unterstützung bei Long-Positionen.
- Platzieren Sie den ersten Schutzstopp einen Average True Range (ATR) vom Ausbruchspunkt entfernt.
- Optional können Sie den Stop nachziehen, sobald sich der Handel zugunsten der Position bewegt.

## Parameter
| Name | Beschreibung | Standard | Optimierung |
| --- | --- | --- | --- |
| `Volume` | Handelsvolumen in Lots/Kontrakten. | 1 | Aktiviert (0,1 → 2,0, Schritt 0,1) |
| `ChannelLength` | Anzahl der fertigen Kerzen, die zur Berechnung der Kanalgrenzen verwendet werden. | 20 | Aktiviert (10 → 60, Schritt 5) |
| `AtrPeriod` | Zeitraum des ATR-Indikators für die Stoppplatzierung. | 4 | Aktiviert (2 → 20, Schritt 2) |
| `TrailingPoints` | Trailing-Stop-Offset, gemessen in Instrumentenpreisschritten. Auf `0` setzen, um das Nachstellen zu deaktivieren. | 30 | Aktiviert (0 → 100, Schritt 10) |
| `CandleType` | Für die Berechnungen verwendeter Kerzentyp und Zeitrahmen. | 30-minütiger Zeitrahmen | — |

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie und füttern Sie drei Indikatoren: `Highest`, `Lowest` und `ATR`.
2. Warten Sie, bis alle Indikatoren vollständig ausgebildet sind. Die ersten abgeschlossenen Werte initialisieren den Kanalstatus und für diese Kerze werden keine Geschäfte getätigt.
3. Für jede neue fertige Kerze:
   - Aktualisieren Sie die Kanalgrenzen und berechnen Sie den Pivot `(resistance + support + close) / 3`.
   - Überprüfen Sie, ob der Widerstand (oder die Unterstützung) im Vergleich zur vorherigen Kerze unverändert ist. Ein flacher Widerstand ermöglicht kurze Aufbauten, eine flache Auflage ermöglicht lange Aufbauten.
   - **Short-Einstieg:** Wenn der Widerstand flach ist *und* die Kerze entweder das Widerstandshoch berührt oder zwischen dem Pivot und dem Widerstand schließt, senden Sie einen Marktverkaufsauftrag.
   - **Long-Einstieg:** Wenn die Unterstützung flach ist *und* die Kerze entweder das Unterstützungstief berührt oder zwischen der Unterstützung und dem Pivot schließt, senden Sie eine Marktkauforder.
   - Es ist jeweils nur eine Position zulässig. Die Strategie wartet auf das Flat-Channel-Signal, solange keine Trades offen sind.
4. Bei der Einreise:
   - Speichern Sie den Einstiegspreis.
   - Legen Sie den anfänglichen Stopp für Short-Positionen auf `resistance + ATR` und für Long-Positionen auf `support − ATR` fest.
5. Offene Stellen verwalten:
   - **Ausstiegsbedingungen für Long-Positionen:**
     - Der Preis berührt die obere Kanalgrenze, während er flach bleibt.
     - Das Kerzentief kreuzt unter dem Trailing-/Initial-Stop-Level.
   - **Ausstiegsbedingungen für Shorts:**
     - Der Preis berührt die untere Kanalgrenze, während er flach bleibt.
     - Das Kerzenhoch kreuzt über dem Trailing-/Initial-Stop-Level.
6. Trailing Stop (wenn `TrailingPoints` > 0):
   - Konvertieren Sie die Eingabe mithilfe des `Security.Step` des Instruments in Preiseinheiten (fällt auf den Rohwert zurück, wenn der Schritt nicht verfügbar ist).
   - Bei Long-Positionen verschieben Sie den Stop auf `close − offset`, sobald der Schlusskurs den Einstiegspreis um den nachlaufenden Offset übersteigt.
   - Bei Shorts gilt: Sobald der Schlusskurs um den Offset unter den Einstiegspreis fällt, verschieben Sie den Stop auf `close + offset`.
   - Der Trailing Stop bewegt sich nie rückwärts; es verschärft nur das Schutzniveau.

## Notizen
- Alle Entscheidungen werden an fertigen Kerzen getroffen, um mit der ursprünglichen MQL-Logik in Einklang zu bleiben, die `High[1]`, `Low[1]` und `Close[1]` verwendet.
- Die Gleichheitsprüfung zwischen der aktuellen und der vorherigen Kanalgrenze ist gegenüber Instrumentenpreisschritten tolerant, um Gleitkomma-Störungen zu vermeiden.
- Trailing Stops basieren auf korrekten `Security.Step`-Metadaten. Wenn die Börse diesen nicht bereitstellt, wird stattdessen der rohe Punktwert verwendet.
- Die Strategie sendet keine E-Mails und passt die Positionsgröße nicht dynamisch an, da diese Funktionen in der MQL-Implementierung plattformspezifisch waren.
