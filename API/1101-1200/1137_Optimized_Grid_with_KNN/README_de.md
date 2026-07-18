# Optimiertes Grid mit KNN-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Long-Positionen, wenn die schnelle T3-Linie die langsame T3-Linie nach oben kreuzt und die KNN-basierte durchschnittliche Preisänderung positiv ist. Ein- und Ausstiegsschwellen werden anhand der durchschnittlichen Änderung angepasst. Positionen werden geschlossen, sobald die schnelle T3-Linie die langsame nach unten kreuzt und der Preis die Gewinnschwelle überschreitet.

- **Einstiegsbedingungen**: `t3Fast > t3Slow` und `averageChange > 0`
- **Ausstiegsbedingungen**: `t3Fast < t3Slow` und `(close - lastEntryPrice)/lastEntryPrice > adjustedCloseTh`
- **Indikatoren**: T3
