# Super Take-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wechselt zwischen Long- und Short-Positionen und erhöht den Take Profit nach jedem Verlusthandel mithilfe eines Martingale-Multiplikators. Der Stop Loss ist fest, während der Take Profit nach einem Gewinnhandel auf den Basiswert zurückgesetzt wird. Durch ständigen Richtungswechsel und Anpassung der Ziele nach Verlusten versucht die Strategie, frühere Drawdowns auszugleichen.

Eine neue Position wird nur eröffnet, wenn keine Position aktiv ist. Der erste Trade ist standardmäßig Long. Jeder nachfolgende Trade wird in der entgegengesetzten Richtung der zuletzt geschlossenen Position eröffnet.

## Details

- **Einstiegskriterien**:
  - **Long**: Keine aktive Position und die zuletzt geschlossene Position war Short oder nicht vorhanden.
  - **Short**: Keine aktive Position und die zuletzt geschlossene Position war Long.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Position schließen, wenn der Preis den dynamischen Take Profit oder den festen Stop Loss erreicht.
- **Stops**: Fester Stop Loss, dynamischer Take Profit mit Martingale nach Verlusthandeln.
- **Standardwerte**:
  - `TakeProfit` = 10
  - `StopLoss` = 15
  - `MartinFactor` = 1.8
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
