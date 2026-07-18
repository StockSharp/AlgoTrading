# Exp Adaptive Renko MMRec Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader 5 Expert Advisor **Exp_AdaptiveRenko_MMRec_Duplex.mq5** auf die StockSharp High-Level-API. Zwei unabhängige Adaptive Renko-Streams – einer für Long-Gelegenheiten und einer für Shorts – beobachten, wie die benutzerdefinierten Ziegelkanäle zwischen Unterstützung und Widerstand wechseln. Wenn der Long-Kanal frische Unterstützung meldet, während der Short-Kanal Widerstand verliert (oder umgekehrt), öffnet die Strategie die entsprechende Marktposition. Die C#-Version behält den originalen „MM Recounter"-Geldverwaltungsblock, der die Handelsgröße nach einer konfigurierbaren Verlustserie reduziert und sie wiederherstellt, sobald die Serie endet.

## Kern-Workflow

1. **Datenabonnements** – jede Seite abonniert ihren eigenen Kerzentyp (Zeitrahmen) und bindet einen Volatilitätsindikator (ATR oder Standardabweichung) über `SubscribeCandles().BindEx(...)`. Der Indikator steuert die adaptive Ziegelhöhe.
2. **Adaptive Renko-Verarbeitung** – der Helper `AdaptiveRenkoProcessor` rekonstruiert die MQL-Indikatorlogik und gibt eine Momentaufnahme mit dem neuesten Trend und den Unterstützungs-/Widerstandsniveaus zurück. Signale werden nur auf abgeschlossenen Kerzen ausgewertet.
3. **Einstiegslogik** – wenn der Long-Renko-Snapshot einen Aufwärtstrend anzeigt (Unterstützung erscheint auf der Signalbar), öffnet die Strategie eine Long-Position. Short-Einstiege erfordern einen Abwärtstrend aus dem Short-Stream.
4. **Ausstiegslogik** – entgegengesetzte Renko-Ereignisse schließen eine aktive Position. Zusätzliche Prüfungen erzwingen Stop-Loss- und Take-Profit-Abstände in Preisschritten.
5. **MMRec-Geldverwaltung** – jede Richtung pflegt eine Warteschlange der jüngsten realisierten PnL-Werte. Wenn die Anzahl der Verluste innerhalb des konfigurierten Fensters den Verlustauslöser erreicht, verwendet die nächste Order den reduzierten Geldverwaltungswert (`LongSmallMoneyManagement` / `ShortSmallMoneyManagement`). Andernfalls wird der Normalwert (`LongMoneyManagement` / `ShortMoneyManagement`) verwendet. Das Enum `MarginModeOption` reproduziert die MQL-Sizing-Modi (Lot, Bilanzbeteiligung, verlustbasierte Beteiligung usw.).
6. **Handelsregistrierung** – jeder Ausstieg ruft `RegisterTradeResult` auf, um die MMRec-Warteschlangen zu füttern. Das Warteschlangen-Trimming spiegelt die Originalfunktionen `BuyTradeMMRecounterS` und `SellTradeMMRecounterS` wider, ohne die Terminal-Historie zu scannen.

## Parametergruppen

| Gruppe | Schlüsselparameter | Beschreibung |
| --- | --- | --- |
| Long-Seite | `LongCandleType`, `LongVolatilityMode`, `LongVolatilityPeriod`, `LongSensitivity`, `LongPriceMode`, `LongMinimumBrickPoints`, `LongSignalBarOffset` | Steuern den Adaptive Renko-Stream, der Long-Einstiege erzeugt. |
| Short-Seite | `ShortCandleType`, `ShortVolatilityMode`, `ShortVolatilityPeriod`, `ShortSensitivity`, `ShortPriceMode`, `ShortMinimumBrickPoints`, `ShortSignalBarOffset` | Spiegeln die Einstellungen für das Short-Modul wider. |
| MMRec | `LongTotalTrigger`, `LongLossTrigger`, `LongSmallMoneyManagement`, `LongMoneyManagement`, `LongMarginMode`, `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement`, `ShortMoneyManagement`, `ShortMarginMode` | Replizieren den Geldverwaltungs-Erholungsblock. Die *TotalTrigger*-Parameter definieren die rollende Fenstergröße, *LossTrigger* die Verlustanzahl, die das reduzierte Volumen aktiviert. |
| Risiko | `LongStopLossPoints`, `LongTakeProfitPoints`, `ShortStopLossPoints`, `ShortTakeProfitPoints`, `LongDeviationSteps`, `ShortDeviationSteps` | Drücken Schutzlevel und informativen Slippage in Preisschritten aus. |

## Verhaltenshinweise

- Die Strategie arbeitet auf dem Netting-Kontomodell: Vor dem Öffnen eines Long-Trades schließt sie ausstehende Shorts und umgekehrt.
- Positionsgrößen werden durch `CalculateVolume` berechnet. Der Helper unterstützt alle originalen Margin-Modi, einschließlich verlustbasiertem Sizing, das von der konfigurierten Stop-Loss-Distanz abhängt.
- Die gesamte Indikatorverarbeitung erfolgt nur auf abgeschlossenen Kerzen, wie im Quell-EA.
- Protokolle enthalten den Geldverwaltungsmultiplikator und den erwarteten Slippage (in Schritten) für die Nachvollziehbarkeit.

## Dateien

- `CS/ExpAdaptiveRenkoMmrecDuplexStrategy.cs` – Strategieimplementierung mit dem Adaptive Renko-Prozessor und MMRec-Modul.
- `README.md` – englische Dokumentation (diese Datei).
- `README_ru.md` – russische Dokumentation.
- `README_zh.md` – chinesische Dokumentation.
