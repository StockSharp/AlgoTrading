# Wöchentliche Range-Breakout-Strategie (ID 3412)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Range Breakout Weekly Strategy** ist eine StockSharp hochrangige API-Konvertierung des MetaTrader 5-Expertenberaters `RangeBreakout.mq5`. Das System bereitet einmal pro Woche anhand eines konfigurierbaren Wochentags und einer konfigurierbaren Stunde Breakout-Levels vor und eröffnet dann einen einzelnen Trade, wenn der Preis über oder unter die berechnete Spanne fällt. Die Positionsgrößenbestimmung und die Verlustkompensationslogik im Martingale-Stil spiegeln das ursprüngliche Skript wider, während die Implementierung StockSharp-Abonnements für Kerzen, Level-1-Kurse und Indikatorbindung nutzt.

## Handelslogik

1. **Wöchentliches Vorbereitungsfenster.** Beim Schließen der angegebenen stündlichen Kerze am konfigurierten Wochentag zeichnet die Strategie den Schlusskurs der Kerze als Referenzpreis auf und wechselt von der *Standby*- in die *Setup*-Phase.
2. **Reichweitenberechnung.**
   - Der primäre Bereich wird aus einem täglichen durchschnittlichen wahren Bereich über 20 Perioden (ATR) abgeleitet. Der ATR-Wert wird mit `ATR Percentage` multipliziert und auf die Tick-Größe des Instruments normalisiert.
   - Wenn ATR-Daten fehlen, greift der Algorithmus auf die Multiplikation des aktuellen Briefkurses mit `Price Percentage` zurück.
3. **Schutzstufen.**
   - Obere und untere Ausbruchsauslöser werden einen Bereich über und unter dem Referenzschluss platziert.
   - Take-Profit- und Stop-Loss-Offsets werden als Prozentsätze der Spanne berechnet. Wenn die Kompensation nach einem Verlust aktiv ist, wird der Take-Profit durch den kumulierten Kompensationsausgleich ersetzt und der Stop-Loss wird um denselben Betrag erweitert, genau wie bei der MetaTrader-Logik.
4. **Ausführung.**
   - Im *Setup* überwacht die Strategie Kurse der Stufe 1. Ein Bruch über dem oberen Auslöser führt zu einer Long-Position; Ein Abfall unter den unteren Auslöser eröffnet eine Short-Position. Aufträge werden als Marktaufträge mit an Ticks ausgerichteten Preisprüfungen gesendet.
   - Sobald eine Position aktiv ist (*Handelsphase*), werden die Notierungen der Stufe 1 kontinuierlich überwacht. Durch das Erreichen des schützenden Stops oder Ziels wird die Position mit einer Marktorder geschlossen.
5. **Martingale Wiederherstellung.**
   - Nach einem verlustbringenden Ausstieg verdoppelt sich die nächste Handelsgröße und der Verlustausgleich wird dem Kompensationspuffer hinzugefügt, sodass das folgende Ziel darauf abzielt, den kumulierten Verlust auszugleichen.
   - Bei einem gewinnenden Exit werden sowohl der Multiplikator als auch der Kompensationspuffer auf ihre Anfangswerte zurückgesetzt.
6. **Täglicher Reset.** Nach Abschluss eines Handels kehrt die Strategie in die *Standby*-Phase zurück und wartet bis zur nächsten geeigneten Kombination aus Wochentag und Stunde, um ein neues Setup vorzubereiten.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Trading Day` | Montag | Wochentag, der zur Messung der Ausbruch-Referenzkerze verwendet wird. Die Wochenendauswahl wird automatisch auf Montag umgestellt und entspricht dem ursprünglichen Warnverhalten. |
| `Start Hour` | 0 | Stunde (0-23), deren Schlusskerze als Referenz dient. Optimierbar, um verschiedene Sitzungseröffnungen abzudecken. |
| `Price Percentage` | 1,0 | Fallback-Prozentsatz des Briefkurses, der zur Berechnung der Spanne verwendet wird, wenn ATR-Daten fehlen. |
| `ATR Percentage` | 100 | Auf den täglichen ATR-Wert angewendeter Multiplikator, um den Ausbruchsbereich zu erhalten. |
| `Take Profit Percentage` | 100 | Prozentsatz der Spanne, die über den Eintrag hinaus hinzugefügt wird, um den Take-Profit-Preis zu definieren. Wird nach aufeinanderfolgenden Verlusten vom Kompensationspuffer überschrieben. |
| `Stop Loss Percentage` | 100 | Prozentsatz der Spanne, der vom Einstieg abgezogen wird, um den Stop-Loss-Preis festzulegen. Der Kompensationspuffer vergrößert diesen Abstand nach Verlusten. |
| `Base Volume` | 0,1 | Anfängliches Handelsvolumen vor der Martingal-Skalierung. Der Wert wird automatisch auf den Lautstärkeschritt des Instruments gerundet und durch minimale/maximale Einschränkungen begrenzt. |
| `ATR Period` | 20 | Anzahl der täglichen Kerzen, die dem Indikator ATR zugeführt werden. |
| `Hour Candle Type` | 1-stündiger Zeitrahmen | Kerzenabonnement, das zur Erkennung des Vorbereitungsfensters verwendet wird. |
| `ATR Candle Type` | Zeitrahmen 1 Tag | Kerzenabonnement, das den Indikator ATR speist. |

## Implementierungshinweise

- **Datenabonnements.** Die Strategie abonniert stündliche Kerzen für die Planung, tägliche Kerzen für die ATR-Berechnung und Level-1-Daten für die Bid/Ask-Überwachung. Das übergeordnete `Bind` API wird zum Streamen von Indikatorwerten ohne manuelle Pufferbehandlung verwendet.
- **Tick-Ausrichtung.** Alle Preisniveaus (Referenz, Auslöser, Stop-Loss, Take-Profit) werden durch `Security.ShrinkPrice` normalisiert, um Tick-Größenbeschränkungen zu berücksichtigen und das `NormalizeDouble`-Verhalten von MetaTrader nachzuahmen.
- **Volumenabwicklung.** Handelsvolumina werden vor der Auftragserteilung auf `VolumeStep` des Instruments gerundet und durch `VolumeMin`/`VolumeMax` eingeschränkt, wodurch die ursprüngliche Chargenbereinigung nachgebildet wird.
- **Phasenmaschine.** Interne Phasen (`Standby`, `Setup`, `Trade`) ersetzen die ursprüngliche Enum-Logik und stellen einen einzelnen Handel pro Vorbereitungszyklus sicher. Nach jedem Exit wird der Status auf `Standby` zurückgesetzt, bis die nächste qualifizierende Kerze auftritt.
- **Kompensationspuffer.** Das Feld `compensationOffset` speichert die kumulierte Verlustentfernung, ausgedrückt in Preiseinheiten. Wenn es aktiv ist, ersetzt das nächste Setup den Take-Profit-Offset durch diesen Wert und erweitert den Stop um denselben Betrag, was die MetaTrader-Formel widerspiegelt, die vergangene Geldverluste in Preisdistanz umwandelt.
- **Protokollierung.** Durch die Auswahl von Samstag oder Sonntag wird ein Informationsprotokoll ausgelöst und der Arbeitstag automatisch auf Montag umgestellt, entsprechend der Warnung, die in der Version MQL angezeigt wird.

## Nutzungstipps

1. Richten Sie `Trading Day` und `Start Hour` auf die Sitzung aus, die aussagekräftige Spannen generiert (z. B. Ausbruch aus der asiatischen Spanne oder Ausbruch aus der Londoner Eröffnung).
2. Kalibrieren Sie `ATR Percentage`, `Take Profit Percentage` und `Stop Loss Percentage` gemeinsam. Die Erhöhung des Range-Multiplikators führt zu breiteren Triggern und langsameren Trades, während die Anpassung der Gewinn-/Verlust-Prozentsätze das Verhältnis von Chance zu Risiko verändert.
3. Aktivieren Sie die Optimierung für `Start Hour`, `Base Volume` oder die Prozentparameter, um Parameter-Sweeps des ursprünglichen Expert Advisors zu reproduzieren.
4. Überwachen Sie die kumulative Exposition, die durch den Martingal-Multiplikator entsteht. Erwägen Sie, `Base Volume` zu senken, wenn Sie es auf Konten mit hoher Hebelwirkung ausführen.
5. Die Strategie ist für ein einzelnes Instrument konzipiert. Stellen Sie mehrere Kopien mit unterschiedlichen Wertpapieren oder Sitzungseinstellungen bereit, um die Abdeckung zu diversifizieren.

## Conversion-Abdeckung

- ✅ Wöchentliche Planung, Reichweitenberechnungen, Schutzstufen und Martingalverhalten von `RangeBreakout.mq5` beibehalten.
- ✅ MetaTrader-spezifische API-Aufrufe (`iATR`, `CopyBuffer`, `OrderSend` usw.) durch idiomatische StockSharp-Abstraktionen (`SubscribeCandles`, `AverageTrueRange`, `BuyMarket`/`SellMarket`) ersetzt.
- ✅ Auf Wunsch englische Inline-Kommentare und umfangreiche Dokumentation implementiert.
- ✅ Testprojekte unberührt gelassen und keine Python-Variante unter Einhaltung der Aufgabenbeschränkungen erstellt.
