# Hull Suite Strategie – 1% Risiko, Kein SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Hull Suite Strategie eröffnet Long-Positionen, wenn der ausgewählte Hull gleitende Durchschnitt im Vergleich zu zwei Bars zuvor steigt, und Short-Positionen, wenn er fällt. Es wird kein Stop-Loss oder Take-Profit verwendet.

## Details

- **Einstiegskriterien**:
  - **Long**: Hull-Wert größer als der Wert vor zwei Bars.
  - **Short**: Hull-Wert kleiner als der Wert vor zwei Bars.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Position bei entgegengesetztem Signal umkehren.
- **Stops**: Keine.
- **Standardwerte**:
  - `HullLength` = 55
  - `Mode` = Hma
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: HMA, EHMA, THMA
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: 5m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
