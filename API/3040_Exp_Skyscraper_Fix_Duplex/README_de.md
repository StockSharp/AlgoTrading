# Exp Skyscraper Fix Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Exp Skyscraper Fix Duplex ist ein Port des MQL5-Expert Advisors *Exp_Skyscraper_Fix_Duplex*. Die Strategie führt den Skyscraper Fix-Kanal auf der Long- und Short-Seite unabhängig aus, sodass jede Seite ihren eigenen Zeitrahmen, ATR-Fenster und Sensitivität verwenden kann. Long- und Short-Trades können daher auf unterschiedliche Marktregimes reagieren, während sie dieselbe Ausführungslogik innerhalb von StockSharp teilen.

## Indikatorlogik
Der benutzerdefinierte **Skyscraper Fix**-Indikator reproduziert das ursprüngliche Skript:

- Ein ATR mit einer festen internen Periode von 15 wird für jede abgeschlossene Kerze berechnet.
- Die höchsten und niedrigsten ATR-Werte über das konfigurierbare `Length`-Fenster bestimmen den adaptiven Preisschritt.
- Abhängig vom ausgewählten `Mode` werden entweder der Hoch/Tief der Kerze oder der Schlusskurs verwendet, um obere und untere Kanalniveaus im doppelten Schrittabstand zu projizieren.
- Der jüngste Ausbruch über das obere Niveau oder unter das untere Niveau kippt den internen Trend und fixiert das Trailing-Niveau so, dass es sich nie gegen den aktuellen Bias bewegt.
- Das Kreuzen der entgegengesetzten Trail-Linie erzeugt diskrete Kauf- oder Verkaufsauslöser (spiegelt die Pfeil-Buffer des Indikators in MQL wider).

Der Indikator exponiert das obere Trailing-Niveau, das untere Trailing-Niveau, Einstiegsauslöser und eine Mittellinie, die bei Bedarf geplottet werden kann.

## Handelsregeln
Long- und Short-Operationen werden separat für jede abgeschlossene Kerze des jeweiligen Abonnements ausgewertet:

- **Long-Einstieg** – ausgelöst, wenn der Long-Indikator ein frisches Kaufniveau meldet. Bestehende Short-Exponierung wird zuerst gedeckt, dann wird eine neue Long-Marktorder mit dem konfigurierten Volumen eingereicht.
- **Long-Ausstieg** – ausgelöst, wenn der Long-Indikator die entgegengesetzte Trailing-Linie meldet. Jede bestehende Long-Position wird mit einem Marktverkauf geschlossen.
- **Short-Einstieg** – ausgelöst, wenn der Short-Indikator ein frisches Verkaufsniveau meldet. Bestehende Long-Exponierung wird zuerst geschlossen, dann wird eine neue Short-Marktorder gesendet.
- **Short-Ausstieg** – ausgelöst, wenn der Short-Indikator die entgegengesetzte Trailing-Linie meldet. Jede aktive Short-Position wird mit einem Marktkauf gedeckt.

Signale können mit den `SignalBar`-Parametern verzögert werden, damit die Strategie auf der zuletzt geschlossenen Kerze (`0`) oder auf weiter zurückliegenden Kerzen (`1` ahmt die Standard-MQL-Einrichtung nach) reagiert.

## Parameter
- `TradeVolume` – Ordergröße für Markteinstiege.
- `EnableLongEntries` / `EnableLongExits` – Umschalter für Long-seitigen Handel.
- `LongCandleType` – Kerzenserie für den Long-Indikator.
- `LongLength`, `LongKv`, `LongPercentage`, `LongMode`, `LongSignalBar` – Skyscraper Fix-Einstellungen für die Long-Seite.
- `EnableShortEntries` / `EnableShortExits` – Umschalter für Short-seitigen Handel.
- `ShortCandleType` – Kerzenserie für den Short-Indikator.
- `ShortLength`, `ShortKv`, `ShortPercentage`, `ShortMode`, `ShortSignalBar` – Skyscraper Fix-Einstellungen für die Short-Seite.

## Nutzungshinweise
- Die Strategie setzt die globale `Volume`-Eigenschaft aus `TradeVolume`, sodass Standard-`BuyMarket()`- und `SellMarket()`-Aufrufe diese Größe automatisch verwenden.
- Beide Indikatorinstanzen lesen den `PriceStep` des Instruments. Wenn dieser null ist, wartet der Indikator still, bis ein gültiger Preisschritt verfügbar ist.
- `StartProtection()` wird beim Start aufgerufen, damit Schutzmaßnahmen auf Plattformebene aktiv sind, bevor die erste Order eingereicht wird.
- Es gibt keine separate Python-Implementierung; das `PY`-Verzeichnis wird wie angefordert absichtlich weggelassen.
