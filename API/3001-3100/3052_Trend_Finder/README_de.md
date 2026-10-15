# Trend-Finder-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Trend Finder ist eine Multi-Timeframe-Trendfolge-Strategie, die aus dem ursprünglichen Expertenberater **TREND FINDER.mq4** konvertiert wurde. Die Logik verwendet nun die High-Level-API von StockSharp und behält die Kernidee bei, linear gewichtete gleitende Durchschnitte mit Bestätigungen aus Momentum und MACD-Filtern höherer Zeitrahmen zu kombinieren. Die Strategie konzentriert sich auf die Erkennung von Ausbrüchen, die auf anhaltende Hochs oder Tiefs folgen, mit dem Ziel, in Ausbruchsrichtung einzusteigen, sobald Momentum und langfristige Trendausrichtung bestätigt sind.

## Marktdaten und Indikatoren
- **Basiszeitrahmen (`CandleType`)** – primäre Kerzen für die Mustererkennung und Orderausführung. Die linear gewichteten gleitenden Durchschnitte werden auf den typischen Preis dieser Kerzen berechnet.
- **Momentum-Zeitrahmen (`MomentumCandleType`)** – Kerzen eines höheren Zeitrahmens zur Bewertung von Momentum-Abweichungen vom neutralen Wert 100. Die drei aktuellsten Momentum-Werte müssen konfigurierbare Schwellen überschreiten, bevor ein Trade erlaubt wird.
- **MACD-Zeitrahmen (`MacdCandleType`)** – langfristige Kerzen, die durch einen MACD mit anpassbaren Schnell-, Langsam- und Signallängen verarbeitet werden. Für Long-Setups (Short-Setups) ist eine bullische (bärische) MACD-Bedingung erforderlich.

## Einstiegslogik
1. **Trendausbruchserkennung** – die Strategie scannt bis zu den letzten 100 historischen Kerzen (ohne die drei aktuellsten), um das höchste Hoch oder das niedrigste Tief zu finden. Ein bullisches Setup erfordert, dass die aktuelle Bar über einem vorherigen Cluster von Hochs öffnet, während mindestens eines der drei vorherigen Hochs unter diesem historischen Level bleibt. Ein bärisches Setup spiegelt die Logik für Tiefs wider.
2. **Gleitende-Durchschnitt-Ausrichtung** – der schnelle LWMA muss für Longs über dem langsamen LWMA liegen und für Shorts darunter.
3. **Aktuelle Kerzenstruktur** – für Longs muss das Tief von vor zwei Bars unter dem Hoch der vorherigen Bar liegen (`Low[2] < High[1]`), während Shorts erfordern, dass das letzte Tief unter dem Hoch von vor zwei Bars liegt (`Low[1] < High[2]`). Dies bewahrt die ursprüngliche Preisstrukturprüfung.
4. **Momentum-Bestätigung** – mindestens eine der letzten drei Momentum-Abweichungen (berechnet als |Momentum – 100|) im höheren Zeitrahmen muss die konfigurierten Kauf-/Verkaufsschwellen überschreiten.
5. **MACD-Bestätigung** – der neueste MACD-Wert im langfristigen Zeitrahmen muss für Longs über seiner Signallinie liegen und für Shorts darunter.
6. **Positionsfilterung** – neue Long-Orders werden nur ausgegeben, wenn die aktuelle Position nicht positiv ist, und neue Short-Orders nur, wenn sie nicht negativ ist. Das Ordervolumen entspricht `Volume + |Position|` zur Unterstützung schneller Positionsumkehrungen.

## Ausstieg und Risikomanagement
- **Stop-Loss (`StopLoss`)** – fester Abstand unterhalb (oberhalb) des Einstiegspreises für Long- (Short-) Positionen.
- **Take-Profit (`TakeProfit`)** – festes Gewinnziel; wenn erreicht, wird die Position sofort geschlossen.
- **Trailing Stop (`TrailingStop`)** – verfolgt den höchsten nach dem Einstieg in einen Long erreichten Preis oder den niedrigsten Preis für Shorts. Der Stop wird bei jeder abgeschlossenen Kerze angepasst.
- **Break-Even (`BreakEvenTrigger`, `BreakEvenOffset`)** – sobald sich der Preis um die Auslösedistanz zugunsten des Trades bewegt, wird der Schutz-Stop auf den Einstiegspreis plus (minus) den Offset für Longs (Shorts) verschoben, wodurch Gewinne gesichert werden, wenn der Preis zurückläuft.
- **Automatisches Glätten** – Hilfsmethoden schließen die gesamte Positionsgröße und setzen dann alle Tracking-Variablen zurück. In dieser Implementierung gibt es keine Teilausstiege.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `CandleType` | Basiszeitrahmen für Mustererkennung und Orderausführung. | 15-Minuten-Kerzen |
| `MomentumCandleType` | Höherer Zeitrahmen für die Momentum-Bestätigung. | 1-Stunden-Kerzen |
| `MacdCandleType` | Zeitrahmen für MACD-Bestätigung (Standard ca. 30-Tages-Kerzen). | 30-Tages-Kerzen |
| `FastMaLength` | Länge des schnellen linear gewichteten gleitenden Durchschnitts. | 6 |
| `SlowMaLength` | Länge des langsamen linear gewichteten gleitenden Durchschnitts. | 85 |
| `MomentumPeriod` | Anzahl der Bars im höheren Zeitrahmen für das Momentum-Verhältnis. | 14 |
| `MomentumThresholdBuy` | Minimales |Momentum − 100|, das für Long-Einstiege erforderlich ist. | 0.3 |
| `MomentumThresholdSell` | Minimales |Momentum − 100|, das für Short-Einstiege erforderlich ist. | 0.3 |
| `MacdShortLength` | Schnelle EMA-Länge innerhalb der MACD-Berechnung. | 12 |
| `MacdLongLength` | Langsame EMA-Länge innerhalb der MACD-Berechnung. | 26 |
| `MacdSignalLength` | Signal-EMA-Länge für MACD. | 9 |
| `StopLoss` | Absoluter Stop-Loss-Abstand in Instrumentpreiseinheiten. | 0.0020 |
| `TakeProfit` | Absoluter Take-Profit-Abstand in Instrumentpreiseinheiten. | 0.0050 |
| `TrailingStop` | Trailing-Stop-Abstand, der günstigen Bewegungen folgt. | 0.0040 |
| `BreakEvenTrigger` | Gewinndistanz, die den Break-Even-Stop aktiviert. | 0.0030 |
| `BreakEvenOffset` | Zusätzlicher Offset, der angewendet wird, sobald Break-Even aktiv ist. | 0.0010 |

> **Hinweis:** Setzen Sie die Eigenschaft `Strategy.Volume` auf die gewünschte Ordergröße, bevor Sie die Strategie starten. Die obigen Parameter sind in absoluten Preiseinheiten ausgedrückt; passen Sie sie entsprechend der Tick-Größe des gehandelten Instruments an.

## Verwendungsrichtlinien
1. Weisen Sie die Strategie dem gewünschten `Security` zu und konfigurieren Sie die `Portfolio`- und `Volume`-Eigenschaften.
2. Stellen Sie sicher, dass die ausgewählte Datenquelle alle drei angeforderten Kerzenzeitrahmen liefern kann; andernfalls werden die Bestätigungsfilter nie bereit sein.
3. Passen Sie die Risikoparameter an die Volatilität des Instruments an. Da die Standardwerte als absolute Preisabstände ausgedrückt sind, müssen sie möglicherweise für Aktien, Futures oder Kryptowährungen skaliert werden.
4. Binden Sie optional den generierten Chartbereich ein, um Preis, Trades und beide gleitende Durchschnitte zu visualisieren.
5. Überwachen Sie Logs für Orderbestätigungen. Die Strategie verwendet Marktorders (`BuyMarket`, `SellMarket`) für Ein- und Ausstiege.

## Unterschiede zum ursprünglichen Expertenberater
- Eigenkapitalbasierte Stops, saldobasierte Take-Profit-Logik und Push-/E-Mail-Benachrichtigungen aus dem MQL-Skript wurden absichtlich weggelassen, um die Strategie auf die Kernhandelsregeln zu fokussieren und mit der High-Level-API von StockSharp zu harmonieren.
- Die Volumenverwaltung ist vereinfacht: Die StockSharp-Version eröffnet höchstens eine Nettoposition gleichzeitig und verwendet das konfigurierte `Volume` zur Dimensionierung von Trades.
- In Kontowährung ausgedrückte Geldverwaltungsparameter werden nicht repliziert; stattdessen werden preisbasierte Risikokontrollen (`StopLoss`, `TakeProfit`, `TrailingStop`, Break-Even) bereitgestellt.

## Empfohlene Erweiterungen
- Fügen Sie portfolioweite Risikokontrollen hinzu, wenn mehrere Symbole gleichzeitig gehandelt werden.
- Kombinieren Sie mit Sitzungs- oder Volatilitätsfiltern, um den Handel während illiquider Perioden zu deaktivieren.
- Erwägen Sie, Fills an externe Analysen weiterzuleiten (z. B. für Eigenkapital-Tracking), wenn solche Funktionalität erforderlich ist.
