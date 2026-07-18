# Daydream-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Daydream-Strategie** ist eine direkte Umsetzung des MQL4-Expertenberaters *Daydream von Cothool*. Der ursprüngliche Roboter handelt mit dem USD/JPY H1-Chart, indem er nach Ausbrüchen eines aktuellen Preiskanals Ausschau hält und dann die Geschäfte mit einem virtuellen, nachlaufenden Take-Profit verwaltet. Dieser StockSharp-Port behält die gleiche Kernlogik bei, während er den High-Level-API verwendet: Donchian-Kanäle liefern die Breakout-Levels, Aufträge werden über `BuyMarket` / `SellMarket` platziert und die gesamte Nachfolgelogik wird innerhalb der Strategie gehandhabt, ohne tatsächliche Take-Profit-Aufträge an der Börse zu platzieren.

Hauptmerkmale:

- Single-Position-Breakout-System, das die Richtung erst ändert, nachdem eine Kerze außerhalb der vorherigen Kanalextreme schließt.
- Virtueller Take-Profit, gemessen in Pips, der sich mit dem Preis ändert, um Gewinne zu sichern und Trades zu schließen, wenn sie erreicht werden.
- Eintrittsdrosselung, sodass pro Kerze nur eine Handelsaktion (Öffnen/Schließen) stattfinden kann, was der Einschränkung MQL4 `LastOrderTime` entspricht.

## Handelslogik

1. Erstellen Sie einen Donchian-Kanal mit `ChannelPeriod` abgeschlossenen Kerzen und speichern Sie die vorherigen oberen/unteren Ebenen.
2. Wenn eine Kerze **unterhalb** des vorherigen unteren Bandes schließt:
   - Schließen Sie eine bestehende Short-Position.
   - Eröffnen Sie bei der nächsten Kerze eine neue Long-Position mit `OrderVolume` und setzen Sie das virtuelle Take-Profit-Level auf `close + TakeProfitPips * pipSize`.
3. Wenn eine Kerze **über** dem vorherigen oberen Band schließt:
   - Schließen Sie eine bestehende Long-Position.
   - Eröffnen Sie bei der nächsten Kerze eine neue Short-Position und setzen Sie den virtuellen Take-Profit auf `close - TakeProfitPips * pipSize`.
4. Während eine Position aktiv ist, verschärfen Sie den virtuellen Take-Profit-Preis für jeden Balken. Wenn der Preis dieses Niveau bei einer nachfolgenden Kerze erreicht, beenden Sie den Handel.

Die Pip-Größe wird aus der Sicherheit `PriceStep` abgeleitet. Bei JPY-Paaren wird dadurch ein Schritt von 0,001 in ein Inkrement von 0,01 Pip umgewandelt, was dem Verhalten von MQL entspricht.

## Parameter

| Name | Beschreibung | Standard | Notizen |
|------|-------------|---------|-------|
| `OrderVolume` | Für jeden neuen Markteintritt verwendetes Volumen. | `1` | Entspricht der `Lots`-Eingabe des MQL-Experten. |
| `ChannelPeriod` | Anzahl der abgeschlossenen Kerzen im Kanal Donchian. | `25` | Spiegelt `ChannelPeriod` in MQL. |
| `Slippage` | Zulässiger Slippage in Punkten. | `3` | Der Vollständigkeit halber gespeichert; Marktordnungen ignorieren es. |
| `TakeProfitPips` | Entfernung des virtuellen Take-Profits in Pips. | `15` | Bewegt sich mit dem Preis, solange die Position offen ist. |
| `CandleType` | Zeitrahmen für die Erstellung des Donchian-Kanals. | `1 hour` | Standardzeitrahmen der ursprünglichen Strategie. |

## Workflow-Diagramm

„
Kerze schließt
│
├─► Donchian-Kanal aktualisieren (vorherige Bänder)
│
├─► Ausbruch unter vorheriges Tief? ──► Kurz schließen → Lang nächsten Takt einplanen
│
├─► Ausbruch über vorheriges Hoch? ─► Lang schließen → nächsten Takt kurz einplanen
│
└─► Trail virtueller Take-Profit in Richtung der offenen Position
└─► Preis virtuelles Ziel erreicht? → Schließstellung
„

## Nutzungshinweise

- Bringen Sie die Strategie mit Streaming-Kerzen an jedes Wertpapier an. Die Standardeinstellungen entsprechen der ursprünglichen USD/JPY H1-Empfehlung.
- Es existiert immer nur eine Position. Die Strategie verhindert das Öffnen und Schließen von Trades innerhalb derselben Kerze, um die MQL4-Logik zu reproduzieren.
- Der Take-Profit ist virtuell: Der Ausstieg erfolgt durch eine Marktorder, sobald das berechnete Niveau überschritten wird. Es werden keine tatsächlichen TP-Aufträge an den Broker gesendet.
- Passen Sie `CandleType` an, um es in unterschiedlichen Zeitrahmen auszuführen. Höhere Zeiträume erfordern ausreichend historische Daten, um den Donchian-Kanal aufzuwärmen.

## Unterschiede zur MQL4-Version

- Verwendet den Indikator StockSharp `DonchianChannels`, anstatt Hochs und Tiefs manuell zu scannen.
- Trailing Take Profit und Aktionsdrosselung bleiben erhalten, aber die Ausführung verwendet StockSharp Marktaufträge, ohne auf die MT4-Ticketverwaltung angewiesen zu sein.
- Der Parameter `Slippage` wird aus Paritätsgründen beibehalten, obwohl die Marktausführung in StockSharp keine Slippage auf die gleiche Weise wie MT4 anwendet.

## Dateien

- `CS/DaydreamStrategy.cs` – Strategieimplementierung in C#.
- Python-Version: noch nicht implementiert.
