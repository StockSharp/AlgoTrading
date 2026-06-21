# Kauf/Verkauf Renko-basierte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Renko-Blöcke, die mit einer ATR-basierten Größe erstellt werden. Eine Long-Position wird eröffnet, wenn der Renko-Schlusskurs seinen Eröffnungskurs nach oben kreuzt. Eine Short-Position wird eröffnet, wenn der Schlusskurs den Eröffnungskurs nach unten kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs kreuzt den Eröffnungskurs nach oben.
  - **Short**: Schlusskurs kreuzt den Eröffnungskurs nach unten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - ATR-Länge 10.
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Renko
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Nicht zeitbasiert
