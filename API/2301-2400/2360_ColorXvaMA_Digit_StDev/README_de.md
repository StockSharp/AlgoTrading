# ColorXvaMA Digit StDev-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie handelt basierend darauf, wie weit der Preis von einem exponentiellen gleitenden Durchschnitt (EMA) abweicht. Zwei Abweichungsmultiplikatoren (K1 und K2) definieren innere und äußere Bänder, die aus der Standardabweichung des Preises berechnet werden.

Wenn der Preis um K2 Standardabweichungen über den EMA steigt, geht die Strategie in eine Long-Position. Wenn der Preis um K2 Standardabweichungen unter den EMA fällt, geht sie in eine Short-Position. Bestehende Positionen werden geschlossen, sobald die Abweichung in das durch K1 definierte innere Band zurückehrt.

## Parameter
- **EMA Length** – Periode des exponentiellen gleitenden Durchschnitts.
- **StdDev Length** – Periode für die Berechnung der Standardabweichung.
- **Deviation K1** – Multiplikator für das innere Band zum Schließen von Positionen.
- **Deviation K2** – Multiplikator für das äußere Band zum Öffnen von Positionen.
- **Candle Type** – Zeitrahmen der Kerzen.

## Indikatoren
- Exponential Moving Average
- StandardDeviation

## Funktionsweise
1. Kerzen des gewählten Zeitrahmens abonnieren.
2. EMA und Standardabweichung des Preises berechnen.
3. Preisabweichung vom EMA berechnen.
4. Long/Short einsteigen, wenn die Abweichung ±K2×StdDev überschreitet.
5. Aussteigen, wenn die Abweichung in ±K1×StdDev zurückkehrt.

Dieser Ansatz versucht, starke Mittelwertabweichungen zu erfassen und bei der Umkehr auszusteigen.
