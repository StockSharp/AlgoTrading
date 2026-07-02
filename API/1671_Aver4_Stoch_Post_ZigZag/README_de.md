# Strategie Aver4 Stoch Post ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert vier Stoch-Oszillatoren über mehrere Zeithorizonte und einen einfachen ZigZag-Pivot-Detektor. Der gemittelte Stoch leitet die überkauften/überverkauften Niveaus, während der ZigZag Swing-Hochs und -Tiefs bestätigt. Käufe erfolgen, wenn der gemittelte Stoch unter das überverkaufte Niveau fällt und ein neues ZigZag-Tief entsteht. Verkäufe erfolgen, wenn der gemittelte Stoch über das überkaufte Niveau steigt und ein neues ZigZag-Hoch entsteht. Bestehende entgegengesetzte Positionen werden bei Signalumkehr geschlossen.

## Details
- **Einstiegskriterien**: Gemittelter Stoch kreuzt überverkaufte/überkaufte Zonen mit passendem ZigZag-Pivot.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: StartProtection 2%/2% (Standard).
- **Standardwerte**:
  - `ShortLength` = 26
  - `MidLength1` = 72
  - `MidLength2` = 144
  - `LongLength` = 288
  - `ZigZagDepth` = 14
  - `Oversold` = 5
  - `Overbought` = 95
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Stochastic, ZigZag
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
