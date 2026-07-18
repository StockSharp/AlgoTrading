# Liquiditäts-Swings-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Liquiditäts-Swings-Strategie verfolgt aktuelle Pivot-Hochs und -Tiefs, um Widerstand und Unterstützung zu definieren. Ein Long-Trade tritt auf, wenn das Tief die Unterstützung nach oben kreuzt, während der Schlusskurs unter dem Widerstand bleibt. Ein Short-Trade wird ausgelöst, wenn das Hoch den Widerstand nach unten kreuzt, während der Schlusskurs über der Unterstützung bleibt. Das Risikomanagement verwendet einen Stop-Loss unterhalb/oberhalb des Niveaus mit einem Puffer und einen Take-Profit beim doppelten dieser Distanz, was ein 1:2-Risiko-Ertrags-Verhältnis ergibt.

## Details

- **Einstiegskriterien**:
  - **Long**: Tief kreuzt Unterstützung nach oben und Schlusskurs < Widerstand.
  - **Short**: Hoch kreuzt Widerstand nach unten und Schlusskurs > Unterstützung.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Stop-Loss am Niveau oder mit Puffer.
  - Take-Profit bei 2× Risikoabstand.
- **Stops**: Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `Lookback` = 5
  - `StopLossBuffer` = 0.5
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Pivot highs/lows
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: 1h (Standard)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
