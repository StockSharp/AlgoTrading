# EurUsd Sitzungs-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die klassische EUR/USD-Ausbruchsidee, bei der eine enge europäische Morgensession als Sprungbrett für die US-Session genutzt wird. Sie überwacht ein rollendes 24-Kerzen-Fenster (standardmäßig 15-Minuten-Kerzen), um die Pre-US-Handelsspanne zu messen, filtert Tage heraus, an denen die Spanne einen konfigurierbaren Pip-Schwellenwert überschreitet, und handelt dann Ausbrüche, die vollständig außerhalb dieser Spanne auftreten. Pro Handelstag ist nur ein Long- und ein Short-Versuch erlaubt.

## Funktionsweise

1. **Sitzungsverfolgung** – zu Beginn der konfigurierten US-Sitzungsstunde sperrt die Strategie die von den 24 letzten abgeschlossenen Kerzen (ohne den aktuellen Balken) erfasste EU-Spanne. Die Spanne wird für 3- oder 5-stellige Forex-Kurse automatisch auf Pip-Werte angepasst.
2. **Spannenfilter** – der Handel wird nur aktiviert, wenn die erfasste EU-Spanne kleiner ist als der Schwellenwert *Kleine EU-Sitzung (Pips)*.
3. **Ausbruchsvalidierung** – während der erlaubten US-Sitzungsstunden, und nur zwischen `(EU-Startstunde + 5)` und `(EU-Startstunde + 10)`, sucht die Strategie nach Kerzen, deren gesamter Körper außerhalb der gespeicherten Spanne mit einem zusätzlichen in Punkten gemessenen Puffer gehandelt hat.
4. **Orderausführung** – eine Market-Kauf-Order wird gesendet, wenn das Tief des Balkens über dem oberen Ende der Spanne plus Puffer bleibt. Eine Market-Verkauf-Order wird gesendet, wenn das Hoch des Balkens unter dem unteren Ende der Spanne minus Puffer bleibt. Long- und Short-Trades sind unabhängige Flags, sodass jede Richtung einmal pro Tag versucht werden kann.
5. **Risikomanagement** – Stop-Loss- und Take-Profit-Niveaus werden in Pips definiert, in absolute Preisabstände umgerechnet und auf jeder abgeschlossenen Kerze mithilfe von Hoch/Tief-Extremen verfolgt.

## Parameter

- **EU-Sitzungsstart / US-Sitzungsstart / US-Sitzungsende** – Stunden (0–23), die definieren, wann das EU-Monitoring beginnt und wann das US-Ausbruchsfenster geöffnet ist.
- **Kleine EU-Sitzung (Pips)** – maximale Größe der EU-Spanne, die noch den Handel erlaubt.
- **Handel am Montag** – aktiviert oder deaktiviert den Montags-Handel bei gleichzeitiger Blockierung von Wochenenden.
- **Stop-Loss (Pips)** – Abstand zwischen Einstiegspreis und Schutz-Stop, automatisch nach Tick-Größe und Stellen skaliert.
- **Take-Profit (Pips)** – Gewinnziel-Abstand, auf dieselbe Weise wie der Stop behandelt.
- **Ausbruchspuffer (Punkte)** – Anzahl der Preisschritte, die zum Ausbruchsauslöser hinzugefügt werden, sodass der bestätigende Balken vollständig jenseits der gespeicherten Spanne liegen muss.
- **Kerzentyp** – Datentyp für das Kerzenabonnement; standardmäßig 15-Minuten-Zeitrahmen, da das ursprüngliche Skript für M15-Charts konzipiert wurde.

## Zusätzliche Hinweise

- Die Strategie setzt Netting-Konten voraus: Schutzlevel glätten die gesamte Position mithilfe von Market-Orders.
- Der Tagesstatus wird um Mitternacht zurückgesetzt, sodass Spanne und Ausbruchs-Flags nicht zwischen Sitzungen überlaufen, während offene Positionen ihre Preisziele behalten.
- Da Stop-Loss- und Take-Profit-Niveaus mit Kerzenextremen simuliert werden, werden Intrabar-Spitzen, die nicht in historischen Balken erscheinen, nicht erkannt.
