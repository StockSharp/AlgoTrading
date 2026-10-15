# Doppelboden- und Doppeltop-Jäger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach Doppelboden- und Doppeltop-Mustern, indem sie aktuelle Tiefs und Hochs vergleicht. Ein Doppelboden entsteht, wenn das niedrigste Tief innerhalb eines längeren Rückblickfensters zweimal erreicht wird, während das Doppeltop das höchste Hoch verwendet. Long- und Short-Positionen werden entsprechend eröffnet und geschlossen, wenn der Preis das entgegengesetzte Extrem nach Bildung eines neuen Extrems durchbricht.

## Details

- **Einstiegskriterien**:
  - **Long**: Doppelboden erkannt.
  - **Short**: Doppeltop erkannt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Long: Neues Hoch über dem vorherigen Hoch bei fallendem Preis unter das vorherige Tief.
  - Short: Neues Tief unter dem vorherigen Tief bei steigendem Preis über das vorherige Hoch.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 100
  - `Lookback` = 100
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
