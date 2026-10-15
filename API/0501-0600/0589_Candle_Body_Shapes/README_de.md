# Strategie für Kerzenkörper-Formen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die basierend darauf handelt, wo eine Kerze innerhalb ihrer Spanne öffnet und schließt.
Eröffnet eine Long-Position, wenn die Kerze nahe ihrem Tief öffnet und nahe ihrem Hoch schließt — starker bullischer Druck.
Eröffnet eine Short-Position, wenn die Kerze nahe ihrem Hoch öffnet und nahe ihrem Tief schließt — starker bärischer Druck.

Der Ansatz basiert ausschließlich auf Price Action und ist auf jeden liquiden Markt anwendbar.

## Details

- **Einstiegskriterien**:
  - Long: `Open near Low && Close near High`
  - Short: `Open near High && Close near Low`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenteiliges Signal
- **Stops**: Nein
- **Standardwerte**:
  - `BodyThreshold` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Kerzenmuster
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
