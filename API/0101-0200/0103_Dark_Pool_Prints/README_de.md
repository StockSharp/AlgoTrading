# Dark Pool Prints Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dark Pool Prints verfolgt große außerbörsliche Transaktionen, die oft starken Bewegungen vorausgehen, sobald die Aktivität bekannt wird.
Ungewöhnliches Volumen auf dem Tape kann institutionelle Positionierungen signalisieren, die den regulären Markt noch nicht beeinflusst haben.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 46%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie tritt in dieselbe Richtung ein wie starke Dark Pool-Käufe oder -Verkäufe und erwartet eine Fortsetzung, wenn der Rest des Marktes reagiert.

Ein kleiner prozentualer Stop hält das Risiko begrenzt, und Positionen werden geschlossen, wenn der erwartete Impuls ausbleibt.

### Tatsächlich ausgeführte Handelsregeln (C# und Python)

Standard sind 1-Minuten-Kerzen und SMA20. Nach 20 Volumenwerten muss das aktuelle Volumen **strikt größer** als das 1,5-Fache des Mittelwerts der vorigen 19 Werte sein. Nur ohne Position: Kauf bei `Close > Open` und `Close > SMA20`; Verkauf bei `Close < Open` und `Close < SMA20`. Zwischen neuen Einstiegen gelten 500 abgeschlossene Kerzen Wartezeit. Long-Positionen werden bei `Close < SMA20`, Shorts bei `Close > SMA20` geschlossen; **Volumenspitzen und Einstiegspausen verhindern keine Ausstiege**. Ausgeführte Positionen erhalten einen nativen Stop-Loss von 2%, ohne Take-Profit. Die oben genannte Rendite ist durch den BTC/TON-Replay nicht unabhängig belegt.


## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 1 minute
  - `StopLossPercent` = 2%
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

