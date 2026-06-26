# Spearman Rank Correlation Histogram Time Window-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den MetaTrader-Experten **Exp_SpearmanRankCorrelation_Histogram_TimeWeekPeriod** auf der hochrangigen StockSharp-API. Sie abonniert einen einzelnen Kerzen-Stream (Standard: 4-Stunden-Bars) und wertet das Spearman-Rangkorrelationshistogramm aus dem ursprünglichen MQL-Indikator aus. Die Histogrammfarbe bestimmt, ob der kurzfristige Trend bullisch (Werte über null) oder bärisch (Werte unter null) ist. Ein dediziertes Handelsfenster hält die Aktivität zwischen einem konfigurierbaren Wochentag/Zeitbereich, entsprechend den `TimeTrade`-Steuerelementen des Quellcodes.

## Handelslogik
1. **Indikatorberechnung**
   - Bei jeder abgeschlossenen Kerze speichert die Strategie den Schlusskurs und berechnet die Spearman-Rangkorrelation über `RangeLength` Schlüsse.
   - Die Histogrammfarbe wird genau wie im Indikator zugewiesen: `4` wenn die Korrelation über `HighLevel` liegt, `3` wenn zwischen `0` und `HighLevel`, `1` wenn zwischen `LowLevel` und `0`, `0` wenn unter `LowLevel`, und `2` wenn genau null.
   - Signale werden auf dem geschlossenen Bar Nummer `SignalBar` ausgewertet (Standard: der gerade geschlossene Bar). Der vorherige geschlossene Bar wird zur Farbübergangserkennung verwendet.

2. **Handelsmodi** – der Parameter `TradeMode` steuert die Farbinterpretation:
   - **Mode1** – Longs öffnen wenn die Farbe über `2` springt nachdem sie unter `3` war; Shorts öffnen wenn die Farbe unter `2` fällt nachdem sie über `1` war. Jede bullische Farbe fordert auch Short-Schließung, jede bärische Farbe Long-Schließung.
   - **Mode2** – Longs bei Farbe `4` öffnen (Übergang von unter `4`), Shorts bei Farbe `0` öffnen (Übergang von über `0`). Farben größer als `2` schließen Shorts; Farben kleiner als `2` schließen Longs.
   - **Mode3** – Longs bei Farbe `4` öffnen und gleichzeitig Shorts schließen; Shorts bei Farbe `0` öffnen und gleichzeitig Longs schließen.
   - Nach einem erfolgreichen Einstieg erzwingt die Strategie eine Abkühlung entsprechend der Kerzenlänge (die nächste Order in derselben Richtung wird bis zum nächsten Bar-Schluss in MetaTrader aufgeschoben).

3. **Geldmanagement und Ordergröße**
   - `MoneyManagement` in Kombination mit `MarginMode` konvertiert Eigenkapital- oder Risikoanteile in ein Ordervolumen. Positive Werte folgen den ursprünglichen Geldmanagement-Regeln, null fällt auf das Strategie-`Volume` zurück, und negative Zahlen werden als feste Lotgröße interpretiert.
   - Risikobasierte Modi (`LossFreeMargin`, `LossBalance`) erfordern ein positives `StopLossPoints`. Wenn der Stop null ist, fällt die Strategie auf `Volume` zurück, genau wie der EA den Trade ablehnen würde.

4. **Risikomanagement**
   - `StopLossPoints` und `TakeProfitPoints` werden in Preisniveaus mit `Security.PriceStep` übersetzt. Ausstiege werden bei jeder abgeschlossenen Kerze mit Kerzenhoch/-tief geprüft, und alle offenen Positionen werden auf null reduziert wenn ein Niveau getroffen wird.
   - `DeviationPoints` wird für UI-Vollständigkeit beibehalten; StockSharp-Marktorders ignorieren den Wert.

5. **Wöchentliches Handelsfenster**
   - Wenn `TimeTrade` `true` ist, muss die aktuelle Zeit zwischen (`StartDay`, `StartHour`, `StartMinute`, `StartSecond`) und (`EndDay`, `EndHour`, `EndMinute`, `EndSecond`) liegen. Außerhalb dieses Fensters werden alle Positionen auf dem Strategie-Instrument sofort geschlossen, entsprechend dem ursprünglichen Notausstieg.
   - Die Implementierung setzt voraus, dass `StartDay` nicht später als `EndDay` ist. Für überlappende Sitzungen (z.B. Freitag → Montag) die Parameter entsprechend anpassen.

6. **Sonstiges Verhalten**
   - Mindestens `RangeLength + SignalBar + 1` abgeschlossene Kerzen müssen verfügbar sein, bevor Signale generiert werden können.
   - `Direction` ist ein reservierter Schalter aus dem MQL-Indikator; er wird für Parameter-Parität beibehalten, hat aber keine Auswirkung in diesem Port.

## Parameter
| Name | Beschreibung | Standardwert |
| --- | --- | --- |
| `MoneyManagement` | Kapitalanteil oder feste Lotgröße für Positionsgrößen. | `0.1` |
| `MarginMode` | Interpretation von `MoneyManagement` (`FreeMargin`, `Balance`, `LossFreeMargin`, `LossBalance`, `Lot`). | `Lot` |
| `StopLossPoints` | Stop-Loss-Abstand in Preispunkten. | `1000` |
| `TakeProfitPoints` | Take-Profit-Abstand in Preispunkten. | `2000` |
| `DeviationPoints` | Informationeller Slippage-Spielraum in Punkten. | `10` |
| `BuyOpen` / `SellOpen` | Long- oder Short-Positionen öffnen aktivieren. | `true` |
| `BuyClose` / `SellClose` | Long- oder Short-Positionen bei Signalen schließen erlauben. | `true` |
| `TradeMode` | Histogramm-Interpretationsmodus (`Mode1`, `Mode2`, `Mode3`). | `Mode1` |
| `TimeTrade` | Wöchentliches Handelsfenster umschalten. | `true` |
| `StartDay`, `StartHour`, `StartMinute`, `StartSecond` | Fensterstart (Wochentag und Uhrzeit). | `Dienstag`, `8`, `0`, `0` |
| `EndDay`, `EndHour`, `EndMinute`, `EndSecond` | Fensterende (Wochentag und Uhrzeit). | `Freitag`, `20`, `59`, `40` |
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen. | `H4` |
| `RangeLength` | Anzahl der Schlüsse für die Spearman-Korrelation. | `14` |
| `MaxRange` | Maximal erlaubte `RangeLength` (Sicherheitsgrenze). | `30` |
| `Direction` | Reserviertes Indikator-Flag, keine Auswirkung im Port. | `true` |
| `HighLevel`, `LowLevel` | Obere und untere Histogramm-Schwellenwerte. | `0.5`, `-0.5` |
| `SignalBar` | Anzahl geschlossener Bars zurück beim Lesen des Farb-Buffers. | `1` |

Alle anderen Strategiekonfigurationen (Portfolio-Auswahl, Wertpapierzuweisung, Basis-`Volume`, Risikoregeln) folgen dem Standard-StockSharp-Workflow.
