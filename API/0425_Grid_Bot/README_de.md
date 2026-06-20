# Grid Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Grid Bot teilt einen vordefinierten Preisbereich in gleiche Ebenen auf und handelt die Schwingungen zwischen ihnen. Wenn der Preis zur unteren Hälfte des Gitters driftet, akkumuliert die Strategie Long-Positionen und verkauft sie, wenn der Preis zur oberen Hälfte zurückkehrt. Dieser Ansatz gedeiht in Seitwärtsmärkten mit klaren Grenzen.

Es wird keine Richtungsverzerrung angenommen; der Bot reagiert einfach auf die Nähe zu den Gitterlinien.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis berührt ein Niveau in der unteren Hälfte ohne Long-Position
  - **Short**: Preis berührt ein Niveau in der oberen Hälfte ohne Short-Position
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetztes Einstiegssignal schließt bestehende Position
- **Stops**: Keine
- **Standardwerte**:
  - `UpperLimit` = 48000
  - `LowerLimit` = 45000
  - `GridCount` = 10
- **Filter**:
  - Kategorie: Range trading
  - Richtung: Beide
  - Indikatoren: Price levels
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
