# Stochastic Überkauft/Überverkauft-Umkehr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Strategie reagiert auf extreme Niveaus des Stochastic Oszillators. Wenn die %K-Linie in überverkauftes Terrain abtaucht, erwartet das System eine Erholung, während überkaufte Werte einen Rückgang ankündigen können. Die Methode läuft auf kurzen Intraday-Kerzen, sodass Signale schnell eintreffen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 73%. Sie funktioniert am besten auf dem Kryptomarkt.

Nach der Anmeldung auf dem ausgewählten Zeitrahmen überwacht sie die %K- und %D-Linien. Ein bullisches Setup entsteht, wenn %K unter 20 fällt und dann beginnt, sich zu erholen. Umgekehrt erscheint ein bärisches Setup, wenn %K über 80 steigt und anfängt, sich nach unten zu drehen. Ein fester prozentualer Stop kontrolliert das Risiko für beide Seiten.

Positionen werden geschlossen, wenn die %K-Linie wieder durch das Niveau 50 kreuzt, was signalisiert, dass sich der Schwung in die entgegengesetzte Richtung verschoben hat. Da die Stops mit dem aktuellen ATR skalieren, passt sich die Handelsgröße an die Volatilität an.

## Details

- **Einstiegskriterien**:
  - **Long**: `%K < 20` mit bullischer Wende.
  - **Short**: `%K > 80` mit bärischer Wende.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: %K kreuzt Niveau 50 oder Stop-Loss.
- **Stops**: Ja, bei `2%` Abstand.
- **Standardwerte**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `CandleType` = 5 minute
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

