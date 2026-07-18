# Zone-Recovery-Formula-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Zone-Recovery-Formula-Strategie** ist eine Portierung des MetaTrader-4 Expert Advisors "Zone Recovery Formula". Der Algorithmus folgt einer durch gleitende Durchschnitte bestimmten Trendrichtung und wendet anschließend eine Zone-Recovery-Technik an, um ungünstige Preisbewegungen abzufedern. Die Kernidee besteht darin, Long- und Short-Zyklen mit allmählich steigendem Volumen abzuwechseln, bis die Preisbewegung die definierte Recovery-Zone verlässt und selbst nach mehreren Umkehrungen Gewinn sichert.

## Funktionsweise

1. **Signalerkennung** - Die Strategie abonniert Zeitrahmenkerzen (standardmäßig 15 Minuten) und verfolgt einen schnellen und einen langsamen einfachen gleitenden Durchschnitt. Eine bullische Kreuzung startet einen Long-Recovery-Zyklus, während eine bärische Kreuzung einen Short-Zyklus startet.
2. **Initialorder** - Wenn ein neuer Zyklus beginnt, eröffnet die Strategie eine Marktposition mit dem Basis-Volumenmultiplikator. Take-Profit- und Recovery-Distanzen werden aus den Pip-Einstellungen und der Tickgröße des Instruments berechnet.
3. **Zone Recovery** - Wenn sich der Preis um die konfigurierte Recovery-Distanz gegen die offene Position bewegt, kehrt die Strategie die Richtung um und erhöht die Ordergröße anhand der ursprünglichen Formelsequenz (bis zur maximalen Anzahl von Trades). Dadurch entsteht eine alternierende Nettoexposure, die frühere Verluste decken soll, sobald der Preis zum Gewinnziel zurückkehrt.
4. **Gewinnmanagement** - Der Algorithmus überwacht unrealisierten Gewinn:
   - Geld- und prozentbasierte Take-Profit-Bedingungen können alle Positionen sofort schließen.
   - Optionales Trailing-Management erfasst Gewinne nach einem vordefinierten Gewinn und schützt sie mit einer Trailing-Stop-Distanz.
5. **Zyklus-Reset** - Wenn Gewinnziele erreicht werden oder Trailing-Schutz die Position schließt, wird der Recovery-Zyklus zurückgesetzt und die Strategie wartet auf das nächste Signal des gleitenden Durchschnitts.

## Schlüsselparameter

- **TP Geld verwenden / TP Geld** - Geldbasierten Take-Profit aktivieren und konfigurieren.
- **TP % verwenden / TP Prozent** - Prozentualen Take-Profit auf Basis des Portfoliosaldos aktivieren und konfigurieren.
- **Trailing aktivieren / Trailing TP / Trailing SL** - Trailing-Gewinnsicherung aktivieren und Aktivierungsniveau zusammen mit Schutzdistanz definieren.
- **TP Pips / Zone Pips** - Distanzen (in Pips), die Take-Profit-Ziel und Recovery-Auslösezone definieren.
- **Basisvolumen / Max. Trades** - Anfangsordergröße und Anzahl der erlaubten Recovery-Schritte in einem Zyklus.
- **Schnelle MA / Langsame MA** - Gleitende Durchschnitte, die Einstiegssignale erzeugen.
- **Gewinn-Offset** - Optionale Anpassung, die in der ursprünglichen Recovery-Volumenformel verwendet wird.

## Hinweise

- Die Strategie verwendet die High-Level-API von StockSharp mit Kerzenabonnements und Indikatorbindung.
- Hedging-Positionen werden emuliert, indem die Nettopositionsrichtung gewechselt und das Volumen skaliert wird; dadurch bleibt die Logik mit StockSharps Nettopositionsrechnung kompatibel.
- Trailing- und Take-Profit-Prüfungen beruhen auf unrealisiertem Gewinn, der aus dem aktuellen Positionspreis berechnet wird. Passen Sie Geldwerte an den Tickwert des Instruments an.
- Testen Sie immer in einer simulierten Umgebung, bevor Sie auf einem Live-Konto handeln.

## Dateien

- `CS/ZoneRecoveryFormulaStrategy.cs` - C#-Implementierung der Strategie.
- `README.md` - diese Dokumentationsdatei auf Englisch.
- `README_ru.md` - Dokumentation auf Russisch.
- `README_zh.md` - Dokumentation auf Chinesisch.
