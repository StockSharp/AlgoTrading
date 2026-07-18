# Three Neural Networks-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist ein hochrangiger StockSharp-Port des MetaTrader-Expert-Advisors „Three neural networks". Sie funktioniert vollständig über die StockSharp-Kerzen-Abonnement-API und verwendet integrierte `SmoothedMovingAverage`-Indikatoren, um die drei neuronalen Schichten der ursprünglichen Implementierung zu emulieren. Die Strategie operiert auf drei verschiedenen Zeitrahmen (H1, H4, D1) und analysiert die Steigung jedes geglätteten Durchschnitts, um eine kollektive Handelsentscheidung abzuleiten.

## Arbeitsablauf

1. Beim Start abonniert die Strategie H1-, H4- und D1-Zeitrahmen-Kerzen und bindet geglättete gleitende Durchschnitte, die den Medianpreis verwenden, was den `iMA(..., MODE_SMMA, PRICE_MEDIAN)`-Aufrufen von MetaTrader entspricht.
2. Jeder Zeitrahmen führt einen rollierenden Verlauf, der den konfigurierten Shift respektiert. Sobald vier verschobene Werte verfügbar sind, berechnet der Algorithmus drei neuronale Ausgaben mit genau derselben gewichteten Differenzformel wie der EA und rundet das Ergebnis auf vier Dezimalstellen.
3. Nach dem Abschluss der H1-Kerze kombiniert die Strategie die neuronalen Ausgaben:
   - Wenn alle drei Werte positiv sind → Long-Position öffnen oder beibehalten.
   - Wenn die H1-Ausgabe positiv ist, während H4- und D1-Ausgaben negativ sind → Short-Position öffnen oder beibehalten.
4. Positionen werden entweder mit einem festen Lot oder einem Risikoprozentmodell dimensioniert. Im Risikomodus weist die Strategie `VolumeOrRisk` Prozent des Portfolio-Werts zu und konvertiert es in Volumen durch Division durch den aktuellen Preis.
5. Die Schutzlogik repliziert die EA-Kontrollen: Stop-Loss und Take-Profit werden sofort nach dem Positionsrichtungswechsel in lokalen Variablen gesetzt, und ein Trailing-Stop wird jedes Mal angepasst, wenn der H1-Bar schließt, wenn der Preis über die Trailing-Distanz plus den konfigurierten Schritt hinausgeht.
6. Jede abgeschlossene H1-Kerze prüft zuerst, ob die aktuellen Stop-Loss- oder Take-Profit-Niveaus überschritten werden, und schließt die Position mit einer Marktorder falls nötig. Optionale ausführliche Protokollierung reproduziert das ursprüngliche `InpPrintLog`-Flag.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `StopLossPips` | `50` | Schutz-Stop-Abstand in Pips. Auf `0` setzen zum Deaktivieren des Stop-Loss. |
| `TakeProfitPips` | `50` | Take-Profit-Abstand in Pips. Auf `0` setzen zum Deaktivieren des Ziels. |
| `TrailingStopPips` | `15` | Abstand zwischen dem aktuellen Preis und dem Trailing-Stop. |
| `TrailingStepPips` | `5` | Mindestverbesserung bevor der Trailing-Stop erneut bewegt wird. |
| `ManagementMode` | `RiskPercent` | Volumen-Dimensionierungsmodus. `FixedLot` verwendet den Wert direkt als Lotgröße; `RiskPercent` als Prozentsatz des Portfolio-Eigenkapitals. |
| `VolumeOrRisk` | `1` | Lotgröße oder Risikoprozentsatz, abhängig vom Geldmanagement-Modus. |
| `H1Period`, `H1Shift` | `2`, `5` | Periode und Verschiebung des geglätteten gleitenden Durchschnitts H1. |
| `H4Period`, `H4Shift` | `2`, `5` | Periode und Verschiebung des geglätteten gleitenden Durchschnitts H4. |
| `D1Period`, `D1Shift` | `2`, `5` | Periode und Verschiebung des geglätteten gleitenden Durchschnitts D1. |
| `P1`, `P2`, `P3` | `0.1` | Gewichte für die drei H1-neuronalen Komponenten. |
| `Q1`, `Q2`, `Q3` | `0.1` | Gewichte für die drei H4-neuronalen Komponenten. |
| `K1`, `K2`, `K3` | `0.1` | Gewichte für die drei D1-neuronalen Komponenten. |
| `EnableDetailedLog` | `false` | Aktiviert ausführliche Diagnosemeldungen, die der EA-Log-Ausgabe entsprechen. |

## Risikomanagement

- Stop-Loss- und Take-Profit-Niveaus werden aus Pip-Abständen mit der erkannten Pip-Größe übersetzt (mit automatischer 3/5-Ziffern-Anpassung identisch zum ursprünglichen Code) und sofort nach dem Positionsrichtungswechsel angewendet.
- Trailing-Logik folgt den MetaTrader-Bedingungen: wird aktiviert sobald der Preis mehr als `TrailingStopPips + TrailingStepPips` vom Einstieg entfernt ist und rückt nur vor, wenn die Verbesserung den konfigurierten Schritt überschreitet.
- Alle Ausstiege werden mit `ClosePosition()`-Marktorders ausgeführt, da serverseitige Stop-/Limit-Orders in der hochrangigen API nicht verfügbar sind.

## Hinweise

- Die Einfrieren-/Stop-Level-Validierung des EA ist in StockSharp nicht verfügbar, daher verlässt sich die Strategie nur auf die Pip-Größen-Konvertierung und Volumenormalisierung durch `VolumeStep`, `VolumeMin` und `VolumeMax`.
- Risikobasiertes Sizing verwendet den aktuellen Portfolio-Wert und den Einstiegspreis zur Approximation der MetaTrader-Margin-Prüfung. Dies entspricht dem allgemeinen Verhalten ohne Abhängigkeit von broker-spezifischen Margin-Rechnern.
- Optionale Protokollierung kann über `EnableDetailedLog` für Schritt-für-Schritt-Diagnosen ähnlich zu `InpPrintLog` in MetaTrader aktiviert werden.
