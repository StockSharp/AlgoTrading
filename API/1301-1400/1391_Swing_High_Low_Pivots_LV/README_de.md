# Swing-Hoch-Tief-Pivot-Strategie [LV]
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt um bestätigte Swing-Hochs und -Tiefs. Wenn ein Pivot-Tief erscheint, platziert die Strategie eine Long-Limitorder beim Pivot-Balkenkurs und setzt feste Stop- und Take-Profit-Ziele. Pivot-Hochs lösen Short-Setups aus. Ein optionaler gleitender Durchschnitt-Filter kann Trades auf die Trendrichtung beschränken.

## Details

- **Eingaben**:
  - Pivot-Länge.
  - Stop-Loss-Abstand in Ticks.
  - Take-Profit-Abstand in Ticks.
  - Zweiter Take-Profit und Doppeleinstiegsschalter.
  - Typ und Länge des gleitenden Durchschnitts-Filters.
- **Long/Short**: Beide.
- **Ausstieg**: Fester Stop und bis zu zwei Gewinnziele.
- **Filter**:
  - Kategorie: Mustererkennung
  - Richtung: Beide
  - Indikatoren: Gleitender Durchschnitt
  - Stops: Fest
  - Komplexität: Hoch
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
