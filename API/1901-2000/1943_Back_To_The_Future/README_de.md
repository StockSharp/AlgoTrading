# Back to the Future-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Momentum-Strategie vergleicht den aktuellen Schlusskurs mit dem Kurs vor einer festgelegten Anzahl von Minuten. Wenn der Kurs relativ zum historischen Kurs einen definierten Schwellenwert überschreitet, eröffnet das System eine Long-Position. Umgekehrt öffnet es eine Short-Position, wenn der Kurs unter den negativen Schwellenwert fällt. Der Ansatz geht davon aus, dass starke Bewegungen weg vom vergangenen Preis auf entstehende Trends hinweisen.

Die Strategie operiert auf abgeschlossenen Kerzen und funktioniert mit jedem von StockSharp unterstützten Instrument und Zeitrahmen. Eingebaute Take-Profit- und Stop-Loss-Niveaus steuern das Risiko, sobald eine Position eröffnet wird. Eine Warteschlange vergangener Preise hält eine rollende Historie zur Auswertung der Preisdifferenz bereit.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close(t) - Close(t-Δ) > BarSize`.
  - **Short**: `Close(t) - Close(t-Δ) < -BarSize`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - **Long**: `Close >= Entry + TakeProfit` oder `Close <= Entry - StopLoss`.
  - **Short**: `Close <= Entry - TakeProfit` oder `Close >= Entry + StopLoss`.
- **Stops**: Ja, feste Take-Profit- und Stop-Loss-Werte in Kurseinheiten.
- **Standardwerte**:
  - `BarSize = 0.25`
  - `HistoryMinutes = 60`
  - `TakeProfit = 10`
  - `StopLoss = 5000`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
