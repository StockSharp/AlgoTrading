# Künstliche-Intelligenz-Perceptron-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie für Künstliche Intelligenz verwendet ein einfaches Perceptron, um mehrere Accelerator-Oscillator-Messwerte (AC) mit unterschiedlichen Zeitverschiebungen zu kombinieren. Die gewichtete Summe des aktuellen AC-Werts und drei verzögerter Werte (7, 14, 21 Balken zurück) bestimmt die Handelsrichtung. Wenn der Perceptron-Output positiv ist, eröffnet oder hält die Strategie eine Long-Position; bei negativem Output eröffnet oder hält sie eine Short-Position.

Nach einem Einstieg schützt die Strategie den Trade mit einem Stop-Loss in Punkten. Wenn sich der Preis in die profitable Richtung bewegt, folgt das Stop-Niveau dem Preis. Wenn der Perceptron-Output das Vorzeichen wechselt, während die Position profitabel ist, kehrt die Strategie um, schließt die aktuelle Position und eröffnet die entgegengesetzte.

Tests zeigen, dass dieser Ansatz schnell auf Momentum-Änderungen reagieren kann, während das Risiko kontrolliert bleibt. Er funktioniert auf jedem Instrument, das Kerzendaten liefert, und ist nicht auf bestimmte Marktregime angewiesen.

## Details

- **Einstiegskriterien**  
  - **Long**: Perceptron-Output > 0 und keine bestehende Long-Position.  
  - **Short**: Perceptron-Output < 0 und keine bestehende Short-Position.
- **Ausstieg / Umkehr**  
  - Trailing-Stop ausgelöst.  
  - Perceptron-Output wechselt das Vorzeichen; Strategie kehrt Position um.
- **Stops**: Ja, Trailing-Stop basierend auf dem Parameter `StopLoss`.
- **Standardwerte**  
  - `X1 = 135`  
  - `X2 = 127`  
  - `X3 = 16`  
  - `X4 = 93`  
  - `StopLoss = 85`
- **Filter**  
  - Kategorie: Momentum  
  - Richtung: Beide  
  - Indikatoren: Accelerator Oscillator  
  - Stops: Ja  
  - Komplexität: Mittel  
  - Zeitrahmen: Kurzfristig  
  - Neuronale Netze: Perceptron  
  - Divergenz: Nein  
  - Risikolevel: Mittel
