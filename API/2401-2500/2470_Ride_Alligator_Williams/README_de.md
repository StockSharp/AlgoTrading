# Ride Alligator Williams-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert Bill Williams' Alligator-Indikator. Die Lips-, Teeth- und Jaw-Linien werden aus dem Medianpreis mithilfe geglätteter gleitender Durchschnitte berechnet, deren Längen aus einer Basisperiode über den Goldenen Schnitt abgeleitet werden. Eine Long-Position wird geöffnet, wenn die Lips über die Jaws kreuzen, während die Teeth darunter bleiben. Eine Short-Position wird geöffnet, wenn die Lips unter die Jaws kreuzen, während die Teeth darüber bleiben. Für eine offene Position folgt ein Trailing Stop der Jaw-Linie.

## Parameter
- **Base Period** – Basisperiode zur Ableitung der Alligator-Längen.
- **Candle Type** – Zeitrahmen der Eingangskerzen.

## Indikatoren
- Geglätteter gleitender Durchschnitt (Lips, Teeth, Jaw)

## Einstiegsregeln
- Long, wenn Lips über Jaws kreuzen und Teeth darunter liegen.
- Short, wenn Lips unter Jaws kreuzen und Teeth darüber liegen.

## Ausstiegsregeln
- Ein entgegengesetzter Kreuzungspunkt schließt die Position.
- Trailing Stop an der Jaw-Linie schließt bei Kurskreuzung.
