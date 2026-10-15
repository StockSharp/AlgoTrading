# Dkoderweb Repainting Issue Fix-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt harmonische Muster mithilfe eines einfachen Zickzack-Ansatzes und handelt, wenn der Preis auf ein Fibonacci-Retracement-Level zurückkehrt. Wenn sich ein bullisches Muster bildet und der Preis in das Einstiegsfenster zurückzieht, öffnet die Strategie eine Long-Position mit vordefinierten Take‑Profit- und Stop‑Loss-Levels. Ein bärisches Muster löst dieselbe Logik in die entgegengesetzte Richtung aus.

## Details

- **Einstiegskriterien**:
  - **Long**: ABCD-harmonisches Muster und Schlusskurs auf oder unterhalb des Fibonacci-Einstiegslevels.
  - **Short**: ABCD-harmonisches Muster und Schlusskurs auf oder oberhalb des Fibonacci-Einstiegslevels.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Der Preis erreicht die Fibonacci-Take‑Profit- oder Stop‑Loss-Levels.
- **Stops**: Ja.
- **Standardwerte**:
  - `TradeSize` = 1
  - `EntryRate` = 0.382
  - `TakeProfitRate` = 0.618
  - `StopLossRate` = -0.618
- **Filter**:
  - Kategorie: Mustererkennung
  - Richtung: Beide
  - Indikatoren: ZigZag
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel

