# Contrarian Trade MA Wöchentliche Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein wöchentliches konträres System, das vom ursprünglichen Expertenratgeber MQL „Contrarian_trade_MA“ umgewandelt wurde. Die Strategie analysiert wöchentliche Kerzenextreme zusammen mit einem einfachen gleitenden Durchschnitt, um gestreckte Bewegungen zu Beginn einer neuen Woche auszublenden.

## Handelslogik

- **Datenquelle**: Wöchentliche Kerzen, bereitgestellt durch den Parameter `CandleType` (standardmäßig ein 7-Tage-Zeitrahmen).
- **Historische Extremwerte**: Die Indikatoren `Highest` und `Lowest` verfolgen die Höchst- und Tiefstwerte der letzten `CalcPeriod` abgeschlossenen Wochen, mit Ausnahme der aktuell bewerteten Kerze.
- **Filter für gleitenden Durchschnitt**: Ein einfacher gleitender Durchschnitt der Länge `MaPeriod`, der auf wöchentliche Schlusskurse angewendet wird, fungiert als Richtungsfilter.
- **Teilnahmebedingungen**:
  - **Kaufen**, wenn der Schlusskurs der Vorwoche über dem verfolgten Höchststand (`highest < previousClose`) liegt oder wenn der gleitende Durchschnitt über dem aktuellen Wocheneröffnungskurs liegt.
  - **Verkaufen**, wenn der Schlusskurs der Vorwoche unter dem verfolgten Tief (`lowest > previousClose`) liegt oder wenn der gleitende Durchschnitt unter dem aktuellen wöchentlichen Eröffnungskurs liegt.
  - Es kann immer nur eine Position offen sein; Gegensignale werden ignoriert, bis der bestehende Handel geschlossen wird.
- **Ausgangsregeln**:
  - Die Position wird unabhängig von der Richtung nach sieben Tagen (604.800 Sekunden) geschlossen.
  - Bei jeder abgeschlossenen wöchentlichen Kerze wird ein Schutzstopp ausgewertet. Die Stoppdistanz wird aus `StopLossPoints * PriceStep` berechnet (fällt auf `1` zurück, wenn in den Metadaten des Instruments kein Schritt angegeben ist).

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CalcPeriod` | `4` | Anzahl der abgeschlossenen Wochen, anhand derer das höchste Hoch und das niedrigste Tief berechnet werden. |
| `MaPeriod` | `7` | Zeitraum des einfachen gleitenden Durchschnitts, der auf wöchentliche Abschlüsse angewendet wird. |
| `StopLossPoints` | `300` | Abstand vom Einstiegspreis zum Stop-Loss, gemessen in Preisschritten. Auf `0` setzen, um den Stopp zu deaktivieren. |
| `Volume` | `0.5` | Bestellgröße in Losen, eingereicht von `BuyMarket`/`SellMarket`. |
| `CandleType` | `7 days` | Zeitrahmen für die Kerzen, die alle Berechnungen steuern. |

## Zusätzliche Hinweise

- Die Strategie ruft den Preisschritt automatisch von `Security.PriceStep` ab. Geben Sie diesen Wert in den Metadaten des Instruments an, um eine genaue Stop-Loss-Platzierung zu gewährleisten.
- `StartProtection()` ist aktiviert, um unerwartete Positionsänderungen zu verfolgen, die außerhalb der Strategie durchgeführt werden.
- Da die Logik auf abgeschlossenen wöchentlichen Kerzen basiert, werden Füllungen beim wöchentlichen Schluss der Signalleiste im Testmodus simuliert.
