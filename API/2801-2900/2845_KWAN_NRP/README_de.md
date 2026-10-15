# Exp KWAN NRP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Exp KWAN NRP-Strategie reproduziert den ursprünglichen MetaTrader Expert Advisor, indem ein stochastischer Oszillator, ein relativer Stärkeindex und ein Momentum-Indikator zu einem einzigen Verhältnis kombiniert werden. Das Verhältnis wird mit einem konfigurierbaren gleitenden Durchschnitt geglättet, und die Steigung der geglätteten Linie bestimmt, wann Positionen geöffnet oder geschlossen werden. Der Ansatz funktioniert bei jedem Symbol oder Zeitrahmen und ist für den direktionalen Handel konzipiert, wenn sich das Momentum verschiebt.

## Handelslogik
1. Das KWAN-Verhältnis berechnen, indem die stochastische %D-Linie mit dem RSI-Wert multipliziert und durch die Momentum-Lesart dividiert wird.
2. Das Verhältnis mit der ausgewählten Methode des gleitenden Durchschnitts glätten (einfach, exponentiell, geglättet oder gewichtet).
3. Die Steigung der geglätteten Linie beim konfigurierbaren Signalbalken-Offset auswerten.
4. Long-Positionen eröffnen, wenn die Linie nach oben dreht, und Short-Positionen schließen. Short-Positionen eröffnen, wenn die Linie nach unten dreht, und Long-Positionen schließen.
5. Optionaler Stop-Loss- und Take-Profit-Schutz kann Positionen automatisch nach einer vordefinierten Preisbewegung in Preisschritten schließen.

## Signale
- **Long-Einstieg**: Der geglättete KWAN-Wert am Signalbalken steigt gegenüber dem vorherigen Balken und Long-Einstiege sind aktiviert.
- **Long-Ausstieg**: Der geglättete KWAN-Wert dreht nach unten, während eine Long-Position offen ist und Long-Ausstiege sind aktiviert.
- **Short-Einstieg**: Der geglättete KWAN-Wert am Signalbalken fällt gegenüber dem vorherigen Balken und Short-Einstiege sind aktiviert.
- **Short-Ausstieg**: Der geglättete KWAN-Wert dreht nach oben, während eine Short-Position offen ist und Short-Ausstiege sind aktiviert.

## Risikomanagement
- Setzen Sie die `Volume`-Eigenschaft der Strategie, um die Basis-Ordergröße zu kontrollieren. Positionsumkehrungen schließen automatisch eine entgegengesetzte Position, bevor eine neue geöffnet wird.
- Aktivieren Sie `UseProtection`, um Stop-Loss- und Take-Profit-Niveaus anzuwenden, die in Instrument-Preisschritten gemessen werden. Beide Schutzmaßnahmen können zusammen oder separat verwendet werden.
- Die Strategie abonniert durch `CandleType` definierte Kerzen und handelt beim Abschluss fertiger Kerzen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zeitrahmen für Indikatorberechnungen und Signalauswertung. | 1-Stunden-Kerzen |
| `KPeriod` | Periode der stochastischen %K-Linie. | 5 |
| `DPeriod` | Periode der stochastischen %D-Linie. | 3 |
| `SlowingPeriod` | Zusätzliche Glättung der stochastischen %K-Linie. | 3 |
| `RsiPeriod` | Periode des relativen Stärkeindex. | 14 |
| `MomentumPeriod` | Periode des Momentum-Indikators. | 14 |
| `SmoothingMethod` | Gleitender Durchschnitt-Typ für das KWAN-Verhältnis (Simple, Exponential, Smoothed, Weighted). | Simple |
| `SmoothingLength` | Länge des glättenden gleitenden Durchschnitts. | 3 |
| `SignalBar` | Anzahl der Balken zurück zur Steigungsauswertung (0 = aktueller geschlossener Balken). | 1 |
| `EnableBuyEntries` | Long-Positionen bei bullischen Signalen erlauben. | true |
| `EnableSellEntries` | Short-Positionen bei bärischen Signalen erlauben. | true |
| `EnableBuyExits` | Long-Positionen schließen, wenn ein bärisches Signal erscheint. | true |
| `EnableSellExits` | Short-Positionen schließen, wenn ein bullisches Signal erscheint. | true |
| `UseProtection` | Stop-Loss- und Take-Profit-Schutz aktivieren. | true |
| `StopLossSteps` | Stop-Loss-Abstand in Preisschritten. | 1000 |
| `TakeProfitSteps` | Take-Profit-Abstand in Preisschritten. | 2000 |

## Verwendungshinweise
- Das KWAN-Verhältnis kann instabil werden, wenn der Momentum-Indikator null ist. Die Strategie überspringt automatisch Signale für diese Balken, um eine Division durch null zu vermeiden.
- Der Parameter `SignalBar` ermöglicht die Ausrichtung von Signalen mit historischen Balken, wenn eine verzögerte Bestätigung benötigt wird.
- Kombinieren Sie bei Bedarf mit Risikokontrollen auf Broker-Ebene oder zusätzlichen Filtern für den Produktionshandel.
