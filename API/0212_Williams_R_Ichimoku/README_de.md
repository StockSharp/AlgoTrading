# Williams R Ichimoku Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieses Setup kombiniert die Momentum-Extreme von Williams %R mit der Trendstruktur, die durch die Ichimoku Cloud definiert wird. Die Idee ist, starken Bewegungen nur dann beizutreten, wenn der Preis auf der günstigen Seite der Cloud liegt und die kurzfristigen Linien die Richtung bestätigen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 73%. Sie funktioniert am besten auf dem Kryptomarkt.

Eine Long-Gelegenheit entsteht, wenn der Oszillator unter -80 fällt, während der Preis über der Cloud liegt und Tenkan-sen über Kijun-sen kreuzt. Ein Short-Signal entsteht, wenn %R über -20 steigt, der Preis unter der Cloud liegt und Tenkan-sen unter Kijun-sen. Die Position bleibt offen, bis der Preis die entgegengesetzte Seite der Cloud überkreuzt.

Da die Methode auf mehrere Bestätigungen wartet, eignet sie sich für Trader, die klare Trendfilter gegenüber schnellen Umkehrungen bevorzugen. Dynamische Stops werden um den Kijun-sen gesetzt, sodass das Risiko sich mit der zugrunde liegenden Trendstärke anpasst.

## Details
- **Einstiegskriterien**:
  - **Long**: %R < -80 && price above Ichimoku cloud and Tenkan-sen > Kijun-sen
  - **Short**: %R > -20 && price below Ichimoku cloud and Tenkan-sen < Kijun-sen
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn der Preis unter die Cloud fällt
  - **Short**: Ausstieg, wenn der Preis über die Cloud steigt
- **Stops**: Ja.
- **Standardwerte**:
  - `WilliamsRPeriod` = 14
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: Williams R Ichimoku
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

