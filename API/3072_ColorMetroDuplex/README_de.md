# Strategie ColorMetroDuplexStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

`ColorMetroDuplexStrategy` ist eine C#-Konvertierung des MetaTrader-5-Experten **Exp_ColorMETRO_Duplex**. Der ursprüngliche Roboter verwendet zwei unabhängige Instanzen des ColorMETRO-Indikators, um Long- und Short-Handelsmodule zu verwalten. Jedes Modul arbeitet mit seiner eigenen Kerzenabonnierung, wertet zwei gestaffelte RSI-Envelopes aus, die vom ColorMETRO-Indikator erzeugt werden, und öffnet oder schließt optional Positionen, wenn die schnellen und langsamen Envelopes kreuzen.

Die StockSharp-Version behält beide Module bei und reproduziert dieselben Signalauswertungsregeln, während sie die High-Level-API für Kerzenabonnements, Orderverwaltung und Indikatoranbindung verwendet. Ein benutzerdefinierter `ColorMetroIndicator` ist enthalten, um die MT5-iCustom-Implementierung nachzuahmen, und stellt die schnellen und langsamen ColorMETRO-Bänder zusammen mit dem internen RSI-Wert bereit.

## Funktionsweise

1. Zwei `SignalModule`-Instanzen werden erstellt — **Long** und **Short** — jede mit ihrer eigenen Kerzenserie, ColorMETRO-Einstellungen und Handelsverwaltungsoptionen.
2. Wenn die Strategie startet, abonniert jedes Modul seinen konfigurierten Zeitrahmen und bindet den `ColorMetroIndicator` über `SubscribeCandles(...).BindEx(...)`.
3. Für jede abgeschlossene Kerze erzeugt der Indikator:
   - Das schnelle ColorMETRO-Band (schneller RSI-Envelope).
   - Das langsame ColorMETRO-Band (langsamer RSI-Envelope).
   - Den zugrunde liegenden RSI-Wert (nur als Referenz verwendet).
4. Das Modul speichert die Indikatorhistorie und wertet die letzten zwei Werte mit dem konfigurierten `SignalBar`-Versatz aus (entsprechend der `CopyBuffer`-Logik von MT5).
5. Handelsregeln:
   - **Long-Modul**
     - *Öffnen*: Das schnelle Band lag auf dem vorherigen Balken über dem langsamen und liegt jetzt darunter oder gleich.
     - *Schließen*: Das langsame Band lag auf dem vorherigen Balken über dem schnellen.
   - **Short-Modul**
     - *Öffnen*: Das schnelle Band lag auf dem vorherigen Balken unter dem langsamen und liegt jetzt darüber oder gleich.
     - *Schließen*: Das langsame Band lag auf dem vorherigen Balken unter dem schnellen.
6. Aufträge werden über `BuyMarket` / `SellMarket` geroutet. Die aktuelle Nettoposition wird berücksichtigt — entgegengesetzte Trades schließen das bestehende Engagement, bevor ein neues eröffnet wird.

## Parameter

Jedes Modul stellt eine dedizierte Parametergruppe bereit. Die Standardwerte spiegeln den MT5-Experten wider.

### Gemeinsame Marktparameter

- **Long_Volume**, **Short_Volume** — Handelsgröße (Lots) für neue Einstiege.
- **Long_OpenAllowed**, **Short_OpenAllowed** — Eröffnen von Trades für das Modul aktivieren oder deaktivieren.
- **Long_CloseAllowed**, **Short_CloseAllowed** — Automatische Ausstiege aktivieren oder deaktivieren.
- **Long_MarginMode**, **Short_MarginMode** — Geldverwaltungsmodus für Kompatibilität beibehalten (kein Effekt in diesem Port).
- **Long_StopLoss**, **Long_TakeProfit**, **Long_Deviation**, **Short_StopLoss**, **Short_TakeProfit**, **Short_Deviation** — Für Dokumentationszwecke reserviert; Stops und Slippage-Kontrolle sind in dieser Version nicht automatisiert.
- **Long_Magic**, **Short_Magic** — Ursprüngliche MT5-Magic-Numbers als Referenz beibehalten.

### Indikatorparameter

- **Long_CandleType**, **Short_CandleType** — Zeitrahmen für jedes ColorMETRO-Modul.
- **Long_PeriodRSI**, **Short_PeriodRSI** — RSI-Länge innerhalb des ColorMETRO-Algorithmus.
- **Long_StepSizeFast**, **Short_StepSizeFast** — Schritt (in RSI-Punkten) für das schnelle Envelope.
- **Long_StepSizeSlow**, **Short_StepSizeSlow** — Schritt für das langsame Envelope.
- **Long_SignalBar**, **Short_SignalBar** — Balkenversatz beim Lesen der Indikatorbuffer (identisch mit dem MT5-`SignalBar`-Eingang).
- **Long_AppliedPrice**, **Short_AppliedPrice** — Preisquelle für die RSI-Berechnung (standardmäßig Schlusskurs).

## Unterschiede gegenüber MT5

- **Positionsmodell** — StockSharp-Strategien arbeiten mit der Nettoposition. Der ursprüngliche Experte speicherte separate Positionen über Magic Numbers; der Port schließt das aktuelle Engagement, bevor die entgegengesetzte Seite geöffnet wird.
- **Geldverwaltung** — Margin-Modi und Abweichungseinstellungen werden als Parameter beibehalten, aber nicht automatisch angewendet. Verwenden Sie die `Volume`-Eingaben zur Größensteuerung.
- **Stop-Loss / Take-Profit** — Der MT5-Experte platzierte bei jeder Order Schutzstops. Die StockSharp-Version behält die Abstände als Referenzparameter, aber tatsächliche Stop-Orders müssen bei Bedarf separat implementiert werden.
- **Zeitstufenkontrolle** — Der MT5-Code verwendete globale Variablen, um nur einen Trade pro Signalzeitraum sicherzustellen. In StockSharp verarbeiten wir jede abgeschlossene Kerze einmal und verlassen uns auf die Nettopositionsprüfung, um doppelte Einstiege zu verhindern.

## Hinweise

- Der benutzerdefinierte `ColorMetroIndicator` reproduziert die MT5-Logik einschließlich der gestaffelten RSI-Envelopes und der Trendgedächtnis. Er stellt die schnellen/langsamen Bänder und den internen RSI zum Charting oder zur Fehlersuche bereit.
- Kommentare im Code sind absichtlich ausführlich gehalten, um die Portierungsentscheidungen zu erläutern und weitere Anpassungen zu erleichtern.
- Um Stop-Loss- oder Take-Profit-Automatisierung zu aktivieren, erweitern Sie `SignalModule.ProcessModule`, um Schutzorders mit den Risikokontrollen von StockSharp zu platzieren.
