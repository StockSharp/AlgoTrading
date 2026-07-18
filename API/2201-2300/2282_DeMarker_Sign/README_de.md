# DeMarker Sign-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den DeMarker-Oszillator zur Erkennung potenzieller Trendumkehrungen. Bei jeder abgeschlossenen Kerze (standardmäßig 4-Stunden-Zeitrahmen) wird der DeMarker-Wert mit konfigurierbaren oberen und unteren Schwellenwerten verglichen. Wenn der Oszillator über den unteren Schwellenwert (standardmäßig 0.3) steigt, eröffnet die Strategie eine Long-Position und schließt eine vorhandene Short-Position. Wenn der Oszillator unter den oberen Schwellenwert (standardmäßig 0.7) fällt, eröffnet sie eine Short-Position und schließt eine vorhandene Long-Position. Positionen werden gehalten, bis ein entgegengesetztes Signal erscheint.

## Details

- **Einstiegskriterien**:
  - **Long**: DeMarker kreuzt nach oben durch das untere Niveau.
  - **Short**: DeMarker kreuzt nach unten durch das obere Niveau.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Standardmäßig keine.
- **Filter**: Keine.
