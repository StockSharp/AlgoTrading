# Strategie Bullishes Verlassenes Baby
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Das Bullishe Verlassene Baby ist ein seltenes Drei-Kerzen-Muster mit einem Gap-Down-Doji gefolgt von einem Gap-Up.
Diese Formation lässt die mittlere Kerze "verlassen" zurück und geht oft einer starken Aufwärtsumkehr voraus.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 76%. Es funktioniert am besten auf dem Forex-Markt.

Die Strategie kauft bei der Eröffnung der dritten Kerze, sobald diese mit einem Gap über den Doji steigt, und erwartet starke Anschlusskäufe, wenn Shorts eindecken.

Stops liegen knapp unterhalb des Doji-Tiefs, um Verluste gering zu halten, falls die Umkehr nicht standhält.

## Details

- **Einstiegskriterien**: Mustererkennung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `DojiBodyPercent` = 10
  - `StopLossPercent` = 2
    Der Markt handelt rund um die Uhr, daher haben Kerzenspannen fast nie Lücken (im Beispielmonat gibt es kein solches bullisches Muster); die Lücken werden zwischen den Kerzenkörpern gemessen. Der Körper des Doji beträgt höchstens DojiBodyPercent seiner Spanne. Die Strategie kauft nur, zum Schluss der dritten Kerze; der Stop liegt StopLossPercent unter dem Doji-Tief, und die Position schließt, wenn eine Kerze darunter schließt.
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

