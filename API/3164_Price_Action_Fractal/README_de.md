# Price Action Fractal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein C#-Port des MetaTrader-Expert-Advisors „PRICE_ACTION". Sie kombiniert Williams-Fractals mit gewichteten gleitenden Durchschnitten, Momentum- und MACD-Filtern, um Ausbrüche zu handeln, die durch Preisaktion im gewählten Zeitrahmen bestätigt werden.

## Idee

1. Ausschließlich abgeschlossene Kerzen analysieren; jede Entscheidung wird beim Barschluss des konfigurierten Zeitrahmens getroffen.
2. Neue bullische oder bärische Fractals mit einem 5-Kerzen-Fenster erkennen. Ein neues Down-Fractal signalisiert potenzielle Unterstützung, ein neues Up-Fractal potenzielle Resistance.
3. Den direktionalen Bias mit zwei linear gewichteten gleitenden Durchschnitten (LWMA) bestätigen. Long-Trades erfordern den schnellen LWMA über dem langsamen; Short-Trades erfordern das Gegenteil.
4. Momentum durch Prüfung der absoluten Abweichung des Momentum-Indikators vom neutralen 100er-Level im höheren Zeitrahmen validieren.
5. Einen MACD-Filter (12,26,9 Standard) verwenden: Bullische Setups verlangen MACD über seiner Signallinie, bärische Setups verlangen MACD unter der Signallinie.
6. Sobald alle Filter übereinstimmen, in Ausbruchsrichtung einsteigen und die Position mit festen Stops, einem Trailing-Stop und optionalem Break-Even-Shift verwalten.

## Einstiegsregeln

- **Long-Einstieg**
  - Ein neues Down-Fractal bildet sich auf der aktuellen Kerze (Fünf-Balken-Muster).
  - Fast LWMA &gt; Slow LWMA.
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`.
  - MACD-Hauptlinie &gt; MACD-Signallinie.
  - Die Positionsgröße basiert auf dem Strategievolumen und ist durch `MaxPositionUnits` begrenzt.

- **Short-Einstieg**
  - Ein neues Up-Fractal bildet sich auf der aktuellen Kerze.
  - Fast LWMA &lt; Slow LWMA.
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`.
  - MACD-Hauptlinie &lt; MACD-Signallinie.

## Ausstiegsregeln

- Fester Stop-Loss (`StopLossPoints`) und fester Take-Profit (`TakeProfitPoints`) in Preisschritten.
- Optionaler Trailing-Stop (`TrailingStopPoints`), der dem günstigsten Preis folgt, sobald die Position mindestens die Trailing-Distanz gewonnen hat.
- Optionaler Break-Even-Schutz: nach Erreichen von `BreakEvenTriggerPoints` wird der Stop auf `EntryPrice ± BreakEvenOffsetPoints` verschoben.
- Ausstiege werden mit Marktorders ausgeführt; alle Berechnungen basieren auf Kerzenhochs/-tiefs zur Stop-Erkennung.

## Positionsmanagement

- Die Strategie hält eine einzelne aggregierte Position pro Symbol.
- `Volume` definiert die Basisordergröße. Bei Umkehrung schließt die Strategie zunächst die entgegengesetzte Exposition und eröffnet dann eine neue Position mit der gewünschten Größe.
- `MaxPositionUnits` begrenzt den absoluten Positionswert, um Übergrößen zu vermeiden.

## Parameter

- `CandleType` – Zeitrahmen für alle Indikatoren und Entscheidungen (äquivalent zur MQL-Variable `T`).
- `FastMaPeriod` / `SlowMaPeriod` – Längen der gewichteten gleitenden Durchschnitte (`FastMA`, `SlowMA`).
- `MomentumPeriod` – Momentum-Rückblicklänge (im MQL-Skript auf 14 festgelegt).
- `MomentumThreshold` – minimale absolute Abweichung von 100 zur Momentum-Bestätigung (`Mom_Buy`/`Mom_Sell`).
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – MACD-Konfiguration (12/26/9 Standard).
- `StopLossPoints`, `TakeProfitPoints` – Preisschritt-Abstände für Schutzorders (`Stop_Loss`, `Take_Profit`).
- `TrailingStopPoints` – Trailing-Stop-Abstand (`TrailingStop`).
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – Break-Even-Auslöser und Offset (`WHENTOMOVETOBE`, `PIPSTOMOVESL`).
- `FractalLifetime` – Anzahl der Kerzen, die ein erkanntes Fractal gültig bleibt (`CandlesToRetrace`).
- `MaxPositionUnits` – maximale absolute Positionsgröße (Constraint `Max_Trades` in Lot-Einheiten).
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – Schalter für die jeweiligen Ausstiegsmechanismen.

## Unterschiede zum Original-EA

- Portfolioweite Funktionen wie geldbasierter Take-Profit, Eigenkapital-Stop und E-Mail-/Benachrichtigungsalarme sind nicht implementiert.
- Lot-Optimierungsroutinen aus MetaTrader sind vereinfacht; die Strategie verwendet die StockSharp-Volumenormalisierung.
- Schutzorders werden mit Marktausstiegen statt mit ausstehenden Ordermodifikationen ausgeführt, da StockSharp das Risikomanagement anders handhabt.

## Dateien

- `CS/PriceActionFractalStrategy.cs` – Strategieimplementierung in C#.
- Python-Version ist noch nicht verfügbar.
