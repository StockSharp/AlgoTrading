# NQ Phantom Scalper Pro Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

VWAP-Band-Ausbruch-Strategie mit optionalen Volumen- und Trendfiltern.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis schließt über dem oberen VWAP-Band bei bestätigendem Volumen.
  - **Short**: Preis schließt unter dem unteren VWAP-Band bei bestätigendem Volumen.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Preis kreuzt zurück durch VWAP oder der ATR-Stop wird ausgelöst.
- **Stops**: ATR-basiert
- **Standardwerte**:
  - `Band #1 Mult` = 1.0
  - `Band #2 Mult` = 2.0
  - `ATR Length` = 14
  - `ATR Stop Mult` = 1.0
  - `Volume SMA Period` = 20
  - `Volume Spike Mult` = 1.5
  - `Trend EMA Length` = 50
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: VWAP, ATR, EMA, SMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
