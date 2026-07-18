# Drei-Gleitende-Durchschnitte-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt, wenn ein kurzfristiger gleitender Durchschnitt einen mittelfristigen kreuzt, während beide relativ zu einem langfristigen Durchschnitt ausgerichtet sind.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurzer MA kreuzt den mittleren MA von unten nach oben und der mittlere MA liegt über dem langen MA.
  - **Short**: Kurzer MA kreuzt den mittleren MA von oben nach unten und der mittlere MA liegt unter dem langen MA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegenseitiger Kreuzungspunkt.
- **Stops**: Nein.
- **Standardwerte**:
  - `ShortMa` = 20
  - `MediumMa` = 50
  - `LongMa` = 200
