# Volume EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie handelt auf Basis von Volumenspitzen und dem Commodity Channel Index (CCI). Sie eröffnet Positionen zu Beginn einer neuen Stunde, wenn das Volumen der vorherigen Kerze das der Kerze davor um einen konfigurierbaren Faktor übertrifft. CCI-Werte müssen in bestimmte Bänder fallen, um das Signal zu bestätigen.

## Regeln
- Es ist immer nur eine Position gleichzeitig offen.
- Zu Beginn jeder Stunde:
  - **Long-Einstieg** wenn:
    - Die vorherige Kerze bullisch ist.
    - Vorheriges Volumen > voriges Volumen × `Factor`.
    - CCI liegt zwischen `CciLevel1` und `CciLevel2`.
  - **Short-Einstieg** wenn:
    - Die vorherige Kerze bärisch ist.
    - Vorheriges Volumen > voriges Volumen × `Factor`.
    - CCI liegt zwischen `CciLevel4` und `CciLevel3`.
- Ein Trailing-Stop von `TrailingStop` Preisschritten schützt Gewinne.
- Alle Positionen werden geschlossen, wenn die Stunde gleich 23 ist.

## Parameter
- `Factor` – Volumen-Multiplikator-Schwellenwert.
- `TrailingStop` – Trailing-Distanz in Preisschritten.
- `CciLevel1` / `CciLevel2` – CCI-Grenzen für Long-Trades.
- `CciLevel3` / `CciLevel4` – CCI-Grenzen für Short-Trades.
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen.
