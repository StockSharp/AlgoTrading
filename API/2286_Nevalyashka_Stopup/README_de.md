# Nevalyashka Stopup-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wechselt die Positionsrichtung nach jedem Trade und imitiert das „Nevalyashka"-Spielzeug, das die Seiten wechselt. Sie verwendet einen Martingale-Ansatz: Wenn ein Trade mit Verlust geschlossen wird, werden die Stop-Loss- und Take-Profit-Abstände für den nächsten Trade mit einem Koeffizienten multipliziert. Nach einem profitablen Trade werden die Abstände auf ihre Basiswerte zurückgesetzt, und die Strategie kann optional den Handel einstellen.

Die anfängliche Richtung ist Short. Jedes Mal, wenn eine Position geschlossen wird, wird die neue Position in die entgegengesetzte Richtung mit dem vorkonfigurierten Volumen geöffnet.

## Details

- **Einstiegskriterien**:
  - Der erste Trade verkauft zum Marktpreis.
  - Nachfolgende Trades gehen immer in die entgegengesetzte Richtung des letzten geschlossenen Trades.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Die Position wird geschlossen, wenn der Kurs den Take-Profit- oder Stop-Loss-Abstand vom Einstieg erreicht.
- **Stops**: Ja, feste Stop-Loss- und Take-Profit-Abstände in Punkten. Abstände wachsen nach Verlusten um den Martingale-Koeffizienten.
- **Standardwerte**:
  - `StopLossPoints` = 150
  - `TakeProfitPoints` = 50
  - `OrderVolume` = 0.1
  - `MartingaleCoeff` = 1.5
  - `StopAfterProfit` = false
- **Filter**:
  - Kategorie: Umkehr / Martingale
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
