# Multi-Timeframe Bollinger Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Wendet Bollinger Bands sowohl auf einem primären als auch auf einem höheren Zeitrahmen an. Handelt, wenn der Kurs die Bänder des höheren Zeitrahmens durchsticht, und filtert Einstiege optional mit einem langfristigen gleitenden Durchschnitt. Das Ziel ist es, Extremwerte gegen den übergeordneten Trend zu faden.

Die Strategie unterstützt sowohl Long- als auch Short-Positionen. Ein Stop-Loss-Prozentsatz kann für das Risikomanagement aktiviert werden. Der Einsatz mehrerer Zeitrahmen hilft, Trades gegen die dominante Marktstruktur zu vermeiden.

## Details

- **Einstiegskriterien**:
  - **Long**: Schluss unter dem unteren Band des höheren Zeitrahmens und über dem MA-Filter (falls aktiviert).
  - **Short**: Schluss über dem oberen Band des höheren Zeitrahmens und unter dem MA-Filter (falls aktiviert).
- **Ausstiegskriterien**:
  - Long: Preis schließt über dem oberen Band des aktuellen Zeitrahmens.
  - Short: Preis schließt unter dem unteren Band des aktuellen Zeitrahmens.
- **Indikatoren**:
  - Bollinger Bands auf zwei Zeitrahmen (Länge 20, Multiplikator 2)
  - Optionaler EMA-Filter (Periode 200)
- **Stops**: Optionaler Stop-Loss über StartProtection (%-basiert).
- **Standardwerte**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `UseMaFilter` = False
  - `MaLength` = 200
  - `SLPercent` = 2
- **Filter**:
  - Gegentrend mit MTF-Kontext
  - Zeitrahmen: Haupt 5m, MTF 60m standardmäßig
  - Indikatoren: Bollinger Bands, EMA
  - Stops: Optional
  - Komplexität: Moderat
