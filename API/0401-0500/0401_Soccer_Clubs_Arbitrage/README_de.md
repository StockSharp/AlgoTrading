# Fußballclub-Arbitrage-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie vergleicht die Schlusskurse abgeschlossener Kerzen zweier verwandter Instrumente. Sie berechnet die relative Prämie als `Hauptinstrument / zweites Instrument - 1` und handelt beide Seiten, sobald die absolute Prämie den Einstiegsschwellenwert überschreitet.

Ist das Hauptinstrument teurer, verkauft die Strategie dieses und kauft das zweite Instrument mit derselben Stückzahl. Ist das zweite Instrument teurer, werden die Richtungen umgekehrt. Beide Positionen werden geschlossen, sobald die absolute Prämie unter den Ausstiegsschwellenwert fällt.

## Details

- **Daten**: Abgeschlossene Kerzen des Hauptwerts und von `Security2Id`; der Standardzeitrahmen beträgt fünf Minuten.
- **Einstieg**: Gegenläufige Market-Orders mit gleicher Stückzahl, wenn die Prämie in einer Richtung `EntryThreshold` überschreitet.
- **Ausstieg**: Glattstellung der tatsächlichen Position jeder Seite, wenn die absolute Prämie unter `ExitThreshold` liegt.
- **Pause**: Nach Einstieg, Ausstieg oder Umkehr wartet die Strategie `CooldownBars` gepaarte Kerzenaktualisierungen.
- **Ausführungsrisiko**: Die beiden Market-Orders werden getrennt übermittelt und sind nicht atomar. Gleiche Stückzahlen garantieren zudem keine gleichen Nominalwerte; Risiken durch einseitige Ausführung, Liquidität und Kontraktgröße bleiben bestehen.

