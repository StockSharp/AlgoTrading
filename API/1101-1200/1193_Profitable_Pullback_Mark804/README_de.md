# Profitable Pullback-Strategie Mark804
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine trendfolgende Pullback-Strategie, die ein Band aus exponentiellen gleitenden Durchschnitten verwendet. Das System sucht nach Preisrücksetzern zur Signal-EMA innerhalb eines bestätigten Trends. Wenn der Preis nach einem Pullback erneut in Trendrichtung schließt, öffnet die Strategie eine Position und schützt sie mit prozentualen Take-Profit- und Stop-Loss-Niveaus.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle EMA > Signal-EMA > Mittlere EMA, optional Mittlere EMA > Langsame EMA, vorheriger Schlusskurs unter der Signal-EMA und aktueller Schlusskurs darüber.
  - **Short**: Schnelle EMA < Signal-EMA < Mittlere EMA, optional Mittlere EMA < Langsame EMA, vorheriger Schlusskurs über der Signal-EMA und aktueller Schlusskurs darunter.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss wird ausgelöst.
- **Stops**: Ja, feste Take-Profit- und Stop-Loss-Prozentsätze.
- **Standardwerte**:
  - Fast EMA Length = 8
  - Signal EMA Length = 21
  - Medium EMA Length = 50
  - Slow EMA Length = 200
  - Take Profit % = 2
  - Stop Loss % = 1
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
