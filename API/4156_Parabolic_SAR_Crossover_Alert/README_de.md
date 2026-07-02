# Parabolic SAR Benachrichtigungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist der StockSharp-Port des MetaTrader 4-Expertenberaters `pSAR_alert.mq4`. Das ursprüngliche Skript gab nur dann einen Warnton aus, wenn der Indikator Parabolic SAR von einer Seite des Preises auf die andere wechselte. Die Konvertierung behält die gleiche Entscheidungslogik bei, wandelt die Warnungen jedoch in tatsächliche Marktaufträge um, sodass das Signal automatisch in StockSharp gehandelt werden kann.

## Handelslogik
- Die Strategie abonniert den konfigurierten Kerzentyp und führt standardmäßig einen Parabolic SAR-Indikator mit dem klassischen Beschleunigungsfaktor (0,02) und der maximalen Beschleunigung (0,2) aus.
- Für jede abgeschlossene Kerze vergleicht die Strategie den Parabolic SAR-Wert mit dem Kerzenschluss und verfolgt auch den vorherigen Kerzenkontext.
- Wenn die vorherige Kerze unter SAR schloss, der aktuelle Schluss jedoch darüber liegt, ist der Indikator nach unten gekippt und eine Long-Position wird eröffnet (oder eine bestehende Short-Position wird umgekehrt).
- Wenn die vorherige Kerze über SAR schloss, der aktuelle Schluss jedoch darunter liegt, ist der Indikator nach oben gekippt und eine Short-Position wird eröffnet (oder eine bestehende Long-Position wird umgekehrt).
- Das Handelsvolumen wird als Basisstrategievolumen plus der absoluten aktuellen Position berechnet, um sicherzustellen, dass Umkehrungen den vorherigen Handel vollständig verlassen, bevor sie in die neue Richtung eintreten.
- `StartProtection()` wird beim Start ausgeführt, sodass StockSharp automatisch unerwartete Verbindungsabbrüche verwaltet, während Positionen offen sind.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `AccelerationFactor` | 0,02 | Anfänglicher Beschleunigungsschritt, der steuert, wie schnell der Parabolic SAR Preisbewegungen folgt. |
| `MaxAccelerationFactor` | 0,2 | Obergrenze für den Beschleunigungsschritt, die begrenzt, wie aggressiv der SAR bei starken Trends beschleunigt. |
| `CandleType` | Zeitrahmen von 5 Minuten | Marktdatentyp, der für Indikatoraktualisierungen verwendet wird; Ändern Sie es, um zwischen Zeitrahmen oder anderen Kerzendarstellungen zu wechseln. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht, sodass sie direkt im StockSharp-Designer optimiert werden können.

## Indikator-Workflow
1. Abonnieren Sie den konfigurierten Kerzenstream über `SubscribeCandles`.
2. Binden Sie den Stream an einen `ParabolicSar`-Indikator, damit StockSharp ihn automatisch aktualisiert.
3. Vergleichen Sie innerhalb des Bindungsrückrufs den aktuellen SAR-Wert mit dem Schlusskurs und behalten Sie das vorherige SAR/Schlusspaar bei.
4. Erkennen Sie Überschneidungen, indem Sie auswerten, ob sich der SAR von oben nach unter dem Schlusskurs (bullischer Flip) oder von unten nach oben (bärischer Flip) bewegt hat.
5. Führen Sie `BuyMarket` oder `SellMarket` entsprechend aus und protokollieren Sie beschreibende Nachrichten für jeden Trade.

## Praktische Hinweise
- Da die Strategie nur auf bestätigte Kerzenschließungen reagiert, vermeidet sie vorzeitige Signale, die möglicherweise verschwinden, bevor der Balken endet.
- Die Standardparameter reproduzieren das Verhalten des Skripts MQL, Sie können sie jedoch anpassen, um die Empfindlichkeit des Skripts Parabolic SAR anzupassen.
- Hängen Sie die Strategie an Instrumente an, die eindeutig im Trend liegen. Die SAR-Flip-Logik funktioniert am besten, wenn Umkehrungen entscheidend und nicht laut sind.
- Die Diagrammvisualisierung wird automatisch aktiviert, wenn ein Diagrammbereich verfügbar ist: Kerzen, der Indikator Parabolic SAR und eigene Trades werden zur schnellen Überprüfung gezeichnet.

## Dateien
- `CS/ParabolicSarCrossoverAlertStrategy.cs` – C#-Implementierung der Strategie.
- `README.md` – Diese Dokumentation auf Englisch.
- `README_zh.md` – Chinesische Übersetzung der Dokumentation.
- `README_ru.md` – Russische Übersetzung der Dokumentation.
