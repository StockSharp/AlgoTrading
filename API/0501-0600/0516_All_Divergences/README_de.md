# Alle-Divergenzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Alle-Divergenzen-Strategie sucht nach bullischen und bärischen RSI-Divergenzen, gefiltert durch einen gleitenden Durchschnittstrend. Eine Long-Position wird eröffnet, wenn der Preis ein niedrigeres Tief macht, während der RSI ein höheres Tief über dem gleitenden Durchschnitt bildet. Eine Short-Position wird eröffnet, wenn der Preis ein höheres Hoch macht, während der RSI ein niedrigeres Hoch unter dem gleitenden Durchschnitt bildet. Ein optionaler Stop-Loss und Take-Profit kann Positionen automatisch schließen, und eine gleitende Durchschnitt-Risikokontrolle schließt die Position nach mehreren Schlusskursen gegen den Trend.

## Details

- **Einstiegskriterien**:
  - Die Preisposition relativ zum gleitenden Durchschnitt definiert den Trend.
  - **Long**: Preis macht tieferes Tief, RSI höheres Tief, Preis über MA.
  - **Short**: Preis macht höheres Hoch, RSI niedrigeres Hoch, Preis unter MA.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal oder MA-Risikoausstieg.
- **Stops**: Optionaler Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `MaLength` = 50
  - `RsiLength` = 14
  - `MaRiskCandles` = 3
  - `UseProtection` = False
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: RSI, Moving Average
  - Stops: Optional
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
