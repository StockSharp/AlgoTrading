# Disturbed-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Absicherungsstrategie eröffnet gleichzeitig Long- und Short-Marktorders und verwaltet sie anhand des aktuellen Spreads. Sobald sich der Preis um einen Spread gegen eine der Seiten bewegt, wird diese Position geschlossen. Die verbleibende Position zielt dann auf einen Gewinn oder Verlust in Höhe eines konfigurierbaren Vielfachen des Spreads ab.

## Details

- **Einstiegskriterien**:
  - Beim Start werden gleichzeitig Kauf- und Verkaufs-Marktorders platziert.
- **Long/Short**: Beide gleichzeitig.
- **Ausstiegskriterien**:
  - Schließen der Seite, die einen Spread verliert.
  - Schließen der verbleibenden Seite bei `gainMultiplier * spread` Gewinn oder Verlust.
- **Stops**: Implizit über spreadbasierte Niveaus.
- **Filter**: Keine.
