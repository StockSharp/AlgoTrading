# Bollinger Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Divergence sucht nach Extremen, bei denen der Preis ein Band durchbricht,
während das gegenüberliegende Band beginnt zu schrumpfen. Diese Divergenz zwischen
Kursmomentum und Volatilität geht oft einer Rückkehr zur Mitte der Range voraus.

Ein Long-Signal erscheint, wenn eine Kerze unterhalb des unteren Bandes schließt,
während das obere Band sich um mindestens einen festgelegten Prozentsatz verengt.
Für Shorts ist das Muster um das obere Band gespiegelt. Positionen zielen auf eine
schnelle Rückkehr zur mittleren Bollinger-Linie mit optionalem festem Take-Profit.

Das Setup funktioniert am besten in seitwärts laufenden Märkten oder nachdem ein
Volatilitätsanstieg beginnt, abzuebben. Der `CandlePercent`-Parameter steuert, wie
stark sich das gegenüberliegende Band zusammenziehen muss, bevor ein Trade erlaubt
wird, und hilft, Whipsaws bei starken Trends zu vermeiden.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Schlusskurs unter unterem Band UND oberes Band zieht sich um `CandlePercent` zusammen.
  - **Short**: Schlusskurs über oberem Band UND unteres Band zieht sich um `CandlePercent` zusammen.
- **Ausstiegskriterien**:
  - Rückkehr zum mittleren Band ODER Take-Profit-Prozentsatz.
- **Stops**: Kein harter Stop; basiert auf Take-Profit oder manuellem Ausstieg.
- **Standardwerte**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `TakeProfit` = 5
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long/Short
  - Indikatoren: Bollinger Bands
  - Komplexität: Einfach
  - Risikolevel: Mittel
