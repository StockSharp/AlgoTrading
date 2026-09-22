# Strategiediagramm für Pivot-Limitorders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm macht aus klassischen Pivot-Unterstützungen und -Widerständen ein dauerhaft verwaltetes Limitorder-Paar. Ein rollierender Tag abgeschlossener Kerzen liefert die Spanne, ein kurzes Mitternachtsfenster steuert den Lebenszyklus, und dieselbe aktuelle Orderreferenz läuft durch Registrierung, Ersetzung, Stornierung und Chart.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen Highest und Lowest mit 288 Perioden, also einem rollierenden Tag; der letzte Schlusskurs vervollständigt die Berechnung.
- Die Formeln berechnen P = (H + L + C) / 3, R1 = 2P − L und S1 = 2P − H ohne verkettete Formeln.
- Working time erzeugt nahe Mitternacht den täglichen Impuls. Ein Zustands-Latch lässt beim ersten fertigen Niveau und flacher Position genau ein Paar zu.
- Order registering stellt ein Kauflimit bei S1 und ein Verkaufslimit bei R1 ein; die berechneten Preise werden unverändert übernommen.
- Combination hält die neueste Orderreferenz. Spätere Tagesimpulse treiben Order replacing, dessen Ergebnis jeweils in denselben Bus zurückläuft.
- Position protection setzt nach einer Ausführung einen Stop von 1%. Dessen Ausführung startet Order cancellation; die Order am gegenüberliegenden Pivot kann die Position regulär glattstellen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Nach Bildung der 288-Kerzen-Spanne stellt der erste gültige Mitternachtsimpuls ein Kauflimit bei S1 mit dem gewählten Volumen ein. Eine Berührung der Unterstützung führt aus; die Verkaufsorder bei R1 kann danach als Gegenausstieg dienen.
- **Short-Einstieg**: Derselbe Impuls stellt ein Verkaufslimit bei R1 ein. Eine Berührung des Widerstands eröffnet den Short, und das Kauflimit bei S1 bildet den Gegenausstieg.
- **Ausstieg**: Die gegenüberliegende Pivot-Order kann die Position am anderen Rand der Spanne schließen. Unabhängig davon beendet Position protection eine ungünstige Bewegung von 1% und storniert nach der Ausführung verbleibende Orders.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 00:05:00 | Zeitrahmen der abgeschlossenen Kerzen für rollierende Tagesspanne, Pivot-Niveaus, Orderzeit und Schutzprüfung. |
| Order Volume | 1 | Menge jeder Kauf- und Verkaufslimitorder. |
| Stop Loss, % | 1 | Ungünstiger Abstand von der Ausführung, bei dem Position protection aussteigt, in Prozent. |

## Diagrammdetails

- Das Highest/Lowest-Fenster von 288 Bars ist eine rollierende Tagesnäherung und benötigt keine getrennte Tageskerzen-Subscription.
- R1 und S1 sind direkt über H, L und C ausgeschrieben; die Gleichungen bleiben erhalten und beide Werte entstehen in derselben Verarbeitungsschicht.
- Eine Flag-Variable merkt sich das bereits aktivierte Anfangspaar und verhindert wiederholte Registrierungen pro Kerze.
- Registrierte und ersetzte Orders laufen in Combination<Order>-Busse; jede neue Referenz wird für die nächste Ersetzung zurückgeführt.
- OnlineOnly = false und ShrinkPrice = false ermöglichen historische Wiedergabe und bewahren die berechneten Preise.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
