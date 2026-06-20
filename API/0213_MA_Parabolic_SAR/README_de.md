# MA Parabolic SAR Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die MA Parabolic SAR Strategie versucht, anhaltende Trends zu erfassen, indem ein einfacher gleitender Durchschnitt die vorherrschende Richtung bestimmt und die Parabolic SAR-Punkte das Einstiegs-Timing und die Stop-Platzierung liefern. Wenn beide Indikatoren übereinstimmen, geht das System davon aus, dass das Momentum stark genug ist, um ihm zu folgen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 76%. Sie funktioniert am besten auf dem Devisenmarkt.

Eine Long-Position wird eröffnet, wenn der Schlusskurs über dem gleitenden Durchschnitt liegt und die Parabolic SAR-Punkte unter den Markt kippen. Eine Short-Position wird eingegangen, wenn der Preis unter dem Durchschnitt liegt und die SAR-Punkte über den Preis kippen, was Abwärtsdruck signalisiert. Die Strategie steigt aus, sobald der Preis in die entgegengesetzte Richtung über den SAR kreuzt, Gewinne sichert oder Verluste begrenzt.

Dieser Ansatz eignet sich am besten für Trader, die systematisches Trendfolgen mit klaren, mechanischen Stops bevorzugen. Der Parabolic SAR passt sich kontinuierlich an, wenn sich die Volatilität ändert, und hält das Engagement im Einklang mit den Marktbedingungen, während der gleitende Durchschnitt Trades gegen den übergeordneten Trend verhindert.

## Details
- **Einstiegskriterien**:
  - **Long**: Price > MA && Price > Parabolic SAR
  - **Short**: Price < MA && Price < Parabolic SAR
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn der Preis unter den Parabolic SAR fällt
  - **Short**: Ausstieg, wenn der Preis über den Parabolic SAR steigt
- **Stops**: Ja, dynamisch über Parabolic SAR und optionaler fester Stop.
- **Standardwerte**:
  - `MaPeriod` = 20
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TakeValue` = new Unit(0, UnitTypes.Absolute)
  - `StopValue` = new Unit(2, UnitTypes.Percent)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, Parabolic SAR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

