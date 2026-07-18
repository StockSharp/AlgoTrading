# CC-Trend-2-Abwärtstrend-Short-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Short-Strategie, die verkauft, wenn der vorherige Schluss unter dem dynamischen Fibonacci-Hoch liegt und EMA21 unter EMA55 ist. Schließt, wenn der Preis mit nicht-negativem Gewinn über EMA200 kreuzt oder wenn der vorherige Schluss über das 0.236-Fibonacci-Niveau steigt und kein neues Short-Signal erscheint.

## Details

- **Einstiegskriterien**:
  - Short: vorheriger Schluss unter Fibonacci-Hoch und EMA21 unter EMA55
- **Long/Short**: Short
- **Ausstiegskriterien**:
  - Preis kreuzt EMA200 mit Gewinn nach oben
  - Vorheriger Schluss über 0.236-Fibonacci-Niveau ohne neues Short-Signal
- **Stops**: Keine
- **Standardwerte**:
  - `FibLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Short
  - Indikatoren: EMA, Fibonacci
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
