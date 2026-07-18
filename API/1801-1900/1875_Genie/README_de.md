# Genie-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Genie ist ein Parabolic-SAR-Expertenberater, der durch den Average Directional Index (ADX) zur Bestätigung der Trendstärke erweitert wird. Die Strategie eröffnet Positionen, wenn der SAR relativ zum Preis wechselt, während die +DI- und -DI-Komponenten des ADX die Dominanz tauschen. Ein Trailing-Stop und ein fester Take-Profit steuern das Risiko.

Tests zeigen, dass der Ansatz bei Trendmärkten mit moderater Volatilität am besten funktioniert.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorheriger SAR über dem vorherigen Schlusskurs, aktueller SAR unter dem aktuellen Schlusskurs, vorheriger +DI < vorheriger -DI, aktueller +DI > aktueller -DI und ADX über beiden aktuellen +DI und -DI.
  - **Short**: Vorheriger SAR unter dem vorherigen Schlusskurs, aktueller SAR über dem aktuellen Schlusskurs, vorheriger +DI > vorheriger -DI, aktueller +DI < aktueller -DI und ADX über beiden aktuellen +DI und -DI.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Trailing-Stop wird ausgelöst oder die vorherige Kerze schließt gegen die Position.
- **Stops**: Ja, Trailing-Stop und Take-Profit in Preiseinheiten gemessen.
- **Standardwerte**:
  - `TakeProfit` = 500
  - `TrailingStop` = 200
  - `SarStep` = 0.02
  - `AdxPeriod` = 14
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja (zwischen +DI und -DI)
  - Risikolevel: Mittel
