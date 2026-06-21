# Strategie zum Schließen von Positionen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Schließt offene Positionen basierend auf Gewinn-, Verlust- oder Zeitregeln. Diese Strategie eröffnet keine neuen Orders.

## Details

- **Einstiegskriterien**: keine, Positionen werden als extern geöffnet angenommen.
- **Ausstiegskriterien**:
  - Gewinn- oder Verlustlimit in Pips wurde erreicht.
  - Das Positionsalter überschreitet das Zeitlimit in Minuten.
  - Die aktuelle Zeit liegt nach der konfigurierten Schließzeit.
- **Stops**: implizite Gewinn- und Verlustschwellen.
- **Filter**: Tageszeit und Haltedauer.
