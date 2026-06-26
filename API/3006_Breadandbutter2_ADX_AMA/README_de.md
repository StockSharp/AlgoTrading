# Bread and Butter 2 (ADX + AMA)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine Portierung des MetaTrader 5 Expert Advisors *Breadandbutter2* von Ron Thompson. Die ursprüngliche Logik wartet auf eine neue Bar, vergleicht den letzten Average Directional Index (ADX)-Wert mit dem vorherigen, und prüft, ob der Kaufman Adaptive Moving Average (KAMA, auch als AMA bekannt) steigt oder fällt. Eine Long-Position wird eröffnet, wenn die Trendstärke abnimmt und die Preisdynamik sich verbessert, während eine Short-Position eröffnet wird, wenn die Trendstärke zunimmt und die Preisdynamik sich verschlechtert. Die StockSharp-Version behält das Verhalten bei, jede entgegengesetzte Exposition zu schließen, bevor eine neue Order eröffnet wird, und wendet dieselben festen Stop-Loss- und Take-Profit-Abstände an, die im Originalskript in Pips angegeben wurden.

## Indikatoren
- **Average Directional Index (ADX)** – misst die Stärke des aktuellen Trends. Die Strategie betrachtet die ADX-Hauptlinie und vergleicht die letzten zwei Werte, um festzustellen, ob die Trendstärke zu- oder abnimmt.
- **Kaufman Adaptive Moving Average (KAMA/AMA)** – passt sich mithilfe separater schneller und langsamer Glättungskonstanten an die Marktvolatilität an. Die Strategie vergleicht die letzten zwei Werte, um die Richtung der Preisdynamik zu bewerten.

## Strategielogik
1. Mit dem konfigurierten Kerzentyp (Standard: 1-Stunden-Bars) arbeiten und warten, bis eine Kerze vollständig geschlossen ist, bevor sie verarbeitet wird.
2. KAMA mit der ausgewählten Länge, der schnellen Periode und der langsamen Periode berechnen.
3. ADX mit der konfigurierten Mittelungsperiode berechnen und den Wert der Hauptlinie extrahieren.
4. Aktuelle und vorherige Indikatorlesungen vergleichen:
   - **Long-Setup** – der ADX-Wert sinkt (Trendstärke nimmt ab), während KAMA steigt (Preisdynamik verbessert sich).
   - **Short-Setup** – der ADX-Wert steigt, während KAMA fällt.
5. Wenn ein Signal erscheint, jede Exposition der entgegengesetzten Seite schließen und eine neue Marktorder eröffnen, damit die endgültige Position dem Basisstrategie-Volumen entspricht.
6. Die aktive Position kontinuierlich überwachen. Wenn der Preis die konfigurierten Stop-Loss- oder Take-Profit-Niveaus berührt (in Pips ausgedrückt und gemäß der Tick-Größe des Instruments in Preiseinheiten umgerechnet), den Trade sofort beenden.

## Trade-Management
- **Stop-Loss** – in Pips ausgedrückt; in Preiseinheiten mit dem `PriceStep` des Instruments umgerechnet. Für Symbole mit 3 oder 5 Dezimalstellen ist die Pip-Größe 10-mal der Preisschritt, entsprechend der MetaTrader-Implementierung.
- **Take-Profit** – ebenfalls in Pips ausgedrückt und auf dieselbe Weise wie die Stop-Loss-Distanz behandelt.
- Die Strategie verwendet Marktorders für Einstiege und Ausstiege und dreht die Position um, wenn ein entgegengesetztes Signal auftritt.

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Kerzentyp für alle Berechnungen. |
| `AdxPeriod` | `14` | Mittelungslänge der ADX-Hauptlinie. |
| `AmaPeriod` | `9` | Basisperiode des Kaufman Adaptive Moving Average. |
| `AmaFastPeriod` | `2` | Schnelle EMA-Periode innerhalb des AMA. |
| `AmaSlowPeriod` | `30` | Langsame EMA-Periode innerhalb des AMA. |
| `StopLossPips` | `50` | Abstand zum Schutz-Stop-Loss in Pips. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPips` | `50` | Abstand zum Gewinnziel in Pips. Auf `0` setzen zum Deaktivieren. |

## Verwendungshinweise
- Sicherstellen, dass die Strategie einem Wertpapier zugeordnet ist, das einen gültigen `PriceStep` exponiert. Für Forex-Symbole mit Bruchpips wird die Pip-Größe automatisch berechnet.
- `Volume` steuert die Basis-Ordergröße. Wenn ein Umkehrsignal erscheint, fügt der Algorithmus genügend Volumen hinzu, um jede entgegengesetzte Exposition zu schließen und eine Position gleich `Volume` in der neuen Richtung aufzubauen.
- Da Stop-Loss- und Take-Profit-Ausstiege auf Kerzenhochs und -tiefs bewertet werden, approximiert das Verhalten die Pending-Order-Ausführung von MetaTrader.

## Referenzen
- Ursprüngliche MetaTrader 5-Strategie: `MQL/22003/Breadandbutter2.mq5`
- StockSharp-Indikatoren: `KaufmanAdaptiveMovingAverage`, `AverageDirectionalIndex`
