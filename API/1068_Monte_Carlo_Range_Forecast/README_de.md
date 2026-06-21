# Monte Carlo Bereichsprognose
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Monte Carlo Bereichsprognose verwendet Monte Carlo-Simulationen mit ATR-basierter Volatilität, um den künftigen Preisbereich zu projizieren. Die Strategie geht Long, wenn der durchschnittliche simulierte Preis den aktuellen Preis übersteigt, und Short, wenn er darunter fällt.

## Details
- **Daten**: Preiskerzen mit ATR.
- **Einstiegskriterien**:
  - **Long**: Erwarteter Preis aus Simulationen liegt über dem aktuellen Preis.
  - **Short**: Erwarteter Preis aus Simulationen liegt unter dem aktuellen Preis.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `ForecastPeriod` = 20
  - `Simulations` = 100
- **Filter**:
  - Kategorie: Statistisch
  - Richtung: Long & Short
  - Indikatoren: ATR
  - Komplexität: Moderat
  - Risikolevel: Mittel
