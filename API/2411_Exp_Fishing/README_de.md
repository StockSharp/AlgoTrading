# Exp Fishing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Position, wenn das Schlusskurs der abgeschlossenen Kerze vom Eröffnungskurs um mindestens **Price Step** abweicht. Ist die Differenz positiv, wird gekauft; ist sie negativ, wird verkauft.

Nach dem Eröffnen einer Position löst jede zusätzliche Bewegung von **Price Step** zugunsten des Trades eine weitere Marktorder in dieselbe Richtung aus, bis **Max Orders** erreicht ist. Für jede Position werden Stop-Loss und Take-Profit mit absoluten Preisabständen angewendet.

## Parameter

- **Price Step** – minimale Preisbewegung (in absoluten Einheiten) zum Öffnen oder Hinzufügen einer Position.  
- **Max Orders** – maximale Anzahl erlaubter Marktorders in eine Richtung.  
- **Stop Loss** – Abstand vom Einstiegspreis, bei dem ein Schutz-Stop gesetzt wird.  
- **Take Profit** – Abstand vom Einstiegspreis, bei dem das Gewinnziel gesetzt wird.  
- **Candle Type** – Kerzen-Zeitrahmen für Berechnungen (Standard: 1 Minute).

## Handelslogik

1. Auf eine abgeschlossene Kerze warten.
2. Wenn keine Position offen ist:
   - Kaufen wenn `Close - Open >= Price Step`.
   - Verkaufen wenn `Open - Close >= Price Step`.
3. Wenn eine Position existiert:
   - Wenn der Kurs um `Price Step` vom letzten Einstieg voranschreitet, eine weitere Order in dieselbe Richtung hinzufügen.
   - Keine weiteren Orders hinzufügen, sobald die Anzahl **Max Orders** erreicht.
4. Stop-Loss und Take-Profit werden für jede Order automatisch verwaltet.

Die Strategie ist vom MQL5-Experten "Exp Fishing" adaptiert und demonstriert einen einfachen Grid-artigen Trendfolge-Ansatz.
