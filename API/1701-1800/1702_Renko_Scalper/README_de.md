# Renko-Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie versucht kurzfristiges Momentum zu erfassen, indem sie den aktuellen Schlusskurs mit dem vorherigen vergleicht.
Wenn die neueste Kerze höher als die vorherige schließt, eröffnet die Strategie eine Long-Position.
Wenn die neueste Kerze niedriger als die vorherige schließt, eröffnet sie eine Short-Position.

Stops und optionaler Trailing-Stop werden über das integrierte Schutzmodul verwaltet.
Der Ansatz funktioniert auf beiden Marktseiten und stützt sich ausschließlich auf die Kursaction.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close(t) > Close(t-1)`.
  - **Short**: `Close(t) < Close(t-1)`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Schutz-Stops.
- **Stops**: Optionaler Trailing-Stop, Stop-Loss und Take-Profit über `StartProtection`.
- **Standardwerte**:
  - `CandleType` = 1 Minute.
  - `StopLossPercent` = 1.
  - `TakeProfitPercent` = 2.
  - `IsTrailingStop` = true.
- **Filter**:
  - Kategorie: Scalping.
  - Richtung: Beide.
  - Indikatoren: Keine.
  - Stops: Ja.
  - Komplexität: Einfach.
  - Zeitrahmen: Kurzfristig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Hoch.
