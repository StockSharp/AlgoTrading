# Sofortausführungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet sofort eine einzelne Position bei der ersten abgeschlossenen Kerze und verwaltet sie mit einfachen Gewinn- und Risikoregeln. Die Positionsrichtung ist über Parameter wählbar. Sobald ein Handel eröffnet ist, verfolgt der Algorithmus Gewinn und Verlust und kann den Preis trailend begleiten, um Gewinne zu schützen.

Die Logik reproduziert das Verhalten des ursprünglichen MQL-Skripts, das die sofortige Ausführung von Marktaufträgen mit optionalen Take-Profit-, Stop-Loss- und Trailing-Stop-Werten ermöglichte.

## Details

- **Einstiegskriterien**: Eröffnet eine Marktposition bei der ersten abgeschlossenen Kerze nach dem Start. Die Richtung wird durch den Parameter `Direction` definiert.
- **Long/Short**: Beide Seiten unterstützt.
- **Ausstiegskriterien**:
  - Take-Profit erreicht.
  - Stop-Loss erreicht.
  - Trailing-Stop aktiviert und Kurs trifft das Trailing-Niveau.
- **Stops**: Take-Profit, Stop-Loss und Trailing-Stop sind verfügbar.
- **Standardwerte**:
  - `TakeProfit` = 70 Preiseinheiten.
  - `StopLoss` = 0 (deaktiviert).
  - `TrailingStart` = 5 Preiseinheiten.
  - `TrailingSize` = 5 Preiseinheiten.
- **Filter**:
  - Kategorie: Hilfsprogramm
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
