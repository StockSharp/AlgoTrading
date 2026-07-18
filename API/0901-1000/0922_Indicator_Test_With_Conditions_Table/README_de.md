# Indikatortest-Strategie mit Bedingungstabelle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie vergleicht den letzten Schlusskurs mit benutzerdefinierten Niveaus und führt Marktaufträge aus, wenn die Bedingungen erfüllt sind. Jede Seite (Long und Short) hat separate Einstiegs- und Ausstiegsregeln, die durch Parameter gesteuert werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Die aktivierte Long-Bedingung ist wahr.
  - **Short**: Die aktivierte Short-Bedingung ist wahr.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Die aktivierte Bedingung zum Schließen der Long-Position ist wahr.
  - **Short**: Die aktivierte Bedingung zum Schließen der Short-Position ist wahr.
- **Stops**: Nein.
- **Standardwerte**:
  - `LongOperator` = `>`
  - `CloseLongOperator` = `<`
  - `ShortOperator` = `<`
  - `CloseShortOperator` = `>`
- **Filter**:
  - Kategorie: Sonstiges
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
