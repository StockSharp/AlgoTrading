# ABE BE CCI Engulfing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie portiert den MetaTrader 5 Expertenberater **Expert_ABE_BE_CCI** (Ordner `MQL/306`). Das Original EA kombiniert bullische/bearische Engulfing-Kerzenmuster mit einem Commodity Channel Index (CCI)-Bestätigungsmodul und einer festen Lot-Geldverwaltung. Die C#-Implementierung behält die gleiche Entscheidungslogik bei und nutzt gleichzeitig die von StockSharp bereitgestellten Abonnement- und Indikatorbindungen auf hoher Ebene.

Die Engine überwacht abgeschlossene Kerzen im ausgewählten Zeitrahmen, berechnet einen gleitenden Durchschnitt der Kerzenkörper, einen Durchschnitt der Schlusskurse und einen CCI mit konfigurierbarem Zeitraum. Bullische oder bärische Engulfing-Muster werden nur akzeptiert, wenn die Kerzenkörper den aktuellen Durchschnitt überschreiten und der Mittelpunkt der verschlungenen Kerze auf der richtigen Seite des gleitenden Durchschnitts liegt, was die MQL `CCandlePattern`-Prüfungen nachahmt. Long-Trades erfordern ein bullisches Engulfing plus CCI unter dem überverkauften Schwellenwert, während Short-Trades die Spiegelbedingung mit CCI über dem überkauften Schwellenwert erfordern. Positionsausgänge spiegeln die EA-„Abstimmungs“-Logik wider: CCI Überschreitungen von ±ExitLevel neutralisieren offene Positionen unabhängig von der Richtung.

## Arbeitsablauf

1. Abonnieren Sie den konfigurierten Kerzentyp und berechnen Sie:
   - Kerzenkörperdurchschnitt über `BodyAveragePeriod` Balken.
   - Gleitender Durchschnitt der Schlusskurse im selben Fenster.
   - Rohstoffkanalindex mit der Länge `CciPeriod`.
2. Für jede fertige Kerze:
   - Stellen Sie sicher, dass die vorherige Kerze einen umhüllten Balken mit entgegengesetzter Farbe bildet.
   - Überprüfen Sie, ob der einhüllende Körper größer als der Durchschnitt des Rollkörpers ist und über die vorherige Öffnung hinaus schließt, indem Sie die MQL-Filter replizieren.
   - Bestätigen Sie den Trendkontext, indem Sie den Mittelpunkt der vorherigen Kerze mit dem gleitenden Durchschnitt des Schlusskurses vergleichen.
   - Bestätigen Sie die Dynamik mit CCI vs. `EntryOversoldLevel` oder `EntryOverboughtLevel`.
3. Trades verwalten:
   - Wenn die bullischen Bedingungen übereinstimmen und keine Long-Position aktiv ist, schließen Sie Short-Positionen und kaufen Sie das konfigurierte Volumen.
   - Wenn die rückläufigen Bedingungen übereinstimmen und kein Short aktiv ist, schließen Sie Long-Positionen und verkaufen Sie das konfigurierte Volumen.
   - Überwachen Sie CCI auf Exits: Jede Kreuzung unter `+ExitLevel` oder über `-ExitLevel` schließt Long-Positionen, während Kreuzungen über `-ExitLevel` oder unter `+ExitLevel` Short-Positionen schließen, was der 40-Punkte-Abstimmungslogik von EA entspricht.

## Standardparameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CciPeriod` | 49 | Länge des Commodity Channel Index-Indikators. |
| `BodyAveragePeriod` | 11 | Fenster zur Mittelung der Kerzenkörpergröße und des Mittelwerts des Schlusskurses. |
| `EntryOversoldLevel` | -50 | CCI-Schwellenwert, der bullische Engulfing-Setups bestätigt. |
| `EntryOverboughtLevel` | 50 | CCI-Schwellenwert, der die bärischen Engulfing-Setups bestätigt. |
| `ExitLevel` | 80 | Absolutes CCI-Niveau, das bei Überschreiten Positionsausstiege auslöst. |
| `CandleType` | 1 Stunde | Für das Kerzenabonnement verwendeter Zeitrahmen. |

## Notizen

- Die Volumenverarbeitung spiegelt typische StockSharp-Konvertierungen wider: `Volume` definiert die Basisauftragsgröße; Gegenüberliegende Positionen werden vor dem Umkehren abgeflacht.
- Trailing- und Money-Management-Komponenten (`TrailingNone`, `MoneyFixedLot`) aus dem Paket MQL werden nicht neu erstellt; Die Auftragsgröße von StockSharp deckt bereits das Verhalten bei festen Losen ab.
- Alle Kommentare im Code sind auf Englisch, Tabulatoren werden zum Einrücken verwendet und es werden keine Indikatorwerte über `GetValue` abgerufen, gemäß den Repository-Richtlinien.
