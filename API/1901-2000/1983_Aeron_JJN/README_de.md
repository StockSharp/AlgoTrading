# Aeron JJN Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Logik des originalen Aeron JJN Expert Advisors. Sie beobachtet eine starke Umkehrkerze und platziert eine Stop-Order am Eröffnungskurs der letzten entgegengesetzten Kerze. Stop und Ziel werden einen ATR entfernt gesetzt, und ein optionaler Trailing-Stop schützt offene Positionen.

Tests zeigen, dass die Idee am besten bei wichtigen Forex-Paaren mit 1-Minuten-Kerzen funktioniert.

Eine Long-Stop-Order wird platziert, wenn die vorherige Kerze bärisch mit einem Körper größer als **DojiDiff1** ist und die aktuelle Kerze bullisch, aber noch unterhalb der letzten signifikanten bärischen Eröffnung liegt. Eine Short-Stop-Order verwendet die Spiegelbedingungen. Ausstehende Orders werden nach **ResetTime** Minuten entfernt, wenn sie nicht ausgeführt wurden.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorherige Kerze bärisch, aktuelle Kerze bullisch und schließt unterhalb der letzten bärischen Eröffnung.
  - **Short**: Vorherige Kerze bullisch, aktuelle Kerze bärisch und schließt oberhalb der letzten bullischen Eröffnung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - ATR-basierter Stop-Loss und Take-Profit.
  - Optionaler Trailing-Stop in Pips.
- **Stops**: Ja, anfänglicher Stop und Ziel basierend auf ATR plus optionalem Trailing.
- **Filter**:
  - Ausstehende Orders laufen nach der konfigurierten Zeit ab.

## Parameter

- `AtrPeriod` – ATR-Berechnungsperiode.
- `DojiDiff1` – Körpergrößenschwelle für die vorherige Kerze.
- `DojiDiff2` – Körpergrößenschwelle bei der Suche nach der letzten entgegengesetzten Kerze.
- `TrailSl` – Trailing-Stop aktivieren.
- `TrailPips` – Trailing-Abstand in Pips.
- `ResetTime` – Minuten bis zur Stornierung von Stop-Orders.
- `CandleType` – Arbeitszeitrahmen.
