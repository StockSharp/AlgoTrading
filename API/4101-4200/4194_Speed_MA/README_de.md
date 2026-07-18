# Speed-MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Speed MA Strategy** ist eine direkte StockSharp-Portierung des MetaTrader 4-Expertenberaters `ytg_Speed_MA_ea`. Das ursprüngliche System misst, wie schnell sich ein einfacher gleitender Durchschnitt von einem Balken zum nächsten ändert. Wenn die Steigung des gleitenden Durchschnitts einen benutzerdefinierten Schwellenwert überschreitet, eröffnet der Experte eine Marktposition in die entsprechende Richtung. Diese C#-Implementierung reproduziert dieses Verhalten mit dem übergeordneten API von StockSharp: Sie abonniert Kerzen, wertet einen verschobenen einfachen gleitenden Durchschnitt aus und löst Trades aus, wenn die Differenz zwischen aufeinanderfolgenden verschobenen Werten groß genug ist. Die Strategie behält das Auftragsvolumen, die Gewinnziele und die Stop-Losses, ausgedrückt in MetaTrader „Punkten“, bei, um dem Quellcode treu zu bleiben.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzentyp (standardmäßig Ein-Minuten-Kerzen) und erstellen Sie einen einfachen gleitenden Durchschnitt mit dem Parameter `MovingAveragePeriod`.
2. Notieren Sie für jede fertige Kerze den letzten gleitenden Durchschnittswert. Die Verlaufsliste enthält nur die Werte, die zur Auswertung des konfigurierten `Shift` und des vorherigen Balkens davor erforderlich sind.
3. Berechnen Sie die Steigung als Differenz zwischen dem gleitenden Durchschnittswert `Shift` Balken zurück und dem Wert einen Balken davor (d. h. `Shift + 1` Balken zurück). Dies spiegelt den MetaTrader-Aufruf `iMA(..., shift)` und `iMA(..., shift + 1)` wider.
4. Vergleichen Sie die Steigung mit `SlopeThresholdPoints`, umgerechnet in absolute Preiseinheiten. Wenn die Differenz größer als der positive Schwellenwert ist, erzeugen Sie ein langes Signal. Wenn die Differenz niedriger als der negative Schwellenwert ist, erzeugen Sie ein kurzes Signal.
5. Wenn `ReverseSignals` aktiviert ist, invertieren Sie das generierte Signal, sodass eine zinsbullische Steigung eine Short-Position eröffnet und umgekehrt.
6. Senden Sie nur dann eine neue Marktorder, wenn keine aktive Position vorhanden ist. Der ursprüngliche Fachberater verließ sich auf `OrdersTotal() < 1` und machte nie direkt eine Umkehr; Diese Implementierung verhält sich identisch, indem sie Signale ignoriert, während eine Position offen ist.
7. Schutzanordnungen werden über `StartProtection` verwaltet. Die Stop-Loss- und Take-Profit-Abstände werden in MetaTrader Punkten (`TakeProfitPoints` und `StopLossPoints`) definiert und mithilfe der Dezimalgenauigkeit des Wertpapiers automatisch in Preis-Offsets übersetzt.

## Risikomanagement
- **Stop-Loss** – `StopLossPoints` definiert, wie viele MetaTrader Punkte unter/über dem Einstieg der Schutzstopp platziert wird. Ein Wert von `0` deaktiviert den Stop-Loss.
- **Take-Profit** – `TakeProfitPoints` legt die Gewinnzieldistanz in MetaTrader Punkten fest. Durch die Einstellung `0` wird das Gewinnziel deaktiviert.
- Die Strategie sieht keine Trail-Stops vor und nimmt keine Teilgewinne mit; Es konzentriert sich auf die Nachbildung des ursprünglichen Verhaltens, das sofort feste Ziele festlegt und stoppt, wenn ein Auftrag ausgeführt wird.
- Da der Experte nur dann eine neue Position eröffnet, wenn er flach ist, gibt es nie mehr als eine aktive Position. Dies macht die Positionsgröße vorhersehbar und spiegelt die MetaTrader-Implementierung wider, bei der das Volumen auf 0,1 Lots festgelegt wurde.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Handelsvolumen, das für Markteintritte verwendet wird. Entspricht der Losgröße `0.1` des ursprünglichen EA. | `0.1` |
| `MovingAveragePeriod` | Periode des einfachen gleitenden Durchschnitts, der zur Geschwindigkeitsmessung verwendet wird. | `13` |
| `Shift` | Anzahl der abgeschlossenen Balken zwischen der aktuellen Kerze und dem gleitenden Durchschnitt. Die Strategie vergleicht die Werte bei `shift` und `shift + 1`. | `1` |
| `SlopeThresholdPoints` | Minimale Differenz zwischen den beiden verschobenen gleitenden Durchschnittswerten, gemessen in MetaTrader Punkten. | `10` |
| `ReverseSignals` | Kehren Sie die Handelsrichtung um, sodass ein Aufwärtstrend eine Short-Position eröffnet. | `false` |
| `TakeProfitPoints` | Take-Profit-Distanz ausgedrückt in MetaTrader Punkten (intern in absoluten Preis umgerechnet). | `500` |
| `StopLossPoints` | Stop-Loss-Distanz ausgedrückt in MetaTrader Punkten (intern in absoluten Preis umgerechnet). | `490` |
| `CandleType` | Für Berechnungen verwendeter Kerzentyp (Standard ist ein 1-Minuten-Zeitrahmen). | `1 minute` Zeitrahmen |

## Implementierungshinweise
- Die `Point`-Konstante von MetaTrader wird mithilfe der `Decimals` des Instruments rekonstruiert. Bei Forex-Symbolen mit 5 oder 3 Dezimalstellen dividiert der Code eins durch `10^Decimals`, um denselben Tick-Wert zu erhalten, der in MetaTrader verwendet wird.
- Der Verlauf des gleitenden Durchschnittswerts wird gekürzt, um nur die Stichproben beizubehalten, die für den ausgewählten `Shift` erforderlich sind. Dies vermeidet ein unbegrenztes Speicherwachstum und berücksichtigt gleichzeitig die genauen Indizes, auf die sich der Fachberater bezieht.
- `StartProtection` wandelt die punktbasierten Parameter MetaTrader in StockSharp `Unit` Instanzen mit absoluten Preisversätzen um. Dadurch bleiben die Stop-Loss- und Take-Profit-Abstände identisch mit denen der MQL4-Version.
- Die Strategie nutzt den High-Level-Workflow `SubscribeCandles().Bind(...)`, sodass Indikatoraktualisierungen und Signalauswertung nur bei fertigen Kerzen erfolgen. Es ist kein manueller Anruf an `Indicator.GetValue()` erforderlich.
- Der Quellcode enthält Inline-Kommentare in englischer Sprache, um die kritischen Konvertierungsentscheidungen hervorzuheben.
- Es wird nur die C#-Implementierung bereitgestellt. Auf einen Python-Port wird bewusst verzichtet, passend zur Anfrage.

## Nutzungstipps
- Eine Senkung von `SlopeThresholdPoints` erhöht die Anzahl der Trades, da kleinere Bewegungen des gleitenden Durchschnitts als Signale gelten. Eine Erhöhung des Wertes filtert mehr Trades heraus und erfordert eine stärkere Dynamik.
- Passen Sie `Shift` an, um zu ändern, wie viele Balken zurück die Steigung gemessen wird. Ein Wert von `0` vergleicht den aktuell abgeschlossenen Balken mit dem vorherigen Balken, während höhere Werte ältere Abschnitte des gleitenden Durchschnitts bewerten.
- Kombinieren Sie die Strategie mit StockSharp Risikomodulen oder Kontrollen auf Portfolioebene, wenn zusätzliches Geldmanagement über feste Stopps und Ziele hinaus erforderlich ist.
- Stellen Sie sicher, dass der abonnierte `CandleType` mit dem Zeitrahmen übereinstimmt, der bei der Optimierung des MQL4-Experten verwendet wurde. Unterschiede im Zeitrahmen verändern die Steigungsgröße drastisch.

## Unterschiede zum ursprünglichen Expert Advisor
- Markteintritte und -austritte nutzen die Market-Order-Helfer von StockSharp anstelle von `OrderSend`, aber das resultierende Verhalten (eine Market-Order mit festem SL/TP) bleibt identisch.
- MetaTrader verwaltet Bestellungen anhand der Ticketanzahl; StockSharp überwacht die Gesamtposition. Die Logik, die eine flache Position erfordert, bevor ein neuer Handel eröffnet wird, erstellt `OrdersTotal() < 1` in der neuen Umgebung neu.
- Protokollierung, Diagrammvisualisierung und Einheitenverwaltung nutzen jetzt StockSharp-Funktionen und ermöglichen eine bessere Diagnose, ohne Handelsentscheidungen zu beeinflussen.

## Dateien
- `CS/SpeedMAStrategy.cs` – Strategieumsetzung.
- `README.md`, `README_zh.md`, `README_ru.md` – detaillierte Dokumentation in Englisch, Chinesisch und Russisch.

Gemäß den Konvertierungsrichtlinien ist kein Python-Verzeichnis enthalten.
