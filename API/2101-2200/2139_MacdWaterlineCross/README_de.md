# MACD Waterline Cross Expectator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long, wenn die MACD-Signallinie das Nullniveau von unten kreuzt, und Short, wenn sie es von oben kreuzt. Das Risikomanagement verwendet einen Stop-Loss und einen konfigurierbaren Risiko-Ertrags-Multiplikator zur Festlegung der Take-Profit-Distanz.

## Logik
- MACD-Indikator mit konfigurierbaren schnellen EMA-, langsamen EMA- und Signalperioden berechnen.
- Den Signallinienwert auf jeder abgeschlossenen Kerze verfolgen.
- Wenn die Signallinie von negativ zu positiv kreuzt und die Strategie bereit zum Kaufen ist, wird eine Long-Marktorder platziert.
- Wenn die Signallinie von positiv zu negativ kreuzt und die Strategie bereit zum Verkaufen ist, wird eine Short-Marktorder platziert.
- Stop-Loss- und Take-Profit-Niveaus werden für jede neue Position automatisch gesetzt.

## Parameter
- **FastEmaPeriod** – Länge der schnellen EMA im MACD.
- **SlowEmaPeriod** – Länge der langsamen EMA im MACD.
- **SignalPeriod** – Länge der Signal-EMA.
- **StopLoss** – Abstand zum Stop-Loss in absoluten Preiseinheiten.
- **Volume** – Auftragsgröße für neue Positionen.
- **RiskBenefitRatios** – voreingestellte Verhältnisse von 1:5 bis 1:1 zur Definition der Take-Profit-Distanz.
- **CandleType** – Zeitrahmen der von der Strategie verwendeten Kerzen.

## Hinweise
- Die Strategie wechselt mithilfe eines internen Flags zwischen Long- und Short-Trades.
- Trades werden zu Marktpreisen ausgeführt und kehren die aktuelle Position immer um.
