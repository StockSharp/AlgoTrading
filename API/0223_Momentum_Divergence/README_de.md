# Momentum-Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Momentum-Divergenz-Strategie vergleicht Momentum-Werte mit der Preisrichtung, um frühe Anzeichen einer Umkehr zu erkennen. Divergenzen treten auf, wenn der Preis ein neues Extrem erreicht, der Momentum-Indikator dies jedoch nicht bestätigt, was auf nachlassende Stärke hindeutet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 106%. Sie funktioniert am besten am Aktienmarkt.

Eine bullische Konfiguration entsteht, wenn der Preis ein tieferes Tief verzeichnet, während der Momentum-Oszillator ein höheres Tief ausgibt. Eine bärische Konfiguration bildet sich, wenn der Preis auf ein höheres Hoch schiebt, aber das Momentum nicht folgt. Positionen werden geschlossen, wenn das Momentum zurück durch null kreuzt oder die Divergenz ungültig wird.

Dieser Ansatz spricht Trader an, die Wendepunkte antizipieren möchten, anstatt Trends zu folgen. Stops werden verwendet, um das Risiko zu kontrollieren, falls der Markt weiter gegen das Divergenzsignal läuft.

## Details
- **Einstiegskriterien**:
  - **Long**: Preis macht tieferes Tief && Momentum zeigt höheres Tief
  - **Short**: Preis macht höheres Hoch && Momentum zeigt niedrigeres Hoch
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn Momentum unter null kreuzt
  - **Short**: Ausstieg, wenn Momentum über null kreuzt
- **Stops**: Ja, fester Stop-Loss.
- **Standardwerte**:
  - `MomentumPeriod` = 14
  - `MaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Momentum
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
