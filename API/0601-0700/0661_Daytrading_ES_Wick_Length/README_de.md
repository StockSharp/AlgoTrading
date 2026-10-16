# Daytrading ES Docht-Länge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn die gesamte Dochtlänge einer Kerze ihren gleitenden Durchschnitt plus einen Versatz überschreitet, und schließt die Position nach einer festen Anzahl von Bars.

### Prüffähige Signalregeln (C# und Python)

Je **abgeschlossener 1-Minuten-Kerze** ist die gesamte Dochtlänge `W = max(0, High − max(Open, Close)) + max(0, min(Open, Close) − Low)`. Die **vorangehenden 20 Kerzen**, ohne die aktuelle Signalkerze, liefern `VWMA(W) = sum(Wᵢ × Volumeᵢ) / sum(Volumeᵢ)`; bei insgesamt null Volumen wird der einfache Durchschnitt verwendet. `MaType=Simple` wählt stets den ungegewichteten Mittelwert. `MaOffset=10` bedeutet **10 minimale Preisticks**, Schwelle `VWMA(W) + 10 × Security.PriceStep`. Nach 20 Kerzen Aufwärmphase wird **nur Long** aus Flat bei `W > Schwelle` gekauft. Nach **18 weiteren abgeschlossenen Kerzen** wird die verbleibende Long-Position vollständig verkauft. Keine Shorts, kein Stop-Loss oder Take-Profit. Standard: `MaLength=20`, `MaType=VolumeWeighted`, `MaOffset=10`, `HoldPeriods=18`, `CandleType=1 Minute`.



## Details

- **Einstiegskriterien**: Gesamte Dochtlänge größer als gleitender Durchschnitt mit Versatz.
- **Ausstiegskriterien**: Position wird nach dem Halten von `Hold periods` Bars geschlossen.
- **Long/Short**: Nur Long.
- **Stops**: Keine.
- **Standardwerte**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = 1-Minuten-Kerzen
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Long
  - Indikatoren: Moving Average, Dochtlänge
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
