# Strategie zur Akkumulation an ungemilderten Niveaus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Akkumuliert Long-Positionen durch Limit-Orders an vorherigen Tages-, Wochen-, Monats- und Jahrestiefs, die in letzter Zeit nicht erneut besucht wurden. Orders werden nur während der London-Session platziert und alle Positionen werden bei neuen Allzeithochs geschlossen.

## Details

- **Einstiegskriterien**:
  - Limit-Käufe an ungemilderten historischen Tiefs während der Sitzungszeiten.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Alle schließen bei neuem Allzeithoch.
- **Stops**: Keine.
- **Standardwerte**:
  - `Max Lookback` = 50
  - `Session Start` = 09:00
  - `Session End` = 17:00
  - `Base PDL` = 0.1
  - `Base PWL` = 0.2
  - `Base PML` = 0.4
  - `Base PYL` = 0.8
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Ja (London-Session)
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
