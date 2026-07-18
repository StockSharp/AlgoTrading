# Trailing Stop und Take Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Trailing Stop und Take Strategie** ist eine direkte StockSharp-Anpassung des MetaTrader-Expertenberaters aus `MQL/19963`. Sie konzentriert sich auf aktives Trade-Management: Sobald eine Position offen ist, fügt die Strategie anfängliche Stop-Loss- und Take-Profit-Niveaus an und zieht dann beide Niveaus nach, wenn sich der Preis bewegt. Trailing-Anpassungen respektieren konfigurierbare Mindestschrittgrößen, Breakeven-Schutz und die Option, das Trailing zu vermeiden, solange ein Trade noch verlustreich ist.

Die Strategie operiert auf einem einzigen Wertpapier mit fertigen Kerzen. Wenn die Strategie flat ist, öffnet sie eine Position in Richtung des jüngsten Kerzenkörpers (bullische Schlüsse führen zu Longs, bärische Schlüsse führen zu Shorts). Dies spiegelt das ursprüngliche Testverhalten des MQL-Skripts wider und bietet einen kontinuierlichen Fluss von Positionen für den Trailing-Engine.

## Funktionsweise
1. Abonnement des konfigurierten Kerzentyps und Verarbeitung nur fertiger Kerzen.
2. Wenn keine Position offen ist, auf bullischen Kerzen long oder auf bärischen Kerzen short einsteigen (unter Berücksichtigung des Positionstyp-Filters).
3. Bei einer neuen Position, Stop-Loss- und Take-Profit-Abstände mit `InitialStopLossPoints`/`InitialTakeProfitPoints` initialisieren. Wenn diese null sind, werden stattdessen die Trailing-Abstände verwendet.
4. Bei jedem Kerzenschluss aktualisierte Trailing-Ziele berechnen:
   - Stops bewegen sich erst näher zum Preis, nachdem der Markt um den Trailing-Schritt vorgerückt ist.
   - Take-Profits rücken näher, wenn der Preis um mindestens den Trailing-Schritt zurückgeht.
   - Breakeven-Schutz verhindert das Verschieben von Niveaus in eine Verlustzone wenn `AllowTrailingLoss` deaktiviert ist.
5. Wenn der Preis einen Trailing Stop oder Take-Profit-Level kreuzt, mit Marktorder aussteigen und alle gespeicherten Niveaus zurücksetzen.

## Trailing-Logik
### Long-Positionen
- Anfänglicher Stop ist mindestens `SpreadMultiplier * PriceStep` vom Einstieg entfernt.
- Anfänglicher Take-Profit ist mindestens denselben Mindestabstand über dem Einstieg positioniert.
- Trailing Stop folgt dem Schlusskurs nach unten um `TrailingStopLossPoints`, unter Berücksichtigung des Trailing-Schritts und des optionalen Breakeven-Filters.
- Trailing Take-Profit zieht sich zusammen, wenn der Preis zurückgeht, und bewegt sich nicht unter das Breakeven-Niveau, wenn das Trailing von Verlusten nicht erlaubt ist.

### Short-Positionen
- Anfänglicher Stop ist oberhalb des Einstiegs gesetzt, nicht näher als der Spread-Multiplikator-Abstand.
- Anfänglicher Take-Profit beginnt unterhalb des Einstiegs mit derselben Mindestabstandsregel.
- Trailing Stop fällt, wenn der Preis fällt, bewegt sich aber nicht höher als Breakeven, es sei denn, Verlust-Trailing ist erlaubt.
- Trailing Take-Profit steigt bei Rücksetzern in Richtung Preis und wird auf Breakeven begrenzt wenn nötig.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Für die Preisauswertung verwendete Kerzen-Aggregation. |
| `Volume` | Standard-Ordervolumen für Ein- und Ausstiege. |
| `PositionType` | Schränkt den Engine auf Long-Positionen, Short-Positionen oder beide ein. |
| `InitialStopLossPoints` | Anfängliche Stop-Loss-Größe in Preispunkten (verwendet Trailing-Abstand wenn null). |
| `InitialTakeProfitPoints` | Anfängliche Take-Profit-Größe in Preispunkten (verwendet Trailing-Abstand wenn null). |
| `TrailingStopLossPoints` | Abstand zwischen Preis und Trailing Stop. |
| `TrailingTakeProfitPoints` | Abstand zwischen Preis und Trailing Take-Profit. |
| `TrailingStepPoints` | Mindestbewegung in Punkten, die vor der Anpassung von Stops oder Zielen erforderlich ist. |
| `AllowTrailingLoss` | Aktiviert das Trailing, während der Trade noch unter Breakeven liegt. |
| `BreakevenPoints` | Offset in Punkten, der zum Einstiegspreis addiert wird, um die Breakeven-Barriere zu bilden. |
| `SpreadMultiplier` | Multiplikator für die Mindest-Stop-Distanz-Approximation (simuliert das MQL `StopLevel`). |

## Hinweise
- Stops und Ziele werden mit Marktorders ausgeführt, wenn sie ausgelöst werden, was die Implementierung einfach hält und die ursprünglichen Stop-Modifikationen widerspiegelt.
- `SpreadMultiplier` approximiert das MQL-Verhalten, bei dem Stop-Niveaus nicht näher als der aktuelle Spread platziert werden können. Diesen Wert anpassen, um dem Ausführungsort zu entsprechen.
- Die Strategie vermeidet bewusst eine Python-Version und konzentriert sich ausschließlich auf die C#-Implementierung, wie gewünscht.
- Erwägen Sie, den Trailing-Engine mit Ihrem eigenen Einstiegsfilter zu kombinieren, indem Sie die integrierten Einstiege deaktivieren und bei Bedarf externe Orders injizieren.
