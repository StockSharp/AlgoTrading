# Ausbruch-Balken-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt Trendumkehrungen mithilfe des Parabolic SAR Indikators. Sie wartet auf eine konfigurierbare Anzahl negativer Umkehrungen, bevor sie in die neue Trendrichtung eintritt. Die Abstände für Stop-Loss und Take-Profit werden entweder in Pips oder als Prozentsatz des Einstiegspreises gemessen.

## Parameter

- **Reversal Mode** – Wahl zwischen pip-basierten oder prozentbasierten Abstandsberechnungen.
- **Delta** – minimale Preisbewegung, die zwischen Umkehrungen erforderlich ist.
- **Negative Signals** – wie viele fehlgeschlagene Umkehrungen auftreten müssen, bevor ein Trade eröffnet werden kann.
- **Stop Loss** – Verlustschutzabstand vom Einstiegspreis.
- **Take Profit** – Gewinnzielabstand vom Einstiegspreis.
- **Candle Type** – Kerzenserie, die für Indikatorberechnungen verwendet wird.

## Logik

1. Kerzendaten abonnieren und Parabolic SAR berechnen.
2. Wenn der Parabolic SAR die Richtung wechselt und der Preis sich um mindestens *Delta* bewegt, den Umkehrpreis speichern.
3. Negative Umkehrungen zählen, bei denen sich der Preis gegen den vorherigen Trend bewegte.
4. Sobald der Zähler den Wert **Negative Signals** erreicht, eine Position in der neuen Trendrichtung eröffnen.
5. Jede Kerze prüft Stop-Loss- und Take-Profit-Niveaus anhand des gewählten **Reversal Mode**.
6. Positionen werden bei entgegengesetzter Trendänderung oder bei Erreichen der Risikolimits geschlossen.

Die Strategie eignet sich für trendfolgende Ausbruchssysteme und kann durch Anpassen von Delta, Stop-Loss und Take-Profit-Abständen optimiert werden.
