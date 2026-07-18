# Meine Systemstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **My System Strategy** ist ein StockSharp-Port des MetaTrader 4 Expert Advisors `MySystem.mq4` (Verzeichnis `MQL/9601`). Das ursprüngliche Skript wertet die Bulls Power- und Bears Power-Indikatoren aus, kombiniert ihre Werte zu einem zusammengesetzten Momentum-Signal und eröffnet Positionen im Umkehrstil, wenn das Momentum das Vorzeichen umkehrt. Diese C#-Version reproduziert den Kernentscheidungsprozess, fügt einen expliziten Risikomanagementstatus hinzu und stellt jede einstellbare Konstante über Strategieparameter zur Optimierung bereit.

Im Gegensatz zur MQL-Implementierung, die `iBullsPower`/`iBearsPower` direkt mit unterschiedlichen angewendeten Preisen für jeden Balken abfragte, speist die StockSharp-Edition beide Indikatoren aus der konfigurierten Kerzenreihe und verfolgt den vorherigen zusammengesetzten Wert intern. Die Übersetzung behält den standardmäßigen 15-Minuten-Zeitrahmen, die gleichen Take-Profit-/Stop-Loss-Abstände und die im Quellcode angegebenen Trailing-Exit-Bedingungen bei.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzenstream (standardmäßig 15-Minuten-Kerzen) und warten Sie auf vollständig fertige Kerzen.
2. Rufen Sie für jede abgeschlossene Kerze die neuesten Bulls Power- und Bears Power-Werte ab und berechnen Sie deren Durchschnitt `((bulls + bears) / 2)`.
3. Behalten Sie den vorherigen Durchschnitt in `_previousAveragePower` bei, um die schichtbasierten Anrufe in MQL widerzuspiegeln.
4. Einstiegsregeln (nur wenn keine Position offen ist):
   - **Short-Einstieg** – wenn der vorherige Durchschnitt größer als der aktuelle Durchschnitt ist und der aktuelle Durchschnitt positiv bleibt. Dies entspricht der MQL-Bedingung `pos1pre > pos2cur && pos2cur > 0`.
   - **Langer Einstieg** – wenn der aktuelle Durchschnitt negativ wird (`pos2cur < 0`), was bedeutet, dass Bears Power dominiert.
5. Das Exit-Management wird bei jeder Kerze ausgeführt, noch bevor neue Signale eintreten:
   - Bewerten Sie die harten Take-Profit- und Stop-Loss-Werte, die bei der Eröffnung der Position aufgezeichnet wurden.
   - Wenden Sie die Trailing-Stop-Logik aus der Quelle EA an: Bei Long-Positionen steigen Sie aus, wenn die Dynamik nachlässt (`pos1pre > pos2cur`) und der Preis um die Trailing-Distanz gestiegen ist. Bei Short-Positionen wird es nachlassen, wenn das zusammengesetzte Momentum negativ wird und sich der Preis um den gewünschten Abstand zu seinen Gunsten bewegt hat.
6. Wenn ein Ausgangssignal ausgelöst wird, rufen Sie `ClosePosition()` auf, um es abzuflachen. Die Strategie wartet dann auf die nächste Kerze, um neue Einträge auszuwerten.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `TakeProfitPoints` | Abstand zum Take-Profit-Niveau, ausgedrückt in Preisschritten. | `86` | Spiegelt die Eingabe `TakeProfit`. Auf `0` setzen, um das Gewinnziel zu deaktivieren. |
| `StopLossPoints` | Abstand zum Stop-Loss-Level, ausgedrückt in Preisschritten. | `60` | Spiegelt die Eingabe `StopLoss`. Auf `0` einstellen, um den Schutzstopp zu deaktivieren. |
| `TrailingStopPoints` | Von der abschließenden Exit-Bedingung verwendete Entfernung (Preisschritte). | `10` | Bei Null wird die nachgestellte Logik umgangen. |
| `OrderVolume` | Bei jedem neuen Eintrag übermitteltes Volumen. | `8.3` | Entspricht dem Parameter `Lots` im EA. |
| `PowerPeriod` | Der Zeitraum gilt sowohl für Bulls Power- als auch Bears Power-Indikatoren. | `13` | Reproduziert den ursprünglichen Zeitraum. |
| `CandleType` | Kerzenserie, die die Indikatorberechnungen steuert. | `15m` | Wechseln Sie, um die Strategie auf einen anderen Zeitrahmen zu übertragen. |

Alle Parameter werden über `Param()` deklariert, um UI-Bindung und Optimierungs-Sweeps zu unterstützen.

## Risikomanagement
- Schutzstufen werden gespeichert, wenn `OnPositionChanged` eine neue lange oder kurze Exposition erkennt. Die Entfernungen werden mithilfe eines Pip-Size-Helfers, der sich der `Point`-Logik von MetaTrader annähert (`PriceStep`, angepasst an 3/5 Dezimal-FX-Symbole), in absolute Preise umgewandelt.
- `ClosePosition()` wird aufgerufen, sobald eine Take-Profit-, Stop-Loss- oder Trailing-Bedingung erfüllt ist, um sicherzustellen, dass die Strategie mit einer einzigen Marktorder beendet wird und doppelte Abschlussanfragen vermieden werden.
- Es werden keine Absicherungen oder Teilschließungen vorgenommen; Die Strategie erzwingt jeweils eine einzelne Position, genau wie der `OrdersTotal() < 1`-Schutz im MQL-Skript.

## Konvertierungshinweise
- Die Argumente `PRICE_WEIGHTED` vs. `PRICE_CLOSE` von MetaTrader wurden angenähert, indem der vorherige zusammengesetzte Wert (`pos1pre`) gespeichert wurde, anstatt zwei Indikatorinstanzen mit unterschiedlichen Preis-Feeds zu verwalten. Dadurch bleibt die Verhaltensabsicht erhalten, ohne dass Kerzentransformationen dupliziert werden müssen.
- Der ursprüngliche EA enthielt mehrere fehlerhafte `OrderSelect`-Aufrufe innerhalb der abschließenden Logik. Der Port setzt den beabsichtigten Effekt – das Schließen von Geschäften, sobald der Preis die nachlaufende Distanz zurücklegt und die Momentum-Bedingung erfüllt ist – auf deterministische Weise um.
- Nachlaufende Ausstiege werden anhand der Kerzenhochs/-tiefs bewertet, um Intrabar-Berührungen zu emulieren, da StockSharp standardmäßig abgeschlossene Kerzen verarbeitet.
- Ordergröße, Stoppdistanzen und Indikatorperioden behalten die ursprünglichen Standardwerte bei, sodass vorhandene Optimierungen ohne Anpassungen wiederholt werden können.

## Nutzungstipps
1. Hängen Sie die Strategie an ein Wertpapier an, das `PriceStep` und `Decimals` verfügbar macht. Fehlen diese, greift der Helfer auf eine Pip-Größe von `1` zurück.
2. Passen Sie `OrderVolume`, `TakeProfitPoints` und `StopLossPoints` an, um sie an die Kontraktgröße und den Tick-Wert des Instruments anzupassen.
3. Denken Sie beim Testen in verschiedenen Zeitrahmen daran, `CandleType` zu aktualisieren und erwägen Sie eine erneute Optimierung des Nachlaufabstands, da kürzere Balken den Schwellenwert häufiger erreichen.
4. Verwenden Sie StockSharp-Diagramme (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`), um zu überprüfen, ob Einträge erfolgen, wenn Bulls and Bears Power die angegebenen Schwellenwerte überschreiten.

## Dateien
- `CS/MySystemStrategy.cs` – Strategieumsetzung mit StockSharps High-Level-API.
- `README.md`, `README_zh.md`, `README_ru.md` – mehrsprachige Dokumentation für den konvertierten Expert Advisor.
