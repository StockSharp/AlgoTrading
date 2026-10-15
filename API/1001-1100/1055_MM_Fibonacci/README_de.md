# MM Fibonacci-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet Murrey Math Fibonacci-Niveaus und handelt Ausbrüche. Sie kauft, wenn der Preis das 100%-Niveau in einem Aufwärtskontext nach oben durchbricht, und verkauft, wenn der Preis in einem Abwärtskontext unter das 0%-Niveau fällt. Positionen werden geschlossen, wenn der Preis das 50%-Niveau gegen die Handelsrichtung kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Preis schließt oberhalb des 100%-Niveaus, während das jüngste Extrem ein Hoch war.
  - **Short**: Der Preis schließt unterhalb des 0%-Niveaus, während das jüngste Extrem ein Tief war.
- **Ausstiegskriterien**:
  - **Long**: Der Preis fällt unter das 50%-Niveau.
  - **Short**: Der Preis steigt über das 50%-Niveau.
- **Indikatoren**: Highest, Lowest.
- **Long/Short**: Beide.
- **Stops**: Nein.
