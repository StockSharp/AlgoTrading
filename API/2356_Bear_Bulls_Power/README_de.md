# Bear Bulls Power-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Konvertierung des MetaTrader 5-Experten "Exp_Bear_Bulls_Power". Sie verwendet einen geglätteten Bear/Bulls Power-Indikator zur Erkennung von Trendumkehrungen.

## Funktionsweise

1. Berechne den Medianpreis jeder Kerze: `(High + Low) / 2`.
2. Glätte den Medianpreis mit einem gleitenden Durchschnitt der Länge `FirstLength`.
3. Berechne die Differenz zwischen dem Medianpreis und seinem gleitenden Durchschnitt.
4. Wende eine zweite Glättung mit einem gleitenden Durchschnitt der Länge `SecondLength` an.
5. Bestimme die Trendrichtung durch Vergleich des aktuellen geglätteten Werts mit dem vorherigen.
6. Erzeuge Signale, wenn sich die Richtung ändert:
   - Eine Aufwärtsdrehung über null öffnet eine Long-Position.
   - Eine Abwärtsdrehung unter null öffnet eine Short-Position.

## Parameter

- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen.
- **First Length** – Periode für die Preisglättung.
- **Second Length** – Periode für die Signalglättung.

Die Strategie verwendet Marktorders und arbeitet nur mit abgeschlossenen Kerzen.
