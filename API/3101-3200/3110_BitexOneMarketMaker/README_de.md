# BitexOne Market-Maker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **BitexOne Market-Maker-Strategie** reproduziert den asynchronen Quotierungsroboter aus dem originalen
`BITEX.ONE MarketMaker.mq5`-Quellcode. Der Algorithmus platziert kontinuierlich Paare von Limitaufträgen um einen
Referenzpreis und hält eine gleiche Anzahl von Niveaus auf Bid- und Ask-Seite. Die Strategie wurde für StockSharp mit der
High-Level-API neu geschrieben: die Quotierungsverwaltung wird durch Order-Book- und Level-1-Abonnements gesteuert, während
die Risiko- und Volumen-Normalisierung auf Instrument-Metadaten (`PriceStep`, `VolumeStep` und `MinVolume`) basiert.

## Handelslogik
1. Den *Lead-Preis* aus dem ausgewählten `PriceSource` bestimmen. Standardmäßig erwartet die Strategie Mark-Preise, kann
   aber das Hauptorder-Buch oder ein Hilfsinstrument (Index oder Mark-Symbol) über den Parameter `LeadSecurity` verwenden.
2. Den Abstand zwischen Preisniveaus als `ShiftCoefficient * lead price` berechnen und eine symmetrische Leiter von Zitaten
   ober- und unterhalb der Referenz erstellen.
3. Die Gesamtexposition auf jeder Seite auf `MaxVolumePerLevel * LevelCount` begrenzen. Ausgeführte Trades reduzieren
   sofort das verfügbare Volumen, sodass das Grid immer die aktuelle Position widerspiegelt.
4. Preise und Volumen mit dem Instrument-Tick-Größe und Volumenschritt normalisieren. Der Algorithmus storniert veraltete
   Aufträge und registriert neue, wenn Preis oder Volumen über die Toleranz aus dem ursprünglichen MQL-Code hinausgehen
   (0,05% Preisschwelle und halber Volumenschritt).
5. Alle aktiven Aufträge werden während Stop/Reset-Ereignissen storniert, um das Buch sauber zu halten.

## Parameter
- `MaxVolumePerLevel` – maximales Volumen, das auf einem einzelnen Preisniveau quotiert wird. Betrifft beide Seiten des
  Buchs und wirkt als Obergrenze, wenn die aktuelle Position wächst.
- `ShiftCoefficient` – relativer Offset vom Lead-Preis für jedes inkrementelle Niveau (`leadPrice ± shift * levelIndex`).
- `LevelCount` – Anzahl der Quotierungsniveaus pro Seite. Jedes Niveau erstellt einen Kauf- und einen Verkauf-Limitauftrag.
- `PriceSource` – aufgezählter Wert (`OrderBook`, `MarkPrice`, `IndexPrice`), der definiert, woher der Referenzpreis stammt.
- `LeadSecurity` – optionales Instrument, das verwendet wird, wenn externe Mark- oder Indexpreise benötigt werden. Falls
  weggelassen, liefert das Hauptstrategie-Instrument die Referenz.

## Konvertierungshinweise
- Das asynchrone Order-Management von MetaTrader (SendAsync/ModifyAsync/RemoveOrderAsync) wird auf StockSharp's
  `BuyLimit`/`SellLimit`-Helfer abgebildet, kombiniert mit expliziter Stornierung, wenn Toleranzen überschritten werden.
- Die Positions-Ausgleichslogik (`max_pos * level_count ± position`) wird beibehalten, um die Leiter zentriert und
  risikobewusst zu halten.
- Die Lead-Preis-Auswahl ahmt die Suffix-Logik des ursprünglichen Roboters nach (`symbol`, `symbolm`, `symboli`), indem
  ein benutzerdefiniertes `LeadSecurity` kombiniert mit einem `PriceSource`-Hinweis erlaubt wird.
- Timer-gesteuerte Überprüfungen in MQL werden durch reaktive Updates ersetzt, die durch Order-Book/Level-1-Nachrichten
  und Portfolio-Ereignisse ausgelöst werden.

## Verwendungshinweise
- Stellen Sie sicher, dass der angeschlossene Adapter Markttiefe oder Level-1-Daten sowohl für das Handelssymbol als auch
  für das optionale `LeadSecurity` bereitstellt.
- Wenn Mark- oder Index-Feeds verwendet werden, abonnieren Sie die entsprechenden Instrumente vor dem Start der Strategie,
  damit der Lead-Preis sofort verfügbar ist.
- Erwägen Sie die Aktivierung von Portfolio-Schutz oder zusätzlichem Risikomanagement in der Hosting-Umgebung, wenn die
  Börse strenge Quote-zu-Trade-Verhältnisse erfordert.
- Die Strategie beginnt erst zu quotieren, wenn ein positiver Lead-Preis empfangen wird; überprüfen Sie die Konnektivität,
  wenn nach dem Start keine Aufträge erscheinen.
