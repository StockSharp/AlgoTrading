# Hans123Trader RangeBreakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
## Überblick
Die **Hans123Trader-Strategie** erstellt den MetaTrader-Expertenberater „Hans123Trader v1“ unter Verwendung des StockSharp-High-Level-API neu. Das System aktiviert Stop-Orders zweimal täglich basierend auf der Unterbrechung der letzten 5-Minuten-Handelsspanne. Es ist auf Forex-Symbole zugeschnitten, bei denen Preisschritte gebrochenen Pips entsprechen. Ausstehende Aufträge werden an jedem Handelstag aktualisiert und alle offenen Positionen werden zwangsweise geschlossen, wenn der Kalender umgestellt wird.

## Kern-Workflow
1. **Bereichsverfolgung** – ein rollierendes 80-Balken-Fenster mit 5-Minuten-Kerzen wird über die Indikatoren `Highest` und `Lowest` aufrechterhalten. Die jüngsten Hochs und Tiefs definieren die Ausbruchsniveaus.
2. **Sitzungsplanung** – zwei unabhängige Handelsfenster werden von `EndSession1` und `EndSession2` gesteuert. Wenn die Uhr die konfigurierte Stunde (Minute `00`) erreicht, berechnet die Strategie neue ausstehende Stop-Orders.
3. **Auftragserteilung** – ein Kaufstopp wird `5` Punkte über dem erkannten Hoch und ein Verkaufsstopp `5` Punkte unter dem erkannten Tief gesetzt. Bestellungen werden entfernt, sobald ein neuer Tag beginnt, der den Ablauf von MetaTrader um 23:59 nachahmt.
4. **Positionsmanagement** – nach dem Einstieg wendet die Strategie den gewünschten anfänglichen Stop-Loss, optionalen Take-Profit und Trailing-Stop an. Schutzniveaus werden in Punkten ausgedrückt und mithilfe des `PriceStep` des Instruments in einen Preis umgerechnet.
5. **Tägliche Hygiene** – wenn eine Position zu Beginn eines neuen Handelstages offen bleibt, wird sie zum Marktwert geschlossen. Alle ausstehenden Bestellungen vom Vortag werden storniert, bevor neue vorbereitet werden.

## Handelsregeln
- **Einstiegssignale**
  - Zwei Ausbruchsversuche pro Tag: einer um `EndSession1`, ein weiterer um `EndSession2` (Stunden sind Broker-/Serverzeit).
  - Kaufstopppreis = `HighestHigh + 5 points`. Verkaufsstopppreis = `LowestLow − 5 points`.
  - Beide Bestellungen verwenden den aktuellen Parameter `Volume` (Standard `1`).
  - Bestellungen werden übersprungen, wenn das Volumen nicht positiv ist.
- **Exit-Logik**
  - Anfänglicher Stop-Loss = Einstiegspreis ± `InitialStopLoss` Punkte (unten für Long-Positionen, oben für Short-Positionen).
  - Take-Profit = Einstiegspreis ± `TakeProfit` Punkte (oben für Long-Positionen, unten für Short-Positionen).
  - Der Trailing-Stop verschärft das Schutzniveau, wenn der Schlusskurs um mindestens `TrailingStop` Punkte weiter in die Gewinnzone geht.
  - Jede Position, die bis zum nächsten Tag bestehen bleibt, wird sofort zum Marktwert geschlossen.
- **Auftragswartung**
  - Ausstehende Stop-Orders werden zu Beginn jedes Kalendertages storniert.
  - Sobald eine Stop-Order ausgelöst (oder storniert/fehlgeschlagen) wird, werden interne Referenzen automatisch gelöscht.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `BeginSession1` / `BeginSession2` | Aus Gründen der UI-Kompatibilität beibehalten (Hinweise zur Startstunde). Die aktuelle Implementierung basiert auf den End-Hour-Triggern. |
| `EndSession1` / `EndSession2` | Stunden (0–23), in denen neue Stoppbefehle aktiviert werden; Die Minuten müssen genau Null sein. |
| `TrailingStop` | Nachlaufdistanz in Punkten. `0` deaktiviert das Trailing. |
| `TakeProfit` | Take-Profit-Distanz in Punkten. `0` deaktiviert Take-Profit. |
| `InitialStopLoss` | Anfängliche Stop-Loss-Distanz in Punkten. `0` verlässt den Handel ohne Schutzstopp, es sei denn, das Trailing wird aktiviert. |
| `CandleType` | Kerzenserie, die für den 80-Bar-Bereich verwendet wird (Standard `TimeSpan.FromMinutes(5)`). |
| `Volume` | Strategie-Basisvolumen, geerbt von `Strategy`. |

## Konvertierungshinweise
- Die MetaTrader-Hilfsfunktionen `OrderSendExtended` und die globale Variablensperre sind nicht erforderlich; StockSharp verwaltet die Parallelität intern.
- Magische Zahlen werden durch explizite Bestellreferenzen (`_session*`-Felder) ersetzt. Auftragslebenszyklusereignisse löschen diese Referenzen, wenn der Auftrag abgeschlossen ist.
- Ausstehende Bestellungen, die um 23:59 Uhr ablaufen, werden nachgeahmt, indem sie zu Beginn eines neuen Tages storniert werden.
- Die Trailing-Stop-Logik verwendet Kerzenschlusskurse als Ersatz für die MetaTrader Geld-/Briefkurse.
- Alle punktbasierten Entfernungen werden mit `Security.PriceStep` multipliziert. Wenn `PriceStep` nicht festgelegt ist, werden die Rohpunktwerte als absolute Preisentfernungen behandelt.

## Nutzungstipps
- Weisen Sie Instrumente mit ordnungsgemäß konfigurierten `PriceStep`, `StepPrice` und `VolumeStep` zu, damit die Point-to-Price-Konvertierung und die Volumenrundung korrekt sind.
- Stellen Sie sicher, dass historische 5-Minuten-Daten verfügbar sind. Die Ausbruchsniveaus hängen von den letzten 80 Kerzen ab.
- Passen Sie `EndSession1`/`EndSession2` an die gewünschten Marktsitzungen an (z. B. Pausen vor London und vor New York).
- Verwenden Sie Designer oder Runner, um `InitialStopLoss`, `TakeProfit` und `TrailingStop` für das ausgewählte Instrument vor der Live-Bereitstellung zu optimieren.
- Kombinieren Sie die Strategie mit StockSharp Risikokontrollen, wenn mehrere Strategien dasselbe Portfolio teilen.
