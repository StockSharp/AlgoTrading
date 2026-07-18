# Secwenta MultiBar Signals-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters "Secwenta" (MQL-ID 22977). Der Algorithmus scannt abgeschlossene Kerzen und zählt, wie viele davon bullisch (Schluss > Eröffnung) oder bärisch (Schluss < Eröffnung) innerhalb eines kurzen rollenden Verlaufs geschlossen haben. Je nach Konfiguration kann er im Nur-Kauf-, Nur-Verkauf- oder bidirektionalen Umkehrmodus betrieben werden. Wenn die erforderliche Anzahl bullischer oder bärischer Balken erscheint, öffnet oder schließt die Strategie Marktpositionen mit einem festen Volumen, das die ursprüngliche Lot-Einstellung widerspiegelt.

## Signalauswertung
- Nur abgeschlossene Kerzen des ausgewählten `CandleType` werden über die High-Level-Abonnement-API verarbeitet.
- Für jede Kerze erfasst die Strategie, ob sie bullisch, bärisch oder neutral (Doji) war. Der interne Puffer hält die letzten *N* Richtungen, wobei *N* der größere Wert von `BullishBarCount` und `BearishBarCount` unter den aktivierten Seiten (Kauf und/oder Verkauf) ist.
- Der bullische Zähler erhöht sich, wenn eine Kerze über ihrer Eröffnung schließt, während der bärische Zähler bei Schlüssen unter der Eröffnung erhöht wird. Neutrale Kerzen beeinflussen die Zähler nicht.
- Ein Signal wird ausgelöst, wenn der entsprechende Zähler seinen konfigurierten Schwellenwert innerhalb des rollenden Fensters erreicht. Dies reproduziert die ursprüngliche MQL-Logik, die durch die jüngsten Balken iterierte, bis die angeforderte Anzahl bullischer oder bärischer Kerzen gefunden wurde.

## Handelsregeln
1. **Nur-Kauf-Modus (`UseBuySignals = true`, `UseSellSignals = false`):**
   - Wenn der bärische Zähler `BearishBarCount` erreicht, wird jede bestehende Long-Position mit einer Market-Sell-Order geschlossen.
   - Wenn der bullische Zähler `BullishBarCount` erreicht und die Strategie flat ist, wird eine neue Long-Position mit `OrderVolume` eröffnet.
2. **Nur-Verkauf-Modus (`UseBuySignals = false`, `UseSellSignals = true`):**
   - Wenn der bullische Zähler `BullishBarCount` erreicht, wird eine offene Short-Position mit einer Market-Buy-Order gedeckt.
   - Wenn der bärische Zähler `BearishBarCount` erreicht und die Strategie flat ist, wird eine neue Short-Position mit `OrderVolume` eröffnet.
3. **Umkehrmodus (`UseBuySignals = true` und `UseSellSignals = true`):**
   - Ein bullischer Auslöser schließt jedes Short-Engagement und öffnet, falls die Strategie nicht bereits long ist, eine neue Long-Position durch den Kauf von `OrderVolume` plus dem absoluten Betrag der Short-Position. Dies ahmt die ursprüngliche Sequenz des Schließens von Verkäufen vor dem Öffnen von Käufen nach.
   - Ein bärischer Auslöser schließt jedes Long-Engagement und öffnet, falls die Strategie nicht bereits short ist, eine neue Short-Position durch den Verkauf von `OrderVolume` plus dem absoluten Betrag der Long-Position.

Alle Marktoperationen verwenden StockSharp's `BuyMarket`- und `SellMarket`-Helfer, und die Strategie ruft `StartProtection()` auf, damit bei Bedarf kontoseitige Schutzmaßnahmen darüber gelegt werden können.

## Parameter
| Parameter | Beschreibung | Standard | Hinweise |
|-----------|-------------|---------|-------|
| `CandleType` | Kerzendatentyp (Zeitrahmen) zur Auswertung von Sequenzen. | 1-Stunden-Zeitrahmen | Jeder von StockSharp unterstützte Kerzentyp kann ausgewählt werden. |
| `OrderVolume` | Basis-Market-Order-Volumen, das die MQL-Lot-Größe widerspiegelt. | 1 | Wird beim Umkehren einer Position zum Schließvolumen addiert. |
| `UseBuySignals` | Aktiviert die Verarbeitung bullischer Signale. | `true` | Wenn deaktiviert, werden keine neuen Long-Trades eröffnet. |
| `BullishBarCount` | Anzahl bullischer Kerzen, die zur Auslösung eines bullischen Ereignisses erforderlich sind. | 2 | Sollte mit dem Schließschwellenwert konsistent bleiben, wenn im Nur-Kauf-Modus betrieben wird. |
| `UseSellSignals` | Aktiviert die Verarbeitung bärischer Signale. | `false` | Wenn deaktiviert, werden keine neuen Short-Trades eröffnet. |
| `BearishBarCount` | Anzahl bärischer Kerzen, die zur Auslösung eines bärischen Ereignisses erforderlich sind. | 1 | Dient sowohl als Öffnungsschwellenwert für Shorts als auch als Ausstiegsschwellenwert für Longs. |

## Implementierungshinweise
- Das rollende Fenster verwendet eine Warteschlange, um die letzten Kerzenrichtungen zu halten, und stellt sicher, dass die Zähler auch nach Parameteränderungen der Fenstergröße entsprechen.
- Nur abgeschlossene Kerzen werden verarbeitet, um dem ursprünglichen "neue Kerze"-Ereignis-Handling treu zu bleiben.
- Neutrale (Doji-)Kerzen lassen die Zähler unverändert, genau wie im MQL-Code.
- Umkehrungen werden mit einer einzigen Market-Order ausgeführt, die das Schließ- und Eröffnungsvolumen kombiniert und deterministische Expositionsänderungen beibehält.
- Die Pufferlänge entspricht dem größten aktiven Schwellenwert; wenn eine Seite deaktiviert ist, trägt nur der entsprechende Schwellenwert zur Lookback-Länge bei und entspricht damit dem Verhalten von `CopyRates` in der MQL-Version.

## Dateien
- `CS/SecwentaMultiBarSignalsStrategy.cs` – Haupt-C#-Implementierung basierend auf der StockSharp High-Level-Strategie-API.

> **Hinweis:** Für diese ID wird keine Python-Übersetzung bereitgestellt; nur die angeforderte C#-Version ist vorhanden.
