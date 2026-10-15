# Schnell-langsam-MA-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Fast Slow MA Crossover Strategy** reproduziert das Verhalten des ursprünglichen MetaTrader 4 Expert Advisors `_HPCS_FastSlowMACrosssover_MT4_EA_V01_WE`. Die Strategie überwacht zwei exponentielle gleitende Durchschnitte (EMAs), die anhand der ausgewählten Kerzenserie berechnet werden, und gibt Trades aus, wenn der schnelle Durchschnitt den langsamen Durchschnitt innerhalb eines konfigurierbaren Intraday-Handelsfensters kreuzt. Schützende Take-Profit- und Stop-Loss-Exits werden in Pips ausgedrückt, sodass das Verhalten mit der MQL-Implementierung übereinstimmt, die sich bei der Preisskalierung auf Broker-Ziffern verlässt.

## Handelslogik

1. Abonnieren Sie den konfigurierten Kerzentyp (Standard: 1-Minuten-Kerzen).
2. Berechnen Sie zwei EMAs:
   - Schneller EMA Zeitraum (Standard **14**).
   - Langsamer EMA Zeitraum (Standard **21**).
3. Bewerten Sie jede fertige Kerze:
   - Überprüfen Sie, ob die Schlusszeit der Kerze innerhalb des zulässigen Handelsfensters liegt.
   - Erkennen Sie einen **bullischen Crossover**, wenn der schnelle EMA den langsamen EMA überschreitet.
   - Erkennen Sie einen **bärischen Crossover**, wenn der schnelle EMA den langsamen EMA unterschreitet.
4. Befehle ausführen:
   - Schließen Sie die gegenüberliegende Belichtung, wenn eine Umkehrposition geöffnet ist.
   - Geben Sie eine Marktorder mit dem konfigurierten Volumen ein (Parameter **Handelsvolumen**).
   - Speichern Sie den Schlusskurs der Kerze als Einstiegsanker für Risikoberechnungen.
5. Verwalten Sie offene Positionen mithilfe von Kerzenhochs und -tiefs:
   - Schließen Sie eine Long-Position, wenn der Preis **Stop Loss (Pips)** unter den Einstiegspunkt fällt.
   - Schließen Sie eine Long-Position, wenn der Preis **Take Profit (Pips)** über den Einstiegspunkt steigt.
   - Wenden Sie die symmetrische Logik für Short-Positionen an (Stopp über dem Einstieg, Ziel unter dem Einstieg).

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **Schnelle MA-Periode** | Länge des schnellen EMA, der für die Crossover-Erkennung verwendet wird. |
| **Langsamer MA-Zeitraum** | Länge des langsamen EMA. |
| **Gewinnmitnahme (Pips)** | Abstand in Pips, der zur Berechnung der Long- und Short-Gewinnziele verwendet wird. |
| **Stop-Loss (Pips)** | Abstand in Pips, der zur Berechnung der schützenden Stop-Preise verwendet wird. |
| **Startzeit** | Beginn des täglichen Handelsfensters (einschließlich). |
| **Stoppzeit** | Ende des täglichen Handelsfensters (einschließlich). |
| **Kerzentyp** | Kerzenreihe zur Speisung der Indikatoren. |
| **Handelsvolumen** | Marktauftragsvolumen für jedes Signal. |

## Notizen

- Die Pip-Größe wird aus der Schrittweite des Wertpapierpreises und der Dezimalgenauigkeit abgeleitet. Wenn das Instrument 5 oder 3 Dezimalstellen verwendet, multipliziert die Strategie den Preisschritt mit **10**, um der Pip-Berechnung von MetaTrader zu entsprechen.
- Der Zeitfilter unterstützt Nachtsitzungen. Wenn die **Startzeit** später als die **Stoppzeit** liegt, bleibt der Handel bis Mitternacht aktiv und wird von Mitternacht bis zur Stoppzeit fortgesetzt.
- Es ist nur ein Signal pro Kerze zulässig, um sicherzustellen, dass das Verhalten dem ursprünglichen EA entspricht, das vor mehreren Übermittlungen pro Balken schützt.
- Protective Exit Orders werden von der Strategielogik anstelle von Resting Orders ausgeführt. Dies spiegelt den EA-Ansatz wider, bei dem die Stop-Loss- und Take-Profit-Level bei der Auftragserteilung definiert wurden.
