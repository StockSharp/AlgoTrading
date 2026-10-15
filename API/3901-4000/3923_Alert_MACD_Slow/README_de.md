# Warnung MACD Langsame Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Alert MACD Slow** reproduziert den MetaTrader 4 Experten `Alert_MACD_Slow.mq4`. Es beobachtet die Hauptlinie MACD und zwei exponentielle gleitende Durchschnitte und löst Textwarnungen aus, wenn der Indikatorstapel einen möglichen Ausbruch signalisiert. Es werden keine Bestellungen übermittelt – die Konvertierung bleibt dem ursprünglichen Ratgeber treu, der nur Popup-Meldungen anzeigte.

## Kernidee

1. Abonnieren Sie die ausgewählte Kerzenserie und geben Sie einen MACD(3, 20, 9) zusammen mit schnellen und langsamen EMAs (20 und 65 Perioden) ein.
2. Zwischenspeichern Sie MACD-Werte für die vorherigen vier abgeschlossenen Kerzen, um die vom MQL-Code verwendeten Steigungsübergänge auszuwerten.
3. Speichern Sie die Höchst- und Tiefstwerte der letzten beiden Kerzen, um die Breakout-Filter `High[1]/High[2]` und `Low[1]/Low[2]` zu emulieren.
4. Wenn der schnelle EMA über (oder unter) dem langsamen EMA bleibt und der Kerzenschluss die gespeicherten Hochs (oder Tiefs) durchbricht, während MACD unter der Nulllinie nach oben (oder nach unten) dreht, protokollieren Sie die entsprechende Warnmeldung.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `MacdFastPeriod` | `3` | Schnelle EMA-Länge innerhalb der MACD-Berechnung. |
| `MacdSlowPeriod` | `20` | Langsame EMA-Länge, die von MACD verwendet wird. |
| `MacdSignalPeriod` | `9` | Signalglättungszeitraum des MACD. |
| `QuickEmaPeriod` | `20` | Zeitraum des trendfolgenden schnellen EMA (`Ma_Quick`). |
| `SlowEmaPeriod` | `65` | Zeitraum des langsamen EMA-Trendfilters (`Ma_Slow`). |
| `CandleType` | `TimeFrame(30m)` | Kerzenquelle wird an die Indikatorkette übergeben; Wählen Sie einen Zeitrahmen, der zu Ihrem Diagramm passt. |

## Details zur Alarmlogik

- **MACD Steigungsspeicher**: Die Strategie verschiebt die vorherigen MACD-Werte intern, anstatt `GetValue` aufzurufen, und erfüllt so die Konvertierungsrichtlinien, während die ursprünglichen Vergleiche (`Macd_1 > Macd_2` usw.) erhalten bleiben.
- **Breakout-Check**: Schlusskurse über früheren Höchstständen oder unter früheren Tiefstständen werden als Proxy für die Bid/Ask-Checks von MetaTrader behandelt, bei denen der Live-Kurs im Vergleich zu historischen Candle-Extremen verwendet wurde.
- **Trendfilter**: Die Warnung wird nur ausgelöst, wenn der schnelle EMA auf der richtigen Seite des langsamen EMA liegt und mit den Long/Short-Filtern im MQL-Experten übereinstimmt.
- **Protokollierung**: Benachrichtigungen werden über `AddInfoLog` gesendet. Sie umfassen die vier zwischengespeicherten MACD-Werte und die Breakout-Ebenen, um das Debuggen und Backtesting zu erleichtern.
- **Kein Handel**: Da der Quellberater nie Geschäfte platziert hat, bleibt die Strategie bei der StockSharp-Konvertierung flach und konzentriert sich ausschließlich auf die Signalisierung.

## Typische Verwendung

1. Hängen Sie die Strategie an ein Symbol an, konfigurieren Sie den Kerzentyp für den gewünschten Zeitrahmen und behalten Sie die Standardindikatorperioden bei oder passen Sie sie zum Experimentieren an.
2. Starten Sie die Strategie und warten Sie, bis sich die Indikatoren MACD und EMA gebildet haben (mehrere Kerzen sind erforderlich, da MACD eine Historie erfordert).
3. Sehen Sie sich das Journal an: Wenn ein bullisches Setup auftritt, sehen Sie `SET UP LONG`, während ein bärisches Setup `SET UP SHORT_VALUE` ergibt. Das Suffix spiegelt den ursprünglichen Warntext wider.
4. Nutzen Sie die gedruckten Diagnosen, um zu entscheiden, ob Sie manuell handeln oder die Strategie mit benutzerdefinierter Automatisierung verketten möchten.

## Klassifizierung

- **Kategorie**: Warnungen/Bestätigung von Trendausbrüchen
- **Handelsrichtung**: Keine (nur Signal)
- **Ausführungsstil**: Ereignisgesteuert für fertige Kerzen
- **Datenanforderungen**: Kerzenserie kompatibel mit dem gewählten `CandleType`
- **Komplexität**: Moderat (mehrere Indikatorfilter, aber einfache Statusbehandlung)
- **Risikomanagement**: Nicht anwendbar (keine Positionen eröffnet)

Dieser Port behält das Benachrichtigungsverhalten des MQL-Experten bei und nutzt gleichzeitig StockSharp-Abonnements, Indikatorbindungen und Protokollierungsdienstprogramme.
