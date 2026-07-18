# Intelligente AC-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Smart AC Trader passt die ursprüngliche „Smart AC Trader“-Idee von MetaTrader an das hohe Niveau von StockSharp an: API. Der MQL-Experte bewertete die relative Stärke der Währungen innerhalb eines Paares und reagierte, wenn die Basiswährung die Notierungswährung übertraf. In StockSharp konzentrieren wir uns auf das gleiche impulsgesteuerte Verhalten, operieren jedoch mit einem einzigen Instrument, mit dem die Strategie verknüpft ist. Die Stärke wird durch eine Kombination aus exponentiellen gleitenden Durchschnitten (EMAs) und dem Indikator für die Änderungsrate (ROC) angenähert:

- Ein schneller EMA misst die kurzfristige Trendrichtung.
- Ein langsamer EMA stellt den Haupttrend dar.
- ROC bestätigt, dass die Preisdynamik mit dem Trend übereinstimmt, bevor Einträge zulässig sind.

Sobald eine Position eröffnet ist, verwaltet die Strategie den Handel aktiv mithilfe von Stop-Loss-, Take-Profit-, Trailing-Stop- und Break-Even-Regeln, die die umfassende Money-Management-Konfiguration des ursprünglichen Experten widerspiegeln.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzentyp (Zeitrahmen) und berechnen Sie den schnellen EMA, den langsamen EMA und den ROC beim Kerzenschluss.
2. Geben Sie eine Long-Position ein, wenn der schnelle EMA über dem langsamen EMA liegt und ROC größer oder gleich dem Kaufmomentum-Schwellenwert ist. Bestehende Short-Positionen werden geschlossen, bevor die neuen Long-Positionen eröffnet werden.
3. Geben Sie eine Short-Position ein, wenn der schnelle EMA unter dem langsamen EMA liegt und ROC kleiner oder gleich dem negativen Verkaufsmomentum-Schwellenwert ist. Bestehende Long-Positionen werden geschlossen, bevor die neuen Short-Positionen eröffnet werden.
4. Verwalten Sie eine offene Position für jede fertige Kerze:
   - Schließen Sie den Handel zu den konfigurierten Take-Profit- oder Stop-Loss-Abständen (ausgedrückt in Preisschritten).
   - Aktivieren Sie optional einen Break-Even-Ausstieg, sobald sich der Preis um die Triggerdistanz zu Gunsten des Handels bewegt, und liquidieren Sie ihn, wenn der Preis zum beibehaltenen Offset zurückkehrt.
   - Verfolgen Sie den Stopp optional um den konfigurierten Abstand vom höchsten Höchstwert (lang) oder niedrigsten Tiefstwert (kurz), der nach dem Eintritt beobachtet wird.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| **Schneller EMA** | Länge des schnellen EMA-Trendfilters. |
| **Langsamer EMA** | Länge des langsamen EMA-Trendfilters. |
| **ROC Zeitraum** | Lookback-Fenster für den Rate-of-Change-Momentum-Filter. |
| **Momentum kaufen** | Mindestens positive ROC erforderlich, um Long-Trades zu eröffnen. |
| **Momentum verkaufen** | Mindestabsolut negativ ROC erforderlich, um Short-Trades zu eröffnen. |
| **Stop-Loss** | Stop-Loss-Distanz ausgedrückt in Preisschritten. |
| **Gewinn mitnehmen** | Take-Profit-Distanz, ausgedrückt in Preisschritten. |
| **Trailing verwenden** | Aktiviert die Trailing-Stop-Verwaltung. |
| **Nachlaufend** | Trailing-Stop-Distanz in Preisschritten. |
| **Break Even nutzen** | Aktiviert die Break-Even-Schutzlogik. |
| **Break-Even-Auslöser** | Profitieren Sie von den Preisschritten, die erforderlich sind, um die Break-Even-Logik zu aktivieren. |
| **Break-Even-Offset** | Abstand der Preisschritte, der beibehalten wird, nachdem der Break-Even-Trigger erreicht wurde. |
| **Kerzentyp** | Kerzentyp zur Versorgung der Indikatoren. |

## Notizen
- Die Strategie verwendet `Strategy.StartProtection()` einmal beim Start, um sicherzustellen, dass das integrierte Positionsschutzsystem gemäß den Empfehlungen der Projektrichtlinien aktiv ist.
- Die Positionsgröße basiert auf der Basiseigenschaft `Strategy.Volume`. Umkehraufträge berücksichtigen automatisch das aktuelle Risiko, sodass ein entgegengesetztes Signal sowohl die bestehende Position schließt als auch eine neue aufbaut.
- Alle Risikoparameter werden in Preisschritten ausgedrückt, da der ursprüngliche Fachberater Pip-basierte Abstände verwendete. Stellen Sie sicher, dass für das Gerät ein gültiger `PriceStep` konfiguriert ist.
