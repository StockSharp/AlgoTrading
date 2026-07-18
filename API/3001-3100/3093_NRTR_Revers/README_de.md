# NRTR Revers-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die NRTR Revers-Strategie ist eine C#-Konvertierung des originalen MetaTrader 5 Expert Advisors `NRTR_Revers.mq5`. Das System verwendet den Nick Rypock Trailing Reverse (NRTR)-Ansatz, um je nach Preisinteraktion mit ATR-projizierten Unterstützungs- und Widerstandsbändern zwischen Long- und Short-Bias zu wechseln. Handelsentscheidungen werden beim Schlusskurs jeder abgeschlossenen Kerze einer Single-Timeframe-Subscription bewertet.

## Handelslogik

1. **ATR-Projektion** – Die Strategie berechnet einen Average True Range (ATR) mit der konfigurierbaren Periode. Der ATR-Wert wird mit dem `VolatilityMultiplier` multipliziert, um den Bandversatz zu erhalten.
2. **Dynamische Bänder** – Für die aktuelle Trendrichtung findet die Strategie:
   - Das niedrigste Tief (oder höchste Hoch) unter den Kerzen, die mit der ursprünglichen MQL-Fensterkonfiguration übereinstimmen.
   - Ein sekundäres Extrem, das tiefer in die Geschichte verschoben ist. Der Abstand zwischen dem primären Band und diesem sekundären Extrem wird zusammen mit dem `ReversePips`-Schwellenwert verwendet, um starke Umkehrungen zu bestätigen.
3. **Trendwechsel** – Wenn der vorherige Schlusskurs sich außerhalb des ATR-Bandes bewegt oder die sekundäre Extremdifferenz die Umkehrdistanz überschreitet, wechselt der Bias (von Long zu Short oder umgekehrt). Wenn eine entgegengesetzte Position existiert, wird sie zuerst geschlossen; andernfalls wird sofort eine neue Position in der neuen Richtung eröffnet.
4. **Warten auf flache Position** – Nach dem Ausgeben einer entgegengesetzten Market-Order zum Schließen einer bestehenden Position wartet die Strategie, bis das Portfolio flach ist, bevor sie die neue Entry-Order einreicht. Dieses Verhalten spiegelt den ursprünglichen Expert Advisor wider.
5. **Risikomanagement** – Stop-Loss-, Take-Profit- und Trailing-Stop-Levels werden in Pips definiert und mithilfe eines angepassten Punktwerts (kompatibel mit 3- und 5-stelligen Forex-Symbolen) in absolute Preise umgerechnet. Trailing-Updates erfordern einen Preisfortschritt größer als `TrailingStopPips + TrailingStepPips`, entsprechend der MT5-Logik.

## Parameter

- `CandleType` – Primärer Zeitrahmen für die Subscription von Preisdaten.
- `AtrPeriod` – ATR-Glättungslänge für die Bandberechnung.
- `VolatilityMultiplier` – Auf den ATR-Wert angewendeter Multiplikator zur Dimensionierung des Versatzes vom Extrem.
- `ReversePips` – Zusätzliche pip-basierte Distanz, die das sekundäre Extrem überschreiten muss, bevor der Bias kippt.
- `StopLossPips` – Schützende Stop-Distanz in Pips vom Eintrittspreis (auf null setzen zum Deaktivieren).
- `TakeProfitPips` – Gewinnziel-Distanz in Pips vom Eintrittspreis (auf null setzen zum Deaktivieren).
- `TrailingStopPips` – Trailing-Stop-Aktivierungsdistanz in Pips (auf null setzen zum Deaktivieren des Trailings).
- `TrailingStepPips` – Zusätzliche Pip-Distanz, die erforderlich ist, bevor Trailing-Updates auftreten; muss positiv sein, wenn Trailing aktiv ist.
- `TradeVolume` – Ordervolumen für neue Einstiege (in Lots/Kontrakten je nach Wertpapiereinstellungen).

## Hinweise

- Die Indikatorberechnungen und Umkehrprüfungen verwenden nur abgeschlossene Kerzen; unvollständige Kerzen werden ignoriert.
- Der vom Binding gelieferte ATR-Wert entspricht dem ATR der vorherigen Kerze, der im Quell-EA verwendet wird, da Berechnungen nach der Kerzenfertigstellung erfolgen.
- Die angepasste Punktberechnung verarbeitet automatisch 3- und 5-stellige Forex-Kurse, um pip-basierte Parameter mit dem ursprünglichen Skript kompatibel zu halten.
- Kein Python-Port ist auf Anfrage vorgesehen. Der Ordner enthält derzeit nur die C#-Implementierung und Dokumentation.
