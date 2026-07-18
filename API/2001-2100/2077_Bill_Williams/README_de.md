# Strategie Bill Williams
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bill Williams kombiniert den Alligator-Indikator mit Fraktal-Ausbrüchen. Kiefer, Zähne und Lippen müssen divergieren, bevor ein Ausbruch des letzten Fraktals eine Order auslöst.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - Fraktal-Hochs und -Tiefs aus den letzten 5 Kerzen berechnen.
  - Der Abstand zwischen Kiefer und Zähnen muss `GatorDivSlowPoints` überschreiten.
  - Der Abstand zwischen Lippen und Zähnen muss `GatorDivFastPoints` überschreiten.
  - **Long**: Preis schließt mindestens `FilterPoints` Punkte oberhalb des letzten Aufwärts-Fraktals und die Kerze ist bullisch.
  - **Short**: Preis schließt mindestens `FilterPoints` Punkte unterhalb des letzten Abwärts-Fraktals und die Kerze ist bärisch.
- **Ausstiegskriterien**:
  - Entgegengesetzter Ausbruch.
  - Trailing-Stop am letzten entgegengesetzten Fraktal.
- **Stops**: Fraktalbasierter Trailing-Stop.
- **Standardwerte**:
  - `FilterPoints` = 30
  - `GatorDivSlowPoints` = 250
  - `GatorDivFastPoints` = 150
  - `CandleType` = 1-Stunden-Kerzen
