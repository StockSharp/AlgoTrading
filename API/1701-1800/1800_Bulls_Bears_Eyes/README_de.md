# Bulls Bears Eyes-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie bewertet das Gleichgewicht zwischen bullischem und bärischem Druck mithilfe der Indikatoren **Bulls Power** und **Bears Power**. Die beiden Indikatoren werden zu einem einzigen Oszillator zusammengefasst, der von 0 bis 100 skaliert ist. Hohe Werte zeigen die Dominanz der Käufer an, während niedrige Werte auf die Stärke der Verkäufer hinweisen.

Handelsentscheidungen basieren auf Schwellenwerten ähnlich dem ursprünglichen Experten *BullsBearsEyes*. Wenn der Oszillator den Überkauft-Pegel überschreitet, nachdem er darunter war, wird eine Long-Position eröffnet und jede Short-Position geschlossen. Umgekehrt löst ein Unterschreiten des Überverkauft-Niveaus einen Short-Einstieg aus und schließt bestehende Longs. Neutrale Werte zwischen den Schwellen halten die aktuelle Position, schließen aber gegensätzliche Trades.

## Parameter
- **Period** – Mittelungszeitraum für Bulls/Bears Power (Standard: 13).
- **High Level** – Überkauft-Schwelle, die Long-Signale generiert (Standard: 75).
- **Middle Level** – Referenz-Mittelniveau für die Trendinterpretation (Standard: 50).
- **Low Level** – Überverkauft-Schwelle, die Short-Signale generiert (Standard: 25).
- **Candle Type** – Zeitrahmen der von der Strategie verarbeiteten Kerzen (Standard: 4‑Stunden-Kerzen).

## Ein- und Ausstiegsregeln
1. Bulls Power und Bears Power für jede Kerze berechnen und den Oszillatorwert zwischen 0 und 100 ableiten.
2. **Long-Einstieg**: Oszillator kreuzt *High Level* von unten nach oben. Jede Short-Position wird vor dem Long-Einstieg geschlossen.
3. **Short-Einstieg**: Oszillator kreuzt *Low Level* von oben nach unten. Jede bestehende Long-Position wird vor dem Short-Einstieg geschlossen.
4. **Positionsausstieg**: Wenn der Oszillator die Seiten wechselt (ober-/unterhalb der mittleren Zone), wird die entgegengesetzte Position geschlossen.

Der Oszillator wird auch zusammen mit den Kerzen für die visuelle Analyse dargestellt.

## Hinweise
- Die Strategie verwendet die High-Level-API `SubscribeCandles` und `Bind` für die Indikatorverarbeitung.
- Schutzmechanismen werden beim Start über `StartProtection()` aktiviert.
- Nur abgeschlossene Kerzen werden ausgewertet, um vorzeitige Signale zu vermeiden.
