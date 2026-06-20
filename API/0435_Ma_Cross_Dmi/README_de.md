# MA Cross + DMI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt eine Kreuzung von schnellen und langsamen exponentiellen gleitenden Durchschnitten nur dann, wenn der Directional Movement Index die Trendstärke bestätigt. Indem auf das Dominieren von +DI oder -DI gewartet wird, während der ADX über einem Schlüsselniveau steigt, filtert das System schwache Kreuzungen heraus.

Diese Strategie kann Long- oder Short-Positionen eingehen und steigt bei entgegengesetzten Kreuzungen aus. Die ADX-Filterung hilft der Methode, Seitwärtsphasen zu vermeiden, in denen gleitende Durchschnitte häufig Fehlsignale erzeugen.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle EMA kreuzt über die langsame EMA, +DI > -DI und ADX über dem Schlüsselniveau.
  - **Short**: Schnelle EMA kreuzt unter die langsame EMA, -DI > +DI und ADX über dem Schlüsselniveau.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzung oder manueller Stop.
- **Indikatoren**:
  - Zwei EMAs (Perioden 10 und 20)
  - Directional Movement Index (Länge 14, ADX-Glättung 14)
- **Stops**: Standardmäßig keine; StartProtection kann verwendet werden.
- **Standardwerte**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **Filter**:
  - Trendfolge
  - Funktioniert auf Intraday- bis Swing-Zeitrahmen
  - Indikatoren: EMA, DMI
  - Stops: Optional
  - Komplexität: Grundlegend
