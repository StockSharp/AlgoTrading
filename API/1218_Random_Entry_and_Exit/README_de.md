# Zufällige Einstieg- und Ausstieg-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt Zufallszahlen für Ein- und Ausstiege. Für jede abgeschlossene Kerze wird ein Zufallswert zwischen 0 und 1 erzeugt. Liegt der Wert unter dem Einstiegsschwellenwert, wird ein Trade eröffnet. Ein weiterer Zufallswert steuert die Ausstiege. Long- und Short-Trades können separat aktiviert werden.

## Details

- **Einstiegskriterien**: Zufallswert < Einstiegsschwellenwert.
- **Ausstiegskriterien**: Zufallswert < Ausstiegsschwellenwert.
- **Long/Short**: Beide, individuell konfigurierbar.
