# Laguerre-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt den Crossover zwischen einem Laguerre-Filter und einem kurzen FIR-Filter, der als gewichteter gleitender Durchschnitt der jüngsten Medianpreise aufgebaut ist.

- Der Laguerre-Filter glättet den Preis mithilfe des Gamma-Parameters, um Rauschen zu reduzieren.
- Die FIR-Linie ist ein gewichteter gleitender Durchschnitt über 4 Perioden mit symmetrischen Gewichten.
- Wenn die FIR-Linie über der Laguerre-Linie war und darunter kreuzt, eröffnet die Strategie eine Long-Position.
- Wenn die FIR-Linie darunter war und über die Laguerre-Linie kreuzt, wird eine Short-Position eröffnet.
- Entgegengesetzte Positionen werden geschlossen, wenn sich die Beziehung zwischen den Linien umkehrt.
- Ein Stop-Loss in Prozent des Einstiegspreises schützt jeden Trade.

Dieser Mean-Reversion-Ansatz versucht, Rücksetzer zu erfassen, wenn der Preis von der geglätteten Laguerre-Kurve abweicht.
