# DreamBot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
DreamBot ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „DreamBot“. Die Strategie überwacht den Force-Index-Oszillator auf stündlichen Kerzen und wartet darauf, dass das Momentum bullische oder bärische Schwellenwerte überschreitet. Wenn der Force-Index das bullische Niveau überschreitet, nachdem er beim vorherigen Balken darunter lag, eröffnet die Strategie eine Long-Position. Wenn der Force-Index das rückläufige Niveau unterschreitet, nachdem er darüber gelegen hat, eröffnet die Strategie eine Short-Position. Der Handel erfolgt nur, wenn keine Position vorhanden ist, was die Einzelpositionslogik des ursprünglichen Roboters widerspiegelt.

## Handelslogik
- Abonnieren Sie H1-Kerzen und berechnen Sie einen geglätteten Kraftindex (standardmäßig Länge 13).
- Verfolgen Sie die letzten beiden abgeschlossenen Force-Index-Werte. Signale werden unter Verwendung der *vorherigen* Balkenwerte generiert, genau wie bei der MT4-Implementierung (`iForce` mit Verschiebung 1 und 2).
- Geben Sie Long ein, wenn der Force Index der vorherigen Kerze über `BullsThreshold` liegt und der Wert zwei Kerzen zurück unter dem Schwellenwert lag, vorausgesetzt, es ist keine Position offen.
- Geben Sie Short ein, wenn der Force Index der vorherigen Kerze unter `BearsThreshold` liegt und der Wert zwei Kerzen zurück über dem Schwellenwert lag, vorausgesetzt, es ist keine Position offen.
- Der optionale Trailing-Stop reproduziert den ursprünglichen EA: Sobald der Gewinn `TrailingStepPoints` übersteigt, wird ein Stop-Level auf `TrailingStartPoints` vom Preis weg gezogen und folgt weiteren Anstiegen.

## Risikomanagement
- `StartProtection` fügt klassische Stop-Loss- und Take-Profit-Orders unter Verwendung der durch die Preisstufe des Instruments umgerechneten Distanz von MetaTrader „Punkten“ hinzu.
- Der Trailing-Schutz ist marktbasiert: Wenn das berechnete Trailing-Level durchbrochen wird, sendet die Strategie einen Marktauftrag, um die Position sofort zu schließen.
- Die Positionsverfolgung erfasst den volumengewichteten Einstiegspreis, sodass die abschließende Logik auf Teilfüllungen und Umkehrungen ausgerichtet ist.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `ForcePeriod` | Glättungszeitraum des Force-Index (Standard 13). |
| `TakeProfitPoints` | Take-Profit-Distanz in MetaTrader Punkten. |
| `StopLossPoints` | Stop-Loss-Distanz in MetaTrader Punkten. |
| `BullsThreshold` | Schwellenwert des Bullish Force Index, der Long-Einstiege ermöglicht. |
| `BearsThreshold` | Schwellenwert des Bearish Force Index, der Short-Einstiege ermöglicht. |
| `EnableTrailing` | Aktiviert die Trailing-Stop-Logik. |
| `TrailingStartPoints` | Abstand (in Punkten), der nach der Aktivierung zwischen Preis und Trailing Stop beibehalten wird. |
| `TrailingStepPoints` | Erforderlicher Gewinn (in Punkten), bevor der Trailing Stop aktiviert wird. |
| `CandleType` | Für die Force-Index-Berechnungen verwendeter Zeitrahmen (standardmäßig H1-Kerzen). |

## Notizen
- Die Parametervalidierung verhindert, dass der Trailing-Trigger (`TrailingStepPoints`) die Trailing-Distanz (`TrailingStartPoints`) überschreitet, was der Sicherheitsprüfung MetaTrader entspricht.
- Die Stop-Level-Durchsetzung des ursprünglichen EA (Broker `MODE_STOPLEVEL`) wird durch die Preisstufenumrechnungen von StockSharp angenähert. Abhängig von den Einschränkungen des Brokers kann eine zusätzliche Validierung erforderlich sein.
- Alle Codekommentare und Protokolle werden gemäß den Konvertierungsrichtlinien auf Englisch bereitgestellt.
