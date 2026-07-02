# Strategie für die tägliche Öffnungszeit des Fensters
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reproduziert das Verhalten des MetaTrader-Experten „OpenTime“. Es platziert Marktaufträge zu einer konfigurierbaren Tageszeit, schließt optional alle Engagements während eines speziellen Ausstiegsfensters und wendet einfache Money-Management-Regeln wie feste Stop-Loss-, Take-Profit- und Trailing-Schutzfunktionen an. Der Port verwendet das High-Level StockSharp `Strategy` API, sodass die Strategie mit anderen Komponenten innerhalb des Frameworks kombiniert werden kann.

## Wie es funktioniert
1. Jede fertige Kerze aus dem ausgewählten Zeitrahmen löst eine Tageszeitprüfung aus.
2. Wenn die aktuelle Zeit in das Handelsfenster fällt, sendet die Strategie Marktaufträge für jede aktivierte Richtung:
   * Ist nur eine Seite freigegeben, wird die aktuelle Nettoposition verlängert bzw. umgekehrt, bis das gewünschte Volumen erreicht ist.
   * Wenn beide Seiten aktiviert sind, werden Kauf- und Verkaufsaufträge im selben Fenster erteilt. Da StockSharp das Risiko nebeneinander verrechnet, gleicht das Öffnen der zweiten Richtung automatisch das entgegengesetzte Risiko aus, bevor das neue Risiko entsteht.
3. Während das Schließfenster aktiv ist, ruft die Strategie einmal `ClosePosition()` auf, um das ausstehende Risiko zu reduzieren.
4. Optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände werden an `StartProtection` delegiert, das die Schutzaufträge mithilfe von Marktausstiegen verwaltet.

## Parameter
- **Fenster schließen aktivieren** – spiegelt das Flag `TimeClose` wider. Wenn diese Option aktiviert ist, definieren `Close Position Time` und `Window Length`, wann bestehende Geschäfte geschlossen werden.
- **Positionszeit schließen** – tägliche Zeit, zu der das Ausstiegsfenster beginnt (Standard 20:50).
- **Handelszeit** – tägliche Zeit, zu der neue Trades zulässig sind (Standard 18:50).
- **Fensterlänge** – Dauer sowohl des Handels- als auch des Schlussfensters (Standard 5 Minuten, entsprechend der ursprünglichen `Duration`-Eingabe).
- **Verkaufseinträge zulassen** – entspricht dem Schalter MQL `Sell`; ermöglicht kurze Einträge (Standard: true).
- **Kaufeinträge zulassen** – entspricht dem Schalter MQL `Buy`; ermöglicht lange Einträge (Standard: false).
- **Auftragsvolumen** – angestrebtes Nettovolumen für jeden neuen Trade (Standard 0,1 Lots). Die Strategie addiert den absoluten Wert der aktuellen Position, wenn ein entgegengesetztes Signal auftritt, sodass es zu Umkehrungen in einer einzigen Marktorder kommt.
- **Stop-Loss-Punkte** – Abstand in Punkten für den Schutzstopp (Standard 0 deaktiviert den Stopp).
- **Take-Profit-Punkte** – Abstand in Punkten für das Gewinnziel (Standard 0 deaktiviert das Ziel).
- **Trailing Stop verwenden** – aktiviert die Trailing-Stop-Logik des ursprünglichen `SimpleTrailing`-Helfers.
- **Trailing Stop Points** – Nachlaufdistanz ausgedrückt in Punkten (Standard 300).
- **Trailing Step Points** – zusätzlicher Fortschritt erforderlich, bevor der Trailing Stop vorgezogen wird (Standard 3).
- **Kerzentyp** – Zeitrahmen, der für die Zeitprüfungen verwendet wird (standardmäßige 1-Minuten-Kerzen).

## Notizen
- Die Punktgröße ergibt sich aus der Wertpapierpreisstufe. Für Anführungszeichen mit drei und fünf Dezimalstellen wird der Schritt mit 10 multipliziert, wodurch die vom Skript MQL verwendete Pip-Verarbeitung reproduziert wird.
- `StartProtection` bringt Schutzstopps nur an, wenn mindestens einer der Abstände größer als Null ist. Wenn das Trailing ohne festen Stop-Loss aktiv ist, wird die Trailing-Distanz als anfänglicher Schutzwert geliefert.
- Die Strategie verwaltet absichtlich keine ausstehenden Aufträge oder wiederholten Wiederholungsversuche, da StockSharp bereits eine automatische Fehlerbehandlung für Marktaufträge bietet.
