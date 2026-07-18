# Gazonkos Expertenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „gazonkos expert“, der für den EUR/USD-H1-Chart entwickelt wurde. Der EA wartet auf eine starke einstündige Momentumbewegung und bewegt sich dann nach einem konfigurierbaren Pullback in die Richtung dieser Bewegung. Schützende Stop-Loss- und Take-Profit-Level werden als feste Distanzen, gemessen in Pips, angewendet.

## Ursprüngliche MQL4-Logik
- Der EA überwacht kontinuierlich die Differenz zwischen zwei historischen Schlusskursen (`Close[t2] - Close[t1]`). Die Standardwerte sind `t1 = 3` und `t2 = 2`, die den Schlusskursen der Kerzen entsprechen, die vor zwei und drei Stunden endeten.
- Ein bullischer Impuls wird erkannt, wenn `Close[t2] - Close[t1]` `delta` Punkte überschreitet. Ein rückläufiger Impuls wird erkannt, wenn `Close[t1] - Close[t2]` denselben Schwellenwert überschreitet.
- Sobald ein Impuls erkannt wird, zeichnet EA das höchste (für bullische) oder niedrigste (für bärische) Gebot auf, das vor Beginn der nächsten Stunde erfolgt. Wenn der Preis innerhalb derselben Stunde um `Otkat` Punkte von diesem Extremwert zurückgeht, wird eine Marktorder in Impulsrichtung gesendet.
- Trades werden blockiert, wenn bereits eine offene Position mit derselben magischen Zahl besteht oder wenn in der aktuellen Stunde bereits ein Trade eröffnet wurde.
- Jede Order wird mit einem festen Take-Profit- (`TakeProfit`) und Stop-Loss-Abstand (`StopLoss`) gesendet, ausgedrückt in Punkten.

## Zustandsmaschine in der C#-Version
Die StockSharp-Implementierung erstellt die ursprüngliche Zustandsmaschine neu:
1. **WaitingForSlot** – überprüft, ob in der aktuellen Stunde kein aktueller Trade eröffnet wurde und dass die konfigurierte maximale Anzahl gleichzeitiger Trades nicht erreicht wurde.
2. **WaitingForImpulse** – überprüft die historischen Schlusskurse, um bullische oder bärische Impulse zu erkennen.
3. **MonitoringRetracement** – verfolgt die Kerzenhochs/-tiefs nach dem Impuls und wartet auf einen Pullback von `RetracementPips` (der frühere `Otkat`-Parameter) innerhalb derselben Stunde.
4. **AwaitingExecution** – sendet einen Marktauftrag in Impulsrichtung und wendet sofort schützende Stop-Loss- und Take-Profit-Level an, die aus dem Instrument `PriceStep` berechnet werden.

Die Strategie verarbeitet nur fertige Kerzen aus dem konfigurierten Zeitrahmen und ignoriert unvollendete Daten. Sie spiegelt wider, wie die ursprüngliche EA die Bedingungen für geschlossene stündliche Balken ausgewertet hat.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPips` | Abstand zwischen dem Einstiegspreis und dem Take-Profit-Level. |
| `RetracementPips` | Erforderlicher Rückzug vom Impulsextrem vor dem Einstieg. |
| `StopLossPips` | Abstand zwischen dem Einstiegspreis und dem Schutzstopp. |
| `T1Shift` | Index des älteren Referenzschlusses, der für die Impulserkennung verwendet wird (Standard 3). |
| `T2Shift` | Index des neueren Referenzschlusses, der für die Impulserkennung verwendet wird (Standard 2). |
| `DeltaPips` | Minimaler Impulsabstand, der die beiden Referenzschlüsse trennen muss. |
| `LotSize` | Festes Volumen jeder Bestellung. |
| `MaxActiveTrades` | Maximale Anzahl gleichzeitiger Trades; Werte über eins erfordern, dass das Brokerkonto additive Nettopositionen unterstützt. |
| `CandleType` | Zeitrahmen der Kerzen, die zur Bewertung der Handelsregeln verwendet werden (Standard ist 1 Stunde). |

Alle Pip-basierten Distanzen werden mit `Security.PriceStep` in Preisoffsets umgewandelt. Wenn das Instrument keine Preisschrittinformationen hat, wird ein Standardwert von 0,0001 verwendet, der der ursprünglichen EUR/USD-Konfiguration entspricht.

## Hinweise zur Implementierung
- Die Strategie funktioniert mit dem High-Level-Kerzenabonnement API (`SubscribeCandles().Bind`) von StockSharp.
- Geschlossene Preise werden in einem kompakten Rollpuffer zwischengespeichert, um `Close[i]`-Suchvorgänge aus der MQL4-Version zu emulieren.
- Nachdem ein Handel ausgeführt wurde, zeichnet die Strategie die Kerzenstunde auf und blockiert neue Einträge bis zur nächsten Stunde, wodurch die ursprüngliche `LastTradeTime`-Absicherung reproduziert wird.
- `MaxActiveTrades` wird gegen die aktuelle Nettoposition interpretiert. Bei Netting-Konten wird dadurch das System effektiv auf einen einzelnen offenen Handel beschränkt, was dem Standardverhalten des MQL4-Experten entspricht.
- Kommentare im Code beschreiben die C#-Zustandsmaschine zur einfacheren Wartung auf Englisch.
