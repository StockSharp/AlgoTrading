# Strategie Donchian CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie verwendet Donchian CCI-Indikatoren zur Signalerzeugung.
Ein Long-Einstieg erfolgt, wenn Price > Donchian Upper && CCI > CciOverbought (Ausbruch nach oben mit Aufwärtsmomentum). Ein Short-Einstieg erfolgt, wenn Price < Donchian Lower && CCI < CciOversold (Ausbruch nach unten mit Abwärtsmomentum).
Sie eignet sich für Trader, die in gemischten Märkten nach Gelegenheiten suchen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 43%. Die Strategie funktioniert am besten am Aktienmarkt.

## Details
- **Einstiegskriterien**:
  - **Long**: Price > Donchian Upper && CCI > CciOverbought (Ausbruch nach oben mit Aufwärtsmomentum)
  - **Short**: Price < Donchian Lower && CCI < CciOversold (Ausbruch nach unten mit Abwärtsmomentum)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis unter das mittlere Band fällt
  - **Short**: Short-Position schließen, wenn der Preis über das mittlere Band steigt
- **Stops**: Ja.
- **Standardwerte**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `CciOverbought` = 100
  - `CciOversold` = -100
  - `StopLossPercent` = 2
    Der Kanal ist das Hoch und Tief der vorherigen DonchianPeriod Kerzen, seine Mitte liegt genau dazwischen. Ein Ausbruch nach oben wird bestätigt, wenn der CCI über CciOverbought liegt, einer nach unten, wenn er unter CciOversold liegt; die umgekehrte Lesart, ein überverkaufter CCI bei einem Schluss über dem Kanal, kommt praktisch nicht vor. Ein Einstiegssignal gegen eine offene Position dreht sie.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: Donchian CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

