# e-TurboFx klassische Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **e-TurboFx Classic**-Strategie ist eine direkte C#-Portierung des MetaTrader 4 Expert Advisors, der in `MQL/7262/e-TurboFx.mq4` zu finden ist. Es erkennt eine Impulserschöpfung nach einer Reihe starker Kerzen mit zunehmend größeren Körpern und tritt in die entgegengesetzte Richtung ein. Die StockSharp-Version verwendet die High-Level-Strategie API mit Kerzenabonnements, automatischen Schutzanordnungen und UI-freundlichen Parametern.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzentyp und prüfen Sie nur fertige Kerzen.
2. Messen Sie die Körpergröße der Kerze (`|close - open|`), um eine Ausdehnung zu erkennen.
3. Pflegen Sie zwei Zähler:
   - **Bärische Sequenz** – zählt aufeinanderfolgende bärische Kerzen, deren Körper größer sind als die vorherige bärische Kerze.
   - **Bullish-Sequenz** – zählt aufeinanderfolgende bullische Kerzen, deren Körper größer als die vorherige bullische Kerze ist.
4. Setzen Sie beide Sequenzen zurück, wenn ein Doji (eröffnet gleich geschlossen) erscheint oder wenn eine Position bereits offen ist. Dies ahmt das ursprüngliche EA-Verhalten nach, bei dem jeweils nur ein Trade aufrecht erhalten wird.
5. **Langer Einstieg:** Wenn die Länge der rückläufigen Sequenz den konfigurierten `SequenceLength` erreicht, senden Sie eine Marktkauforder und setzen Sie die Zähler sofort zurück.
6. **Kurzer Einstieg:** Wenn die bullische Sequenzlänge `SequenceLength` erreicht, senden Sie einen Marktverkaufsauftrag und setzen Sie die Zähler zurück.
7. Optionale Stop-Loss- und Take-Profit-Level werden aus Punktabständen in StockSharp Preisschritte übersetzt.

Der Algorithmus wartet daher auf eine kapitulationsähnliche Bewegung, bei der jede Kerze in die gleiche Richtung beschleunigt. Die folgende Umkehrreihenfolge versucht, diese extreme Dynamik abzuschwächen.

## Details zur Implementierung
- Verwendet `SubscribeCandles().Bind(ProcessCandle)`, um fertige Kerzen ohne manuelle Indikatorverwaltung zu verarbeiten.
- Integriert sich in `StartProtection`, sodass Stop-Loss- und Take-Profit-Abstände in Börsenpreisschritte (`UnitTypes.Step`) umgewandelt werden.
- Parameter werden über `Param(...)` registriert, sodass sie in der Benutzeroberfläche angezeigt werden und optimiert werden können.
- Die Strategie funktioniert mit jedem Instrument, das ein gültiges `PriceStep` bereitstellt; andernfalls sollten die Stopp-/Zielentfernungen bei `0` bleiben.
- Während eine Position aktiv ist, wird die Signalerkennung angehalten und interne Zähler werden gelöscht, genau wie beim ursprünglichen MQL-Skript, das sich weigerte, Aufträge zu stapeln.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `SequenceLength` | Anzahl der aufeinanderfolgenden fertigen Kerzen mit expandierenden Körpern, die erforderlich sind, um einen Eintrag auszulösen. | `3` |
| `TakeProfitSteps` | Take-Profit-Distanz gemessen in Preisschritten (Ticks). `0` deaktiviert das Ziel. | `120` |
| `StopLossSteps` | Stop-Loss-Distanz gemessen in Preisschritten (Ticks). `0` deaktiviert den Stopp. | `70` |
| `TradeVolume` | Volumen für Markteintritte. Wenn Sie es ändern, wird die Eigenschaft `Volume` sofort aktualisiert. | `0.1` |
| `CandleType` | Für die Analyse verwendeter Kerzenzeitrahmen. Standardmäßig werden 1-Stunden-Kerzen verwendet. | `1 hour` |

## Nutzungshinweise
- Die Strategie erwartet saubere Kerzendaten. Wenn Sie Instrumente oder Zeitrahmen wechseln, lassen Sie die Caches neu aufbauen, sodass die Zähler nur neue Kerzen widerspiegeln.
- Da das System auf einer strikten Körperexpansion beruht, setzen winzige oder gleichgroße Kerzenkörper die Reihenfolge zurück. Passen Sie `SequenceLength` an, wenn Sie in lauteren Zeitrahmen handeln.
- Testen Sie mehrere Zeitrahmen-/Volumenkombinationen im Backtest, um Instrumente zu finden, bei denen Erschöpfungsbewegungen häufig genug sind, um Spreads und Slippage auszugleichen.
- Überprüfen Sie das Verhalten immer in einer Sandbox-Umgebung, bevor Sie den Live-Handel aktivieren.
