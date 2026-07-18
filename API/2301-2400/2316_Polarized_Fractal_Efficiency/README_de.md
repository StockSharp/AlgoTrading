# Strategie Polarisierte Fraktale Effizienz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des **Polarized Fractal Efficiency (PFE)**-Indikators. PFE misst die Effizienz der Preisbewegung und wechselt das Vorzeichen, wenn sich der Impuls verschiebt.

## Handelslogik

1. Kerzen des gewählten Zeitrahmens abonnieren und PFE berechnen.
2. Wenn PFE am vorherigen Balken niedriger als zwei Balken zuvor ist und der aktuelle Wert höher als der vorherige, wird eine Long-Position eröffnet.
3. Wenn PFE am vorherigen Balken höher als zwei Balken zuvor ist und der aktuelle Wert niedriger als der vorherige, wird eine Short-Position eröffnet.
4. Gegenteilige Positionen werden vor dem Öffnen neuer Positionen geschlossen.
5. Optionaler Stop-Loss- und Take-Profit-Schutz wird aktiviert.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `CandleType` | Kerzenserie für die Analyse. |
| `PfePeriod` | Periode zur Berechnung des PFE-Indikators. |
| `SignalBar` | Offset des für Signale verwendeten Balkens. |
| `TakeProfit` | Take Profit in Preisschritten. |
| `StopLoss` | Stop Loss in Preisschritten. |

