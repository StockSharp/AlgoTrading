# DNSE VN301 SMA & EMA Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt den VN301-Index anhand eines Kreuzungssignals zwischen einem 15-Perioden-EMA und einem 60-Perioden-SMA. Sie schließt Positionen vor Ende der Handelssitzung und wendet einen einfachen prozentualen Stop zur Verlustbegrenzung an.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 20%. Es funktioniert am besten bei VN30-Futures.

Eine Long-Position wird eröffnet, wenn EMA15 den SMA60 nach oben kreuzt und der Kurs über dem EMA liegt. Eine Short-Position öffnet sich beim umgekehrten Kreuzung. Positionen werden bei umgekehrten Signalen, Sitzungsschluss oder wenn der Kurs über das konfigurierte Verlustlimit hinaus gegen den Einstieg läuft, geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: EMA15 kreuzt SMA60 nach oben und Kurs >= EMA15 vor Handelsschluss.
  - **Short**: EMA15 kreuzt SMA60 nach unten und Kurs <= EMA15 vor Handelsschluss.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegenläufige Kreuzung, maximaler Verlust oder Sitzungsschluss.
- **Stops**: Ja, prozentbasierter maximaler Verlust.
- **Filter**:
  - Sitzungsschlusszeit.
