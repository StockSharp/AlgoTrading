# Konfigurierbare Boilerplate-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die konfigurierbare Boilerplate-Strategie kann zwischen zwei Modi wechseln: einem einfachen gleitenden Durchschnitt-Crossover oder einem Bollinger-Squeeze-Ausbruch. Sie bietet Filter für Handelstage und Handelssitzungen, einen Datumsbereich, ein Nachrichtenfenster und Risikomanagement per ATR oder statischem Risiko/Ertrag.

## Details

- **Einstiegskriterien**:
  - Im Modus `SmaCross` Long, wenn der schnelle SMA den langsamen SMA nach oben kreuzt, und Short beim umgekehrten Kreuz.
  - Im Modus `Squeeze` Einstieg, wenn der Kurs das äußere Bollinger-Band durchbricht und innerhalb des engeren Bands geblieben ist.
- **Long/Short**: Konfigurierbar für Long, Short oder beides mit optionaler Inversion.
- **Ausstiegskriterien**:
  - Stop-Loss und Take-Profit basierend auf ATR oder statischen Prozentsätzen.
  - Täglicher Ausstiegszeitraum und Nachrichtenfenster schließen alle Positionen.
- **Stops**: Trade-spezifischer Stop-Loss und Take-Profit mit Drawdown-Schutz.
- **Standardwerte**:
  - `Length` = 20
  - `WideMultiplier` = 1.5
  - `NarrowMultiplier` = 2
  - `MaxLossPerc` = 0.02
  - `AtrMultiplier` = 1.5
  - `StaticRr` = 2
  - `NewsWindow` = 5
  - `MaxDrawdown` = 0.1
- **Filter**:
  - Kategorie: Modular
  - Richtung: Long & Short
  - Indikatoren: SMA, Bollinger Bands, ATR
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Beliebig
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
