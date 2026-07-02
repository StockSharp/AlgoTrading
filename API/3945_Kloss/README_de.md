# Kloss MQL/8186 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Kloss MQL/8186-Strategie** ist eine direkte Umsetzung des MetaTrader 4-Expertenberaters `Kloss.mq4`. Es kombiniert einen Rohstoffkanalindex (CCI), einen Stochastic-Oszillator und einen verschobenen typischen Preisfilter, um Umkehrungen einzelner Positionen zeitlich festzulegen. Die StockSharp-Version behält die ursprünglichen Einstiegsschwellen, Stop-Loss- und Take-Profit-Abstände sowie die Volumenlogik (feste Losgröße oder prozentuale Größe) bei, während das High-Level-Candle-Abonnement API verwendet wird.

## Handelslogik

- **Daten**: Abgeschlossene Kerzen des konfigurierten Zeitrahmens (Standard 5 Minuten). Die Indikatoren werden anhand derselben Serie berechnet.
- **Indikatoren**:
  - CCI mit Punkt 10. Der absolute Wert wird mit `±CciThreshold` (Standard 120) verglichen.
  - Stochastic-Oszillator mit `%K=5`, `%D=3`, Glättung `=3`. Die Hauptlinie `%K` wird anhand der überverkauften/überkauften Bänder überprüft.
  - Typischer Preis ((Hoch + Tief + Schluss) / 3), verzögert um fünf abgeschlossene Kerzen, um den verschobenen LWMA des Fachberaters nachzubilden.
- **Langer Eintrag**:
  - CCI <= `-CciThreshold`.
  - Stochastic %K < `StochasticOversold` (Standard 30).
  - Vorherige offene Kerze > typischer Preis von vor fünf Kerzen.
  - Keine bestehende Long-Position (`Position <= 0`). Jeder offene Short wird geschlossen und in einer einzigen Marktorder in eine Long-Position umgewandelt.
- **Kurzer Eintrag**:
  - CCI >= `CciThreshold`.
  - Stochastic %K > `StochasticOverbought` (Standard 70).
  - Vorheriger Kerzenschluss < typischer Preis von vor fünf Kerzen.
  - Keine bestehende Short-Position (`Position >= 0`). Jede offene Long-Position wird geschlossen und mit einer Market-Order in eine Short-Position umgewandelt.
- **Positionsmanagement**: StockSharps `StartProtection` gibt automatisch Stop-Loss- und Take-Profit-Orders unter Verwendung der angegebenen Punktabstände aus. Ansonsten hält die Strategie jederzeit eine einzelne Position (Flat, Long oder Short).

## Positionsgrößen

- **Festes Volumen**: Bei `FixedVolume > 0` handelt die Strategie immer genau mit diesem Volumen (nach Anpassung an `VolumeStep` und `MinVolume` des Instruments).
- **Risikoprozentsatz**: Bei `FixedVolume = 0` weist die Strategie `RiskPercent` (Standard 0,2) des Kontowerts dividiert durch die zuletzt geschätzte Auftragsgröße zu. Die Lautstärke wird durch `MaxVolume` (Standard 5) begrenzt und auf die Schrittweite des Instruments gerundet.
- **Sicherheitsmaßnahmen**: Die Methode greift auf das minimal handelbare Volumen zurück, wenn Kontoinformationen fehlen oder der berechnete Wert nicht positiv ist.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CciPeriod` | Anzahl der Kerzen, die zur Berechnung des Commodity Channel Index verwendet werden. | 10 |
| `CciThreshold` | Absoluter CCI-Level, der Einträge auslöst. | 120 |
| `StochasticKPeriod` | %K Periode des Stochastic-Oszillators. | 5 |
| `StochasticDPeriod` | %D Glättungszeitraum. | 3 |
| `StochasticSmooth` | Zusätzliche Glättung wird auf %K vor dem Signal angewendet. | 3 |
| `StochasticOversold` | %K-Schwellenwert zur Bestätigung langer Einträge. | 30 |
| `StochasticOverbought` | %K-Schwellenwert zur Bestätigung kurzer Einträge. | 70 |
| `StopLossPoints` | Abstand in Preispunkten für den Schutzstopp. | 48 |
| `TakeProfitPoints` | Abstand in Preispunkten zum Gewinnziel. | 152 |
| `FixedVolume` | Ein positiver Wert erzwingt ein festes Handelsvolumen. | 0 |
| `RiskPercent` | Portfolioanteil in Volumen umgewandelt, wenn `FixedVolume` Null ist. | 0,2 |
| `MaxVolume` | Maximal zulässiges Handelsvolumen. | 5 |
| `CandleType` | Kerzentyp/Zeitrahmen für Indikatorberechnungen. | Zeitrahmen von 5 Minuten |

## Ausführungshinweise

- **Einzelposition**: Es wird nur eine Position offen gehalten. Durch Umkehrungen wird die bestehende Position geschlossen und die neue Position mit einer einzigen Marktorder eröffnet.
- **Indikatorsynchronisation**: Die Preisverschiebung nutzt die letzten fünf abgeschlossenen Kerzen; Es müssen mindestens sechs Kerzen verarbeitet werden, bevor der erste Trade erscheinen kann.
- **Stopps/Ziele**: `StartProtection` wandelt punktbasierte Entfernungen mithilfe des `PriceStep` des Instruments in absolute Preisversätze um. Wenn `PriceStep` unbekannt ist, wird der Rohpunktwert angewendet.
- **Datenanforderungen**: Funktioniert mit jedem Instrument, das OHLC Kerzen bereitstellt; Die Lautstärkeausrichtung berücksichtigt `MinVolume` und `VolumeStep`, sofern verfügbar.
- **Unterschiede gegenüber MT4**: MetaTrader Margenberechnungen werden durch das Kontokapital (`Portfolio.CurrentValue`) angenähert. Wenn keine Aktiendaten verfügbar sind, greift die Strategie auf das minimal handelbare Volumen zurück.

## Nutzungstipps

1. Passen Sie `CandleType` an die in MetaTrader verwendete Marktsitzung an (M5 in der Originalvorlage).
2. Überprüfen Sie die Stoppentfernungen im Verhältnis zur Tickgröße. Die Point-to-Price-Konvertierung erfolgt automatisch, die Werte müssen jedoch möglicherweise für Nicht-Forex-Instrumente angepasst werden.
3. Für feste Kontraktgrößen setzen Sie `FixedVolume` auf das gewünschte Los und `RiskPercent` auf Null.
4. Aktivieren Sie die Optimierung für die Indikatorschwellenwerte, wenn Sie die Strategie auf neue Symbole kalibrieren.
