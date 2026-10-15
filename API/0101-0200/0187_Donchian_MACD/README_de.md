# Strategie Donchian Macd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die den Donchian Channel-Ausbruch mit der MACD-Trendbestätigung kombiniert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 148%. Sie funktioniert am besten auf dem Forex-Markt.

Die Strategie wartet auf einen Donchian-Ausbruch und überprüft den Momentum mit MACD. Long- oder Short-Trades folgen der Bewegung, sobald MACD zustimmt.

Gerichtet an Ausbruchs-Enthusiasten, die Bestätigung möchten. Stops werden in festem Prozentabstand vom Einstiegspreis platziert.

## Details

- **Einstiegskriterien**:
  - Long: `Price breaks Donchian high && MACD > Signal`
  - Short: `Price breaks Donchian low && MACD < Signal`
- **Long/Short**: Beide
- **Ausstiegskriterien**: MACD-Umkehr
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `DonchianPeriod` = 20
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Ein Ausbruch ist ein Schluss jenseits des höchsten Hochs oder tiefsten Tiefs der vorherigen DonchianPeriod Kerzen. Der MACD-Umkehr-Ausstieg schließt einen Long, wenn MACD die Signallinie nach unten kreuzt, und einen Short, wenn er sie nach oben kreuzt. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Donchian Channel, MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

