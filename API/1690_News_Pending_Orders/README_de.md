# Strategie für Nachrichten-Pending-Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert ein Paar ausstehender Stop-Orders rund um den aktuellen Preis und verwaltet sie, während sich der Markt entwickelt. Sie ist für den Handel während Nachrichtenveröffentlichungen konzipiert, bei denen starke Bewegungen erwartet werden.

## Funktionsweise

- Bei flacher Position platziert die Strategie:
  - Eine **Buy-Stop**-Order bei `Ask + Step`.
  - Eine **Sell-Stop**-Order bei `Bid - Step`.
- Ausstehende Orders werden alle `TimeModify` Sekunden neu bepreist, wenn sich der Markt um mindestens `StepTrail` bewegt hat.
- Wenn eine Order ausgeführt wird, wird die entgegengesetzte ausstehende Order storniert.
- Ein Schutz-Stop-Loss und ein optionaler Take-Profit werden auf Basis des Einstiegspreises erstellt.
- Der Stop-Loss kann nach einem definierten Gewinn auf Break-Even verschoben werden und dann dem Preis folgen.

Die Strategie arbeitet mit Level1-Daten und stützt sich auf keine Indikatoren.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `Step` | 10 | Abstand in Ticks für ausstehende Stop-Orders. |
| `StopLoss` | 10 | Initialer Stop-Loss in Ticks. |
| `TakeProfit` | 50 | Take-Profit in Ticks (0 deaktiviert). |
| `TrailingStop` | 10 | Trailing-Stop-Abstand in Ticks. |
| `TrailingStart` | 0 | Gewinn in Ticks vor Aktivierung des Trailings. |
| `StepTrail` | 2 | Mindestänzug im Stop-Preis (in Ticks) für eine neue Stop-Order. |
| `BreakEven` | false | Stop nach Erreichen von `MinProfitBreakEven` auf Einstieg verschieben. |
| `MinProfitBreakEven` | 0 | Gewinn in Ticks zum Verschieben des Stops auf Break-Even. |
| `TimeModify` | 30 | Sekunden zwischen Neubepreisungsversuchen. |

## Hinweise

- Orders werden über die High-Level-API von StockSharp verwaltet.
- Die Strategie storniert Schutzorders, wenn die Position geschlossen wird.
- Nur die C#-Version wird bereitgestellt; keine Python-Implementierung enthalten.
