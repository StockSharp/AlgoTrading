# NRTR-Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie folgt Markttrends mithilfe des **NRTR (Nick R's Trend Reverse)**-Indikators. Der Algorithmus berechnet ein Trailing-Stop-Level, das aus der durchschnittlichen Spanne der letzten Kerzen abgeleitet wird. Wenn der Preis das Trailing-Level bricht, kehrt sich die Position in Richtung des Ausbruchs um. Das System arbeitet auf beiden Long- und Short-Seiten und enthält optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen.

Die NRTR-Länge definiert die Empfindlichkeit des Trailing-Stops: Eine kürzere Periode reagiert schneller, kann aber zu Fehlsignalen führen, während eine längere Periode Rauschen herausfiltert. Ein zusätzlicher Stellenverschiebungsparameter passt den Indikator an Instrumente mit unterschiedlichen Preisskalen an. Die Strategie abonniert Kerzen des gewählten Zeitrahmens und berechnet die NRTR-Werte auf jeder fertigen Bar.

## Details

- **Einstiegslogik**:
  - **Long**: Preis kreuzt nach einem Abwärtstrend über das NRTR-Level.
  - **Short**: Preis kreuzt nach einem Aufwärtstrend unter das NRTR-Level.
- **Ausstiegslogik**:
  - Positionen werden bei einem entgegengesetzten Ausbruch umgekehrt.
- **Stops**: Optionaler Stop-Loss und Take-Profit über `StartProtection`.
- **Standardwerte**:
  - `Length` = 10
  - `DigitsShift` = 0
  - `TakeProfit` = 2000 Punkte
  - `StopLoss` = 1000 Punkte
  - `CandleType` = 1-Stunden-Kerzen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: NRTR, ATR
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Flexibel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
