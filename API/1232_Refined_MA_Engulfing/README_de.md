# Verfeinerte MA + Engulfing-Strategie (M5 + Bestätigter Strukturbruch)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verfeinerte MA + Engulfing kombiniert zwei einfache gleitende Durchschnitte, Engulfing-Kerzen und eine Strukturbruch-Bestätigung. Ein Trade wird platziert, wenn mindestens zwei Confluence-Faktoren übereinstimmen und die Abkühlzeit verstrichen ist.

## Details

- **Einstiegskriterien**: Nach einem bestätigten bullischen oder bärischen Strukturbruch, Preis über oder unter beiden SMAs, und mindestens zwei von vier Confluences (Engulfing, Strukturbruch, MA-Filter, Fib-Platzhalter) mit erfüllter Abkühlzeit.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Keine.
- **Stops**: Nein.
- **Standardwerte**:
  - `Ma1Length` = 66
  - `Ma2Length` = 85
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: SMA, Engulfing, Structure Break
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: 5-minute
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
