# PROphet Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die PROphet Strategie bewertet Preisbereiche der letzten drei abgeschlossenen Kerzen, um während bestimmter Handelszeiten Signale zu generieren. Eine benutzerdefinierte Funktion kombiniert die Bereiche mit benutzerdefinierten Koeffizienten. Wenn die Funktion positiv ist, öffnet die Strategie eine Position in der entsprechenden Richtung.

Long-Trades verwenden die Koeffizienten `X1..X4` und einen Trailing Stop, der durch `BuyStopPoints` definiert wird. Short-Trades verwenden die Koeffizienten `Y1..Y4` und `SellStopPoints`. Stops werden nachgezogen, wenn sich der Preis um mehr als den Spread plus das Doppelte der Stop-Distanz zugunsten der Position bewegt. Positionen werden nach 18:00 Uhr oder beim Erreichen des Trailing Stops geschlossen.

## Details

- **Einstiegskriterien**
  - **Long**: `Qu(X1,X2,X3,X4) > 0` und aktuelle Stunde zwischen 10 und 18.
  - **Short**: `Qu(Y1,Y2,Y3,Y4) > 0` und aktuelle Stunde zwischen 10 und 18.
- **Ausstiegskriterien**
  - **Long**: Stunde > 18 oder bestes Geldkurs fällt unter den Trailing Stop.
  - **Short**: Stunde > 18 oder bester Briefkurs steigt über den Trailing Stop.
- **Parameter**
  - `EnableBuy` – Öffnen von Long-Positionen erlauben.
  - `EnableSell` – Öffnen von Short-Positionen erlauben.
  - `X1, X2, X3, X4` – Koeffizienten für die Long-Signalfunktion.
  - `Y1, Y2, Y3, Y4` – Koeffizienten für die Short-Signalfunktion.
  - `BuyStopPoints` – Trailing Stop-Abstand in Punkten für Long-Trades.
  - `SellStopPoints` – Trailing Stop-Abstand in Punkten für Short-Trades.
  - `CandleType` – Kerzentyp für Berechnungen (Standard 5 Minuten).
- **Filter**
  - Kategorie: Intraday
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Trailing
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
