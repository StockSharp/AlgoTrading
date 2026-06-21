# Gap-Filling-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Gap-Filling-Strategie sucht nach Preisgaps über Nacht zu Beginn einer neuen Sitzung. Wenn eine Lücke erscheint, handelt die Strategie standardmäßig dagegen und erwartet eine Rückkehr zum Preis des Vortages oder, wenn invertiert, handelt sie in Richtung der Lücke mit einem Stop auf dem Gap-Niveau.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Neue Sitzung und Abwärtslücke (oder Aufwärtslücke wenn invertiert).
  - **Short**: Neue Sitzung und Aufwärtslücke (oder Abwärtslücke wenn invertiert).
- **Ausstiegskriterien**:
  - Gap-Fill-Preis erreicht (Gewinnziel) oder, wenn invertiert, Preis trifft den Stop auf dem Gap-Niveau.
- **Stops**: Verwendet den Preis der vorherigen Sitzung als Ziel/Stop.
- **Standardwerte**:
  - `CandleType` = 1 minute
  - `Invert` = false
  - `CloseWhen` = NewSession
- **Filter**:
  - Kategorie: Gap-Handel
  - Richtung: Long & Short
  - Indikatoren: Keine
  - Komplexität: Einfach
  - Risikolevel: Mittel
