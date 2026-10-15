# Up3x1 Premium 2vM-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie ist eine direkte Portierung des MetaTrader 4-Expertenberaters *up3x1_Premium_2vM*. Es handelt ein einzelnes Symbol und hält zu jedem Zeitpunkt höchstens eine Position offen. Die Einträge basieren auf einer Kombination aus geglätteten gleitenden Durchschnitten, starken Kerzenbereichen und einem täglichen Mitternachts-Breakout-Filter. Das Risiko wird durch feste Take-Profit- und Stop-Loss-Abstände gesteuert, die in Preispunkten ausgedrückt werden, während ein optionaler Trailing-Stop das Verhalten des ursprünglichen EA reproduziert, der die Stops kontinuierlich verschärft, sobald sich der Markt zugunsten der Position bewegt.

## Wie es funktioniert

1. Der primäre Zeitrahmen ist konfigurierbar; Der EA verwendete ursprünglich den Diagrammzeitrahmen. Zwei geglättete gleitende Durchschnitte (SMMA) mit den Perioden 12 und 26 sind unter Verwendung des typischen Preises an das Kerzenabonnement gebunden.
2. Ein separater täglicher Kerzenstrom erstellt die D1-Daten neu, die von der MQL-Logik für den Mitternachts-Breakout-Filter und für den einfachen gleitenden 10-Perioden-Tagesdurchschnitt verwendet werden.
3. Im flachen Zustand wertet die Strategie die beiden vorherigen fertigen Kerzen und die zwischengespeicherten SMMA-Werte aus:
   - **Long-Bias**: Entweder kreuzt der schnelle SMMA den langsamen SMMA, während beide Eröffnungen steigen, oder die letzte Kerze zeigt einen bullischen Körper oberhalb der konfigurierten Bereichsschwellen, oder die letzte Tageskerze schließt bullisch nach einer großen Spanne. Der ursprüngliche EA verglich auch den täglichen SMA mit dem Briefkurs; Da die Bedingung immer als wahr ausgewertet wird, wird sie aus Kompatibilitätsgründen beibehalten.
   - **Short-Bias**: Symmetrische Bedingungen der Long-Regeln unter Verwendung rückläufiger Bereiche und Crossovers.
   - Wenn eine Long-Bedingung erfüllt ist, wird ein Marktkauf ausgegeben; andernfalls, wenn eine Short-Bedingung erfüllt ist, wird ein Marktverkauf platziert. Die angefragte Losgröße wird vor Abgabe der Bestellung auf die Sicherheitsvolumenstufe normiert.
4. Während eine Position offen ist, überwacht die Strategie die schnellen/langsamen SMMA-Werte der vorherigen Kerze. Wenn ihre absolute Differenz unter `ConvergenceTolerance` fällt, wird die Position geschlossen, wodurch die Gleichheitsprüfung im Fachberater reproduziert wird.
5. Das abschließende Modul verfolgt den durchschnittlichen Einstiegspreis. Sobald der Preis die Trailing-Distanz überschreitet, wird das Stop-Level erhöht, um die konfigurierte Lücke aufrechtzuerhalten. Durch Berühren dieser Ebene wird die Position sofort geschlossen, wodurch die wiederholten `OrderModify`-Anrufe von MQL nachgeahmt werden.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | `TimeFrame(1h)` | Primärer Zeitrahmen für Einträge. |
| `FastMaPeriod` | `12` | Länge des schnellen geglätteten gleitenden Durchschnitts (typischer Preis). |
| `SlowMaPeriod` | `26` | Länge des langsam geglätteten gleitenden Durchschnitts (typischer Preis). |
| `RangeThreshold` | `0.0060` | Mindestkerzenbereich, der für den Impulsfilter erforderlich ist. |
| `BodyThreshold` | `0.0050` | Mindestgröße des Kerzenkörpers für die Bereichsbedingung. |
| `DailyRangeThreshold` | `0.0060` | Mindestabstand zwischen Öffnung und Schließung der letzten Tageskerze für den Mitternachts-Breakout-Filter. |
| `TakeProfitPoints` | `150` | Take-Profit-Distanz ausgedrückt in Preispunkten. Zum Deaktivieren auf `0` setzen. |
| `StopLossPoints` | `100` | Stop-Loss-Distanz, ausgedrückt in Preispunkten. Zum Deaktivieren auf `0` setzen. |
| `TrailingStopPoints` | `10` | Abstand zwischen Preis und Trailing Stop. Auf `0` setzen, um das Nachstellen zu deaktivieren. |
| `TradeVolume` | `0.05` | Für Marktaufträge verwendete Losgröße vor der Volumennormalisierung. |
| `ConvergenceTolerance` | `0.00001` | Maximale Differenz zwischen den SMMAs, die die Positionsauflösung auslöst. |

## Notizen

- Die Strategie behält die ursprüngliche EA-Eigenart bei, bei der der tägliche SMA-Vergleich immer wahr ist und die Funktionsparität mit der MQL-Quelle garantiert.
- Stop-Loss- und Take-Profit-Orders werden über `StartProtection` registriert und passen sich daher automatisch an die Schrittgröße des Brokers an, sofern verfügbar.
- Die abschließende Logik erfordert sowohl einen positiven `TrailingStopPoints`-Wert als auch einen gültigen `Security.PriceStep`. Wenn eine der Informationen fehlt, wird der Stopp nicht nachgezogen.
- Die Volumennormalisierung berücksichtigt die Austauschbeschränkungen (`VolumeStep`, `VolumeMin`, `VolumeMax`). Negative Werte für `TradeVolume` können verwendet werden, um eine prozentuale Größenanpassung zu emulieren, sobald benutzerdefinierte Logik hinzugefügt wird.
