# Strategie zur Lexikalischen Dichte in Regulatorischen Berichten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Faktorstrategie untersucht die in Regulierungsdokumenten verwendete Sprache, um die zukünftige Aktienperformance einzuschätzen. Die lexikalische Dichte wird als Anteil einzigartiger Begriffe im jüngsten Bericht gemessen. Dichte Berichte deuten auf informationsreiche Offenlegungen hin, die oft stärkeren Renditen vorausgehen, während knappe Formulierungen Schwächen verschleiern können.

Jedes Quartal wird das Universum nach lexikalischer Dichte sortiert. Das höchste Quintil wird long gehalten und das niedrigste Quintil wird geshortet, mit gleichgewichteten Positionen. Das Rebalancing erfolgt in den ersten drei Handelstagen des Februar, Mai, August und November, und die Positionen bleiben zwischen den Reviews ohne Stop-Losses offen.

Backtests auf breiten US-Aktien zeigen, dass der Faktor eine stetige Prämie bei moderatem Umsatz liefert, was ihn zu einem nützlichen Bestandteil von Multi-Faktor-Portfolios macht.

## Details

- **Einstiegskriterien**: Vierteljährliche Sortierung nach lexikalischer Dichte; Long oberstes Quintil,
  Short unterstes Quintil
- **Long/Short**: Beide
- **Ausstiegskriterien**: Nächstes Rebalancing
- **Stops**: Nein
- **Standardwerte**:
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Beide
  - Indikatoren: Textanalyse
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mehrmonatig
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
