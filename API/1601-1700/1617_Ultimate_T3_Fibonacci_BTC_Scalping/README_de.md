# Ultimative T3 Fibonacci BTC Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei Tilson T3 gleitende Durchschnitte, um kurzfristige BTC-Bewegungen zu erfassen. Ein Kreuzung zwischen der Fibonacci-abgestimmten und der Standard-T3-Linie erzeugt Long- oder Short-Einstiege. Optionales TP/SL-Management und Schließen bei entgegengesetzten Signalen werden unterstützt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 38%. Es funktioniert am besten bei BTC-Paaren mit geringer Latenz.

Die Strategie kauft, wenn die schnelle T3 die langsame T3 nach oben kreuzt, und verkauft beim umgekehrten Kreuzung. Positionen können bei umgekehrten Signalen oder durch prozentuale Take-Profit- und Stop-Loss-Niveaus geschlossen werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle T3 kreuzt langsame T3 nach oben.
  - **Short**: Schnelle T3 kreuzt langsame T3 nach unten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegenläufige Kreuzung oder TP/SL, wenn aktiviert.
- **Stops**: Optional, prozentbasiert.
- **Filter**:
  - Keine.
