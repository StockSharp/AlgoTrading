# Strategie 3x Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **3x Supertrend**-Strategie verwendet drei ATR-basierte Bänder mit unterschiedlichen Perioden und Multiplikatoren.
Eine Long-Position wird eröffnet, wenn der Preis über alle drei Bänder steigt und das schnelle Band in einen
Aufwärtstrend wechselt. Der Trade wird geschlossen, wenn der Preis unter alle Bänder fällt und damit den Verlust des bullischen Momentums signalisiert.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**: Preis über allen Bändern und schnelles Band dreht aufwärts.
- **Ausstiegskriterien**: Preis unter allen Bändern.
- **Stops**: Keine.
- **Standardwerte**:
  - `AtrPeriod1` = 11
  - `Factor1` = 1
  - `AtrPeriod2` = 12
  - `Factor2` = 2
  - `AtrPeriod3` = 13
  - `Factor3` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: ATR-basierter Supertrend
  - Komplexität: Moderat
  - Risikolevel: Mittel
