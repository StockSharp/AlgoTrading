# Treppenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Stairs-Strategie** reproduziert das Verhalten des ursprünglichen MetaTrader-Experten. Es beginnt mit der Platzierung symmetrischer Stop-Orders rund um den aktuellen Briefkurs und baut dann das Raster kontinuierlich um die letzte Ausführung herum neu auf. Gewinne werden in Preisschritten (Pips) ohne Gewichtung nach Volumen akkumuliert, genau wie im Quellskript. Wenn ein Gewinnziel erreicht wird, liquidiert die Strategie alle Positionen gemäß der Marktorder, entfernt alle ausstehenden Stopps und setzt das Raster zurück.

## Handelslogik

1. Wenn keine Positionen offen sind, platzieren Sie einen Kaufstopp und einen Verkaufsstopp in einem Abstand von `ChannelSteps / 2` Preisschritten über und unter dem aktuellen Briefkurs.
2. Nachdem die erste Stop-Order ausgeführt wurde, stellen Sie das Raster wieder auf den ausgeführten Preis ein:
   - Wenn weniger als zwei aktive Stop-Orders vorhanden sind, stornieren Sie die veralteten.
   - Solange der aktuelle Gebotspreis innerhalb der Hälfte der Kanalentfernung vom letzten Eintrag bleibt, platzieren Sie einen neuen Kaufstopp und Verkaufsstopp `ChannelSteps` entfernt von der letzten Füllung.
   - Wenn `AddLots` aktiviert ist, erhöhen Sie nach jeder Ausführung das Volumen der ausstehenden Aufträge um das Basislos.
3. Führen Sie zwei laufende Listen mit allen Long- und Short-Einträgen, um den von der MT4-Version verwendeten Hedged Basket zu reproduzieren.
4. Berechnen Sie den nicht realisierten Gewinn des Korbs für jede fertige Kerze unter Verwendung des besten Gebots für Long-Positionen und des besten Briefs für Short-Positionen. Entfernungen werden durch die Preisstufe des Instruments normalisiert und spiegeln die ursprüngliche Punktberechnung wider.
5. Lösen Sie eine vollständige Liquidation aus, wenn einer der Schwellenwerte überschritten wird:
   - `ProfitSteps` – Gewinn, der nur durch das aktuelle Symbol erzeugt wird.
   - `CommonProfitSteps` – Gewinn im gesamten Warenkorb.
6. Die Liquidation sendet Marktaufträge, um jedes Long- und Short-Engagement separat zu schließen. Ausstehende Stop-Orders werden storniert, sobald der Korb flach ist.

> **Hinweis**: Der ursprüngliche Experte hat bei der Registrierung ausstehender Aufträge Stop-Loss-Levels angegeben. StockSharp unterstützt keine pro-Order-Schutzebenen durch die übergeordnete Ebene API, daher schließt der Port Geschäfte ausschließlich über die oben beschriebene gewinnbasierte Logik ab.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `ChannelSteps` | Abstand (in Mindestpreisschritten) zwischen den symmetrischen Stop-Orders. | `1000` |
| `ProfitSteps` | Gewinnschwelle (in Schritten), die zum Schließen des lokalen Warenkorbs erforderlich ist. | `1500` |
| `CommonProfitSteps` | Globale Gewinnschwelle (in Schritten), die eine vollständige Liquidation erzwingt. | `1000` |
| `AddLots` | Wenn diese Option aktiviert ist, erhöhen Sie nach jeder Ausführung das nächste ausstehende Auftragsvolumen um das Basislos. | `true` |
| `BaseVolume` | Das für das allererste Stop-Order-Paar verwendete Volumen. | `0.1m` |
| `CandleType` | Zeitrahmen für Kerzenabonnements und Handelsmanagement. | `1 minute` |

## Hinweise zur Implementierung

- Verwendet die StockSharp-Hochebene API mit `SubscribeCandles()` und `Bind()`, um nur fertige Kerzen zu verarbeiten.
- Verfolgt einzelne Einträge innerhalb von `OnOwnTradeReceived`, sodass die Gewinnberechnung die Absicherungslogik der MQL-Version nachahmen kann.
- Gewinnschwellen basieren auf reinen Preisschrittabständen, ohne Multiplikation mit dem ausgeführten Volumen, und entsprechen der Art und Weise, wie der MT4-Experte die Pips summiert hat.
- Alle Stop-Orders werden über `BuyStop` und `SellStop` erstellt, während Exits mit Market-Orders ausgeführt werden, um die Logik über Datenanbieter hinweg portierbar zu halten.
