# Strategie mit Polynomialregressions-Bandkanal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie passt eine Polynomialregressionslinie an die jüngsten Kurse an und erstellt obere und untere Bänder aus der Standardabweichung der Residuen. Long-Positionen werden eröffnet, wenn der Kurs unter das untere Band fällt, und Short-Positionen, wenn der Kurs über das obere Band steigt.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close < LowerBand`.
  - **Short**: `Close > UpperBand`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 100.
  - `Degree` = 2.
  - `StdDevMultiplier` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Polynomialregression
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Berechnung und Ausführung

Jede abgeschlossene Kerze passt ein Polynom vom Grad `Degree` mit kleinsten Quadraten an die letzten `Length` Schlusskurse einschließlich des aktuellen an, auf einem gleichmäßigen Raster von -1 bis 1. Die Kanalmitte ist der angepasste Wert der neuesten Beobachtung. Breite: `StdDevMultiplier * sqrt(sum((close[i] - fitted[i])^2) / Length)`, die Populationsabweichung der Residuen ohne Freiheitsgradkorrektur.

Schlusskurs strikt unter dem unteren Band eröffnet Long, strikt über dem oberen Short. Gegeneinstieg kehrt die ganze Position mit einer Market-Order von `Volume + abs(Position)` um; Einstieg ohne Position nutzt `Volume`. Innerhalb der Bänder bleibt die Position. Handel erst nach dem vollständigen Fenster; keine Wartefrist oder Schutzstopps.

Zulässig: `0 <= Degree <= 6`, `Length >= Degree + 2`, positiver `StdDevMultiplier`. Grad null ist eine Konstante. Das feste normalisierte Raster nutzt eine vorab berechnete orthonormale Polynombasis mit zwei Orthogonalisierungsdurchläufen; jedes Fenster projiziert Preisverschiebungen darauf mit double-Präzision. C# und Python rechnen gleich; Ordermengen bleiben decimal.
