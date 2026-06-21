# IS-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine einfache Strategie, die eine Long-Position eröffnet, wenn die ausgewählte Quelle dem Long-Auslösewert entspricht, und sie schließt, wenn der Wert zum Gegenteil wechselt. Wenn Leerverkäufe aktiviert sind, eröffnet die Strategie auch eine Short-Position beim entgegengesetzten Signal. Take-Profit und Stop-Loss werden als Prozentsätze des Einstiegspreises angegeben.

## Details

- **Einstiegskriterien**:
  - **Long**: Quelle entspricht dem Long-Wert und vorheriger Wert war unterschiedlich.
  - **Short**: Quelle entspricht dem Short-Wert und vorheriger Wert war unterschiedlich (wenn Shorts aktiviert).
- **Ausstiegskriterien**: Umkehrsignal oder Schutz-Stop.
- **Stops**: Ja, Take-Profit und Stop-Loss als Prozentsätze.
