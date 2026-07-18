# Bjorgum Double Tap-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie sucht nach Doppel-Top- und Doppel-Boden-Mustern. Ein Short-Trade wird eröffnet, wenn der Kurs unter die Nackenlinie eines Doppel-Tops fällt, und ein Long-Trade, wenn der Kurs über die Nackenlinie eines Doppel-Bodens ausbricht. Ziel- und Stop-Niveaus werden als Fibonacci-Extensionen der Musterhöhe berechnet.

## Details

- **Einstiegskriterien**:
  - **Long**: Doppel-Boden-Ausbruch über die Nackenlinie.
  - **Short**: Doppel-Top-Ausbruch unter die Nackenlinie.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop- oder Zielniveaus.
- **Stops**: Fibonacci-Prozentsatz über `StopLossFib`.
- **Standardwerte**:
  - Pivot-Länge 50.
  - Pivot-Toleranz 15%.
  - Ziel-Fib 100%.
  - Stop-Fib 0%.
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Highest/Lowest
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
