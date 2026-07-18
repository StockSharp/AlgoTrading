# Elder-Impulssystem-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das Elder Impulse System, das die Richtung eines Exponentiellen Gleitenden Durchschnitts (EMA) mit dem Momentum des MACD-Histogramms kombiniert. Es eröffnet Trades, wenn der bullische oder bärische Impuls bei Kerzen höherer Zeitrahmen nachlässt.

Der Ansatz beobachtet farbcodierte Impulse, die aus dem EMA-Gefälle und der MACD-Histogramm-Dynamik abgeleitet werden:
- **Grün (2)** — EMA steigt und MACD-Histogramm steigt und ist positiv.
- **Rot (1)** — EMA fällt und MACD-Histogramm fällt und ist negativ.
- **Blau (0)** — jeder andere Zustand.

Eine Long-Position wird eröffnet, wenn ein vorheriger bullischer Impuls (grün) nachlässt, während Short-Positionen erscheinen, nachdem ein bärischer Impuls (rot) nachlässt. Entgegengesetzte Positionen werden geschlossen, wenn der entsprechende Impuls erkannt wird.

## Details

- **Einstiegskriterien**: Elder-Impulse-Farbwechsel auf abgeschlossenen Kerzen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Impuls oder Positionsschutz.
- **Stops**: Verwendet `StartProtection` mit standardmäßig 2% Stop und Take-Profit.
- **Standardwerte**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: EMA, MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 4H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
