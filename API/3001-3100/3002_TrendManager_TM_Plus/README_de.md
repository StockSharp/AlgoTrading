# TrendManager TM Plus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
TrendManager TM Plus ist eine Trendfolge-Strategie, die aus dem originalen MetaTrader 5 Expert Advisor `Exp_TrendManager_Tm_Plus.mq5` konvertiert wurde. Die Strategie basiert auf dem benutzerdefinierten TrendManager-Indikator, der zwei geglättete gleitende Durchschnitte vergleicht und den Abstand zwischen ihnen hervorhebt. Wenn der Abstand einen konfigurierbaren Schwellenwert überschreitet, öffnet die Strategie Positionen in Richtung des vorherrschenden Trends und schließt Positionen, wenn der Trend umkehrt oder wenn Schutzregeln ausgelöst werden.

## Handelslogik
1. Zwei gleitende Durchschnitte auf der ausgewählten Kerzenserie aufbauen. Die Glättungsmethoden und Längen beider Linien sind konfigurierbar.
2. Den Abstand zwischen den schnellen und langsamen Durchschnitten berechnen. Wenn der Abstand größer oder gleich dem Schwellenwert ist, meldet der Indikator einen Aufwärtstrend. Wenn der Abstand kleiner oder gleich dem negativen Schwellenwert ist, meldet der Indikator einen Abwärtstrend. Andernfalls gibt es kein handlungsfähiges Signal.
3. Die Farbzustände (0 für Aufwärtstrend, 1 für Abwärtstrend, 3 für neutral) in einem kurzen Verlauf speichern. Der Parameter `SignalBar` wählt aus, wie viele geschlossene Bars zurück ausgewertet werden, entsprechend der originalen MQL-Logik.
4. Wenn eine neue Aufwärtstrend-Farbe erscheint, schließt die Strategie optional bestehende Short-Positionen und kann eine Long-Position eröffnen, wenn Long-Einstiege erlaubt sind. Umgekehrt kann eine neue Abwärtstrend-Farbe Longs schließen und Shorts öffnen.
5. Optionale Zeit- und preisbasierte Ausstiege schließen offene Trades, wenn die Haltezeit `MaxPositionAge` überschreitet, wenn der Preis für Longs unter `StopLossDistance` fällt (oder für Shorts darüber steigt), oder wenn `TakeProfitDistance` erreicht wird.

## Parameter
- **Candle Type** – Zeitrahmen für die Signalgenerierung (Standard: 4-Stunden-Kerzen zur Übereinstimmung mit dem Originalskript).
- **Fast MA Method / Slow MA Method** – Glättungsalgorithmen für die schnellen und langsamen Linien. Verfügbare Optionen: Simple, Exponential, Smoothed, Weighted, Jurik und Kaufman Adaptive.
- **Fast Length / Slow Length** – Perioden für die gleitenden Durchschnitte.
- **Distance Threshold (`DvLimit`)** – Minimaler absoluter Abstand zwischen den schnellen und langsamen Durchschnitten, der erforderlich ist, um einen Trend zu erkennen. Originale MT5 punktbasierte Werte in Preiseinheiten umrechnen (z.B. 70 Punkte auf einem 5-stelligen Symbol ≈ 0.00070).
- **Signal Bar** – Anzahl der geschlossenen Bars zurück, die zur Bestätigung eines neuen Signals verwendet werden. Ein Wert von 1 reproduziert das Standardverhalten der MQL-Strategie.
- **Allow Long Entries / Allow Short Entries** – Einstiege für jede Richtung aktivieren oder deaktivieren.
- **Close Long / Close Short on Opposite Signal** – Offene Positionen sofort schließen, wenn ein Signal der entgegengesetzten Farbe erscheint.
- **Use Time Exit / Max Position Age** – Die maximale Haltezeit aktivieren und konfigurieren, bevor eine Position zwangsgeschlossen wird.
- **Order Volume** – Festes Volumen, das mit Marktorders gesendet wird. Dieser Parameter ersetzt die Geldverwaltungseinstellungen der MetaTrader-Version.
- **Stop Loss Distance / Take Profit Distance** – Optionale Schutzpreisoffsets in absoluten Preiseinheiten (auf null setzen zum Deaktivieren).

## Implementierungshinweise
- StockSharp-Indikatoren werden verwendet, um das TrendManager-Verhalten zu reproduzieren. Nicht unterstützte exotische Glättungsmodi aus der Originalbibliothek fallen auf den nächsten verfügbaren gleitenden Durchschnitt in StockSharp zurück.
- Die Signalverarbeitung behält einen kleinen Verlaufspuffer, damit die `SignalBar`-Prüfung Übergänge genau wie der MT5-Advisor erkennen kann.
- Schutzausstiege werden auf abgeschlossenen Kerzen bewertet. Intrabar-Füllungen aus der Originalumgebung werden angenähert, indem Kerzenhochs und -tiefs mit den konfigurierten Abständen verglichen werden.
- MT5-spezifische Parameter wie `Deviation` und margenbasierte Positionsgrößen wurden durch StockSharp-freundliche Gegenstücke ersetzt.

## Verwendungsempfehlungen
1. Einen Kerzentyp wählen, der dem beabsichtigten Handelshorizont entspricht. H4 wird als Standard für Parität mit dem Quellcode beibehalten.
2. Den Schwellenwert an die Volatilität des Instruments kalibrieren. Instrumente mit größeren Ticks oder Volatilität erfordern höhere Werte.
3. Den Zeitausstieg mit Stop-Loss- und Take-Profit-Abständen kombinieren, um die Risikokontrollen des originalen Advisors zu emulieren.
4. Für Assets, die in beide Richtungen handeln, beide Einsteig-Schalter aktiviert lassen, damit die Strategie Positionen umkehren kann, wenn sich der Trend ändert.

## Unterschiede zum originalen Expert Advisor
- Die Ordergrößenanpassung verwendet ein festes `OrderVolume` anstelle des MT5-Geldverwaltungsmoduls.
- Stop-Loss- und Take-Profit-Orders werden mit Kerzendaten simuliert anstatt durch sofortige MT5-Orderplatzierung.
- Die Strategie verwendet StockSharp's native gleitende Durchschnitte. Einige Glättungsoptionen (z.B. Jurik, Kaufman adaptive) werden direkt gemappt, während nicht unterstützte MT5-Varianten zur nächsten Übereinstimmung zurückfallen.
- Zeitbasierte Ausstiege stützen sich auf `MaxPositionAge` mit `TimeSpan`-Präzision anstatt auf rohe Minutenzähler.

Dieses Dokument stellt die wesentlichen Informationen bereit, die für die Konfiguration, Ausführung und Erweiterung der TrendManager TM Plus-Strategie im StockSharp-Ökosystem erforderlich sind.
