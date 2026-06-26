# Perceptron Mult Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **Peceptron_Mult.mq5** Experten zur StockSharp-High-Level-API. Sie überwacht gleichzeitig bis zu drei unabhängige Märkte und wendet den Acceleration/Deceleration (AC) Oszillator in einem Perceptron-Modell an. Jeder Markt erhält seine eigene Gewichtskonfiguration, Positionsgrößenbestimmung und Schutzausstiege, sodass das Verhalten des originalen Multi-Symbol-Beraters erhalten bleibt.

## Handelslogik

1. Für jedes konfigurierte Wertpapier abonniert die Strategie denselben Kerzentyp (Standard: 1 Minute).
2. Bei jeder abgeschlossenen Kerze berechnet sie den Bill Williams Acceleration/Deceleration Oszillator:
   - Den Awesome Oscillator (AO) aus Kerzenhochs und -tiefs berechnen (5/34 Median-Preis-gleitende Durchschnitte).
   - Einen 5-Perioden einfachen gleitenden Durchschnitt von AO vom aktuellen AO-Wert subtrahieren.
3. Ein Rollpuffer mit den neuesten 22 AC-Werten wird pro Wertpapier geführt.
4. Das Perceptron-Signal wird aus vier verzögerten AC-Werten mit Gewichten (`w - 100`) gebildet, genau wie im MQL-Code:
   - `AC[0]`, `AC[7]`, `AC[14]`, `AC[21]` entsprechen der neuesten und drei historischen Lesungen.
5. Einstiegsregeln:
   - Positive Summe ⇒ Long-Position öffnen, wenn keine Position auf diesem Wertpapier besteht.
   - Negative Summe ⇒ Short-Position öffnen, wenn das Wertpapier flach ist.
6. Ausstiegsregeln:
   - Stop-Loss- und Take-Profit-Abstände werden in Punkten ausgedrückt. Sie werden in absolute Preisoffsets umgerechnet, indem der Instrument-Preisschritt verwendet wird.
   - Schutzausstiege werden bei jeder abgeschlossenen Kerze ausgewertet. Ein Long-Trade wird geschlossen, wenn das Kerzentief den Stop trifft oder das Hoch das Gewinnziel erreicht; Shorts verwenden die gespiegelte Logik.
7. Positionen sind pro Wertpapier gegenseitig ausschließend. Die Strategie ignoriert neue Signale, während Exposition offen bleibt, und repliziert das Verhalten des Original-Beraters.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `FirstSecurity`, `SecondSecurity`, `ThirdSecurity` | Vom Perceptron verarbeitete Instrumente. Auf `null` belassen, um einen Slot zu deaktivieren.
| `FirstOrderVolume`, `SecondOrderVolume`, `ThirdOrderVolume` | Marktordergröße für jedes Instrument.
| `FirstWeight1`…`FirstWeight4`, etc. | Perceptron-Gewichte (MQL-Eingaben `x1…x12`). Die Strategie subtrahiert intern 100 von jedem Wert, bevor er angewendet wird.
| `FirstStopLossPoints`, `SecondStopLossPoints`, `ThirdStopLossPoints` | Stop-Loss-Abstand in Preispunkten für jedes Instrument. Auf 0 setzen, um zu deaktivieren.
| `FirstTakeProfitPoints`, `SecondTakeProfitPoints`, `ThirdTakeProfitPoints` | Take-Profit-Abstand in Preispunkten für jedes Instrument. Auf 0 setzen, um zu deaktivieren.
| `CandleType` | Von allen Wertpapieren gemeinsam genutzte Kerzenserie.

## Implementierungshinweise

- Die Strategie verlässt sich auf `AwesomeOscillator`- und `SimpleMovingAverage`-Indikatoren von StockSharp zur Rekonstruktion des AC-Oszillators und vermeidet so manuelle Neuberechnungen.
- Rollpuffer werden nur verwendet, um die Perceptron-Eingaben aus der MQL-Implementierung zu emulieren (Indizes 0, 7, 14, 21).
- Schutzniveaus werden durchgesetzt, ohne separate Stop-Orders zu registrieren: Die Strategie überwacht Kerzenextreme und schließt Positionen mit Marktorders, wenn Niveaus verletzt werden, was das Verhalten des Original-EA bei neuen Ticks widerspiegelt.
- Jedes Wertpapier behält unabhängigen Indikatorstatus, Ordervolumen und Risikoeinstellungen, entsprechend der Drei-Symbol-Struktur des Quell-Beraters.

## Verwendungstipps

1. Bis zu drei Wertpapiere im Parameterpanel zuweisen. Jeder unbenutzte Slot kann `null` bleiben.
2. Die punktbasierten Stops und Ziele anpassen, um der Tick-Größe der ausgewählten Instrumente zu entsprechen.
3. Die Perceptron-Gewichte abstimmen, um spezifische Verzögerungen des AC-Oszillators zu betonen, wenn Optimierung erforderlich ist.
4. Da alle Instrumente denselben Kerzentyp teilen, sicherstellen, dass historische Daten für jedes konfigurierte Wertpapier verfügbar sind.
