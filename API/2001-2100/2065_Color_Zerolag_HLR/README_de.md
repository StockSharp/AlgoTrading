# Color Zerolag HLR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Konvertierung des MQL5-Experten `Exp_ColorZerolagHLR`. Sie kombiniert mehrere Hi-Lo-Range-(HLR-)Oszillatoren mit unterschiedlichen Längen und Gewichtungen und wendet dann eine exponentielle Glättung an, um schnelle und langsame Trendlinien zu erstellen. Kreuzungen zwischen diesen Linien erzeugen Handelssignale.

## Überblick
- Erstellt fünf HLR-Werte mit dem höchsten Hoch und niedrigsten Tief über angegebene Perioden.
- Gewichtet jeden HLR und summiert sie, um eine schnelle Trendlinie zu erzeugen.
- Wendet Zero-Lag-Glättung an, um eine langsame Trendlinie abzuleiten.
- Handelt, wenn die schnelle Linie die langsame Linie kreuzt.

## Parameter
- `Smoothing` – EMA-Glättungsfaktor.
- `Factor1`..`Factor5` – Gewichtungen für jede HLR-Komponente.
- `HlrPeriod1`..`HlrPeriod5` – Lookback-Perioden für HLR-Berechnungen.
- `BuyPosOpen`/`SellPosOpen` – erlauben das Öffnen von Long- oder Short-Positionen.
- `BuyPosClose`/`SellPosClose` – erlauben das Schließen bestehender Positionen.
- `CandleType` – Zeitrahmen der Kerzen.

## Indikatoren
- Highest, Lowest (jeweils fünf Paare).

## Handelslogik
- Wenn die vorherige schnelle Linie über der langsamen Linie lag und jetzt darunter kreuzt, öffnet die Strategie eine Long-Position und schließt jede Short-Position.
- Wenn die vorherige schnelle Linie unter der langsamen Linie lag und jetzt darüber kreuzt, öffnet die Strategie eine Short-Position und schließt jede Long-Position.

Verwenden Sie diese Vorlage als Ausgangspunkt und passen Sie die Parameter oder das Risikomanagement Ihren Bedürfnissen an.
