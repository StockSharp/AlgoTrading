# Momentum-Kerzensignal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis der Kreuzung zwischen Momentum-Werten, die aus den Eröffnungs- und Schlusskursen der Kerzen berechnet werden. Wenn das Momentum des Eröffnungspreises unter das Momentum des Schlusskurses fällt, signalisiert dies steigenden bullischen Druck und die Strategie eröffnet eine Long-Position. Die entgegengesetzte Kreuzung zeigt bärischen Druck an und löst eine Short-Position aus.

Standardmäßig arbeitet die Strategie mit 12-Stunden-Kerzen und einer Momentum-Periode von 12.

## Details

- **Einstiegskriterien**:
  - **Long**: Eröffnungs-Momentum kreuzt unter das Schluss-Momentum.
  - **Short**: Eröffnungs-Momentum kreuzt über das Schluss-Momentum.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Kreuzung.
- **Stops**: Keine.
- **Filter**: Keine.
