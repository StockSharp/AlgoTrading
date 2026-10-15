# Color Code Overlay Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt auf Basis von Kerzenfarb-Änderungen unter Verwendung einer benutzerdefinierten Farbcode-Berechnung mit festen Pip-basierten Stops.

## Logik
- Erstellt benutzerdefinierte Farbcode-Kerzen aus OHLC-Werten.
- Erkennt Farbwechsel, wenn der Kerzenkörper 1% der Kerzenspanne überschreitet.
- Geht Long bei Rot-zu-Grün, Short bei Grün-zu-Rot entsprechend dem Handelstyp.
- Läuft nur zwischen `StartTime` und `EndTime`.
- Wendet `StopLossPips`- und `TakeProfitPips`-Schutz an.
