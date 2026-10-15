# Beispiel für eine Strategie zum Erkennen eines Wirtschaftskalenders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Sample Detect Economic Calendar Strategy** repliziert das Verhalten des ursprünglichen MetaTrader-Expertenberaters `SampleDetectEconomicCalendar.mq5`. Die Strategie überwacht eine manuell bereitgestellte Liste von Wirtschaftskalenderereignissen und platziert – wenn ein Ereignis mit großer Auswirkung für die konfigurierte Währung bevorsteht – ein symmetrisches Paar Stop-Orders um die aktuellen Geld-/Briefkurse. Schutzstopps, optionale Take-Profit-Levels und ein nachlaufender Exit reproduzieren die Geldverwaltungslogik aus dem Quellcode.

Im Gegensatz zur Version MQL hat der Port StockSharp keinen Zugriff auf den Kalenderdienst MetaTrader. Stattdessen werden Ereignisse vom Benutzer über den Parameter `CalendarDefinition` bereitgestellt.

## Wie es funktioniert
1. Die Strategie abonniert Level1-Daten, um Geld-/Briefkurse zu verfolgen.
2. In `CalendarDefinition` definierte Kalenderzeilen werden beim Start analysiert.
3. Für jedes Ereignis von hoher Wichtigkeit, das mit `BaseCurrency` übereinstimmt, gilt die Strategie:
   - Wartet bis `LeadMinutes` vor der Veröffentlichung.
   - Berechnet das Auftragsvolumen (entweder fest oder risikobasiert).
   - Platziert Kauf-/Verkaufs-Stop-Orders bei `BuyDistancePoints` und `SellDistancePoints` aus den aktuellen Preisen.
4. Nach der Veröffentlichung werden ausstehende Orders nach Ablauf von `PostMinutes` oder nach dem gesamten Zeitlimit von `ExpiryMinutes` storniert.
5. Wenn eine Seite ausgelöst wird, wird die entgegengesetzte Bestellung storniert. Die offene Position wird mit Stop-Loss, optionalem Take-Profit und Trailing-Stop-Abständen, ausgedrückt in Punkten, verwaltet.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeNews` | Ermöglicht die Platzierung ausstehender Bestellungen rund um geplante Nachrichtenereignisse. |
| `OrderVolume` | Festes Auftragsvolumen, das verwendet wird, wenn die Geldverwaltung deaktiviert ist. |
| `StopLossPoints` | Stop-Loss-Distanz in Instrumentenpunkten. Zum Deaktivieren auf 0 setzen. |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten. Zum Deaktivieren auf 0 setzen. |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Punkten. Auf 0 setzen, um das Nachziehen zu deaktivieren. |
| `ExpiryMinutes` | Maximale Lebensdauer ausstehender Bestellungen nach der Veröffentlichung. |
| `UseMoneyManagement` | Wenn aktiviert, wird das Volumen aus dem Saldorisiko berechnet. |
| `RiskPercent` | Prozentsatz des pro Trade riskierten Portfoliokapitals (wird nur verwendet, wenn das Geldmanagement aktiv ist). |
| `BuyDistancePoints` | Offset über dem Brief für den Kaufstopp-Eintrag. |
| `SellDistancePoints` | Offset unterhalb des Gebots für den Verkaufsstopp-Eintrag. |
| `LeadMinutes` | Minuten vor der Freigabe, wenn ausstehende Orders übermittelt werden. |
| `PostMinutes` | Minuten nach der Freigabe, bevor unbeaufsichtigte Bestellungen storniert werden. |
| `BaseCurrency` | Währungscode, der im Kalendereintrag erscheinen muss (Standard `USD`). |
| `CalendarDefinition` | Mehrzeilige Zeichenfolge mit Kalenderereignissen. |

## Kalenderdefinitionsformat
Geben Sie ein Ereignis pro Zeile im folgenden Format an:

„
jjjj-MM-tt HH:mm;CUR;Hoch;Ereignistitel
„

* `yyyy-MM-dd HH:mm` – Zeitstempel in UTC. Sekunden sind optional. Mehrere Datumsformate (`yyyy/MM/dd`, `dd.MM.yyyy`) werden ebenfalls unterstützt.
* `CUR` – Währungscode (z. B. `USD`). Es werden nur Ereignisse gehandelt, die mit `BaseCurrency` übereinstimmen.
* `High` – Wichtiges Schlüsselwort (`High`, `Medium`, `Low` oder `Nfp`). Nur `High` löst Trades aus.
* `Event title` – Freitext für die Protokollierung.

Beispiel:

„
12.06.2024 18:00;USD;Hoch;FOMC-Erklärung
05.07.2024 12:30;USD;Nfp;Gehaltsabrechnungen außerhalb der Landwirtschaft
„

## Risikomanagement
* Wenn `UseMoneyManagement` **aus** ist, werden Bestellungen mit dem Parameter `OrderVolume` aufgegeben.
* Wenn `UseMoneyManagement` **ein** ist, riskiert die Strategie `RiskPercent` des Portfoliowerts unter Verwendung des konfigurierten `StopLossPoints`. Die Beschränkungen des Austauschvolumens (min./max. Schritt) werden eingehalten.
* Die Trailing-Logik spiegelt die ursprüngliche EA wider: Die Stop-Loss- und Take-Profit-Exits werden durchgesetzt, und sobald sich der Preis um `TrailingStopPoints` günstig bewegt, schützt der Trailing-Stop den Handel.

## Unterschiede zum Expertenberater MQL
* Wirtschaftskalenderereignisse müssen manuell in `CalendarDefinition` bereitgestellt werden.
* Pro Strategieinstanz wird nur ein Instrument/Währungspaar verarbeitet.
* Der ausstehende Orderablauf wird intern mit `PostMinutes`/`ExpiryMinutes`-Timern behandelt, da StockSharp Stop-Orders keine MetaTrader-artigen `ORDER_TIME_SPECIFIED`-Flags verfügbar machen.

## Nutzungshinweise
1. Konfigurieren Sie die `CalendarDefinition`-Zeilen, bevor Sie mit der Strategie beginnen.
2. Aktivieren Sie `TradeNews` und legen Sie die gewünschten Risikoparameter fest.
3. Stellen Sie sicher, dass Level-1-Daten verfügbar sind, damit Bid/Ask-Aktualisierungen vor dem Nachrichtenfenster eintreffen.
4. Überprüfen Sie die Protokolle, um sicherzustellen, dass Bestellungen wie erwartet bei jedem Ereignis aufgegeben und storniert werden.
