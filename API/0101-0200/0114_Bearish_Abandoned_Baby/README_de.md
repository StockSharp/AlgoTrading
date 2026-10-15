# Strategie Bärisches Verlassenes Baby
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Das Bärische Verlassene Baby spiegelt die bullishe Version wider, signalisiert jedoch ein potenzielles Top.
Es zeigt einen Gap-Up-Doji gefolgt von einem Gap-Down, der die mittlere Kerze isoliert oberhalb der vorherigen Range zurücklässt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 79%. Es funktioniert am besten auf dem Aktienmarkt.

Die Strategie geht short, wenn die dritte Kerze mit einem Gap unter den Doji fällt, um vom abrupten Stimmungswechsel zu profitieren.

Das Risiko ist durch einen Stop knapp oberhalb des Doji-Hochs begrenzt, falls der Kurs sich erholt.

## Details

- **Einstiegskriterien**: Mustererkennung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `DojiBodyPercent` = 10
  - `StopLossPercent` = 2
    Der Markt handelt rund um die Uhr, daher haben Kerzenspannen fast nie Lücken (im Beispielmonat gibt es höchstens ein solches bärisches Muster); die Lücken werden zwischen den Kerzenkörpern gemessen. Der Körper des Doji beträgt höchstens DojiBodyPercent seiner Spanne. Die Strategie verkauft nur, zum Schluss der dritten Kerze; der Stop liegt StopLossPercent über dem Doji-Hoch, und die Position schließt, wenn eine Kerze darüber schließt.
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

