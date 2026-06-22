# Ma SAR ADX Bind Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine Konvertierung des ursprünglichen **MaSarADX.mq5** MetaTrader 5-Expertenberaters auf die StockSharp High-Level-API. Das System kombiniert einen einfachen gleitenden Durchschnitt als Trendfilter mit Directional Movement Index (ADX)-Signalen und dem Parabolic SAR Trailing Stop. Handelsentscheidungen werden nur bei abgeschlossenen Kerzen ausgewertet, was das Verhalten des "ersten Tick einer neuen Bar" aus der MQL-Version repliziert. Wenn der Kerzenschlusskurs sowohl mit dem Trendtrend des gleitenden Durchschnitts als auch mit dem gerichteten Gleichgewicht des ADX übereinstimmt, wird eine Position geöffnet. Parabolic SAR leitet sowohl Traderichtung als auch Exits, indem er eine vollständige Liquidation erzwingt, wenn der Preis auf die entgegengesetzte Seite der SAR-Punkte kreuzt.

## Indikatoren und Daten
- **Einfacher gleitender Durchschnitt (SMA)** – liefert den primären Trendrichtungsfilter. Standardlänge: 100 Kerzen.
- **Average Directional Index (ADX)** – liefert +DI und −DI zur Bestätigung der Richtungsstärke. Standardlänge: 14.
- **Parabolic SAR** – dient als Stop-and-Reverse-Overlay und definiert Ausstiegsbedingungen. Standardbeschleunigung: 0.02; maximale Beschleunigung: 0.10.
- **Kerzen** – jeder Zeitrahmen kann angefordert werden. Standardmäßig abonniert die Strategie 1-Stunden-Kerzen, aber der Parameter kann angepasst werden.

Die Implementierung abonniert StockSharp-Kerzenströme und bindet alle drei Indikatoren mit dem `BindEx`-Helper, sodass jeder Callback synchronisierte Werte für dieselbe Kerze empfängt.

## Handelsstrategie
### Long-Einstieg
1. Kerzenschlusskurs liegt über dem gleitenden Durchschnitt.
2. +DI ist größer oder gleich −DI, was bullischen Richtungsdruck anzeigt.
3. Kerzenschlusskurs liegt über dem Parabolic SAR-Wert.
4. Keine Long-Position ist aktuell offen (`Position <= 0`).

Wenn alle Regeln übereinstimmen, wird eine Kauf-Marktorder für das konfigurierte Basisvolumen plus Größe zur Abdeckung einer Short-Position gesendet.

### Short-Einstieg
1. Kerzenschlusskurs liegt unter dem gleitenden Durchschnitt.
2. +DI ist kleiner oder gleich −DI, was bärischen Richtungsdruck anzeigt.
3. Kerzenschlusskurs liegt unter dem Parabolic SAR-Wert.
4. Keine Short-Position ist aktuell offen (`Position >= 0`).

Eine Verkauf-Marktorder wird platziert, wenn alle Short-Regeln übereinstimmen.

### Ausstiege
- **Long-Positionen** werden sofort geschlossen, wenn der Preis unter den Parabolic SAR fällt.
- **Short-Positionen** werden gedeckt, wenn der Preis über den Parabolic SAR steigt.

Es werden keine separaten Stop-Loss- oder Take-Profit-Level hinzugefügt; der SAR-Kreuzungspunkt ist das einzige Ausstiegssignal, dem ursprünglichen Expertenberater folgend. Da Exits vor neuen Einstiegen ausgewertet werden, wird die Strategie nicht auf derselben Kerze von Short zu Long (oder umgekehrt) wechseln.

## Parameter
| Name | Beschreibung | Standard | Hinweise |
| --- | --- | --- | --- |
| `MaPeriod` | Länge des einfachen gleitenden Durchschnitts zur Definition des Trendfilters. | 100 | Optimierbar, muss größer als null sein. |
| `AdxPeriod` | Periode der ADX-Berechnung, die +DI und −DI erzeugt. | 14 | Optimierbar, muss größer als null sein. |
| `SarStep` | Beschleunigungsfaktor und Inkrement für den Parabolic SAR. | 0.02 | Entspricht dem MQL `step`-Parameter. |
| `SarMax` | Maximaler Beschleunigungsfaktor für Parabolic SAR. | 0.10 | Spiegelt die MQL `maximum`-Einstellung wider. |
| `Volume` | Basis-Ordergröße für neue Einstiege. | 1 | Ersetzt die margenbasierte Lot-Größenberechnung der MetaTrader-Version. Die tatsächliche Ordergröße ist `Volume + |Position|`, sodass Umkehrungen das bestehende Engagement glätten. |
| `CandleType` | Der über StockSharp abonnierte Kerzendatentyp. | 1 Stunde | Auf jeden Zeitrahmen einstellbar. |

## Implementierungshinweise
- Die Indikatorverarbeitung verwendet StockSharps High-Level-`BindEx`-Pipeline und stellt sicher, dass SMA, ADX und SAR im Gleichschritt ohne manuelles Puffern aktualisiert werden.
- Exits werden auch dann ausgeführt, wenn `AllowTrading` vorübergehend deaktiviert ist, um Risikokontrollen jederzeit aktiv zu halten.
- Grafik-Hilfstools sind enthalten: das primäre Panel zeigt Preis, SMA und SAR, während ein sekundäres Panel den ADX-Indikator für Diagnosen anzeigt.
- Log-Anweisungen beschreiben jede Handelsentscheidung mit den zugrunde liegenden Indikatorwerten zur Vereinfachung von Vorwärtstests und Debugging.

## Verwendungsrichtlinien
1. Hängen Sie die Strategie an ein Wertpapier und Portfolio im Designer oder Backtester.
2. Passen Sie den Kerzentyp an Ihren Handelshorizont an (z.B. M15-, H1- oder D1-Kerzen).
3. Passen Sie den gleitenden Durchschnittszeitraum, den ADX-Zeitraum und SAR-Parameter an die Volatilität des Instruments an.
4. Setzen Sie den `Volume`-Parameter auf Ihre bevorzugte Positionsgröße. Wenn Sie das adaptive Money Management aus dem ursprünglichen Skript benötigen, integrieren Sie Ihre eigene portfoliobasierte Größenbestimmung vor dem Senden von Orders.
5. Führen Sie die Strategie aus. Trades werden erst ausgelöst, nachdem alle Indikatoren ausreichend historische Werte für ihre Formierung erzeugt haben.

## Unterschiede zum ursprünglichen Expertenberater
- Die margenbasierte Lot-Berechnung wurde durch einen festen `Volume`-Parameter ersetzt, um die Strategie broker-neutral in StockSharp zu halten.
- Trade-Verwaltung, Indikatorwerte und die Auswertungsreihenfolge (Ausstieg vor Einstieg) folgen strikt der MetaTrader-Referenzlogik.
- Alle Kommentare im Quellcode sind auf Englisch, um den Projektrichtlinien zu entsprechen.
