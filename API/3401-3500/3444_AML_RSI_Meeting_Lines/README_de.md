# AML RSI Meeting Lines-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **AML RSI Meeting Lines Strategy** ist eine StockSharp-Portierung des MetaTrader 5-Expertenberaters `Expert_AML_RSI.mq5`. Das ursprüngliche System kombiniert die Erkennung japanischer Candlestick-Muster mit dem Relative Strength Index (RSI), um bullische und bärische „Meeting Lines“-Umkehrungen zu handeln. Diese Konvertierung behält die Kernhandelslogik bei und passt sie gleichzeitig an StockSharps High-Level-API mit Kerzenabonnements und integrierten Indikatoren an.

## Handelslogik
- Abonniert einen konfigurierbaren Kerzentyp und verarbeitet nur fertige Kerzen.
- Berechnet einen einfachen gleitenden Durchschnitt der Kerzenkörpergrößen, um „lange“ Kerzen zu erkennen, die Meeting-Lines-Muster bilden.
- Verfolgt RSI-Werte der beiden zuletzt abgeschlossenen Kerzen für Bestätigungs- und Ausstiegssignale.
- **Bulnisches Setup**: Die Umkehrung der Meeting Lines mit zwei Kursstäben und RSI unter der bullischen Schwelle löst Long-Einstiege aus.
- **Bearisches Setup**: Ein gespiegeltes Muster mit RSI über der bärischen Schwelle löst Short-Einstiege aus.
- **Positionsausstiege**: RSI Crossovers durch konfigurierbare untere und obere Ebenen schließen offene Trades in die entgegengesetzte Richtung.
- Verwendet die Helfer `BuyMarket`, `SellMarket` und `ClosePosition`, um die Belichtung zu verwalten, und ändert die Positionsgröße automatisch, wenn ein gegenteiliges Signal auftritt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zeitrahmen zur Bewertung von Candlestick-Mustern. | Zeitrahmen 1 Stunde |
| `RsiPeriod` | RSI Lookback-Länge. | 11 |
| `BodyAveragePeriod` | Anzahl der Kerzen für die durchschnittliche Körpergröße. | 3 |
| `BullishRsiLevel` | Maximum RSI, das bullische Meeting-Linien bestätigt. | 40 |
| `BearishRsiLevel` | Minimum RSI, das die rückläufigen Meeting-Linien bestätigt. | 60 |
| `LowerExitLevel` | RSI-Level, das Shorts bei Aufwärtskreuzen schließt. | 30 |
| `UpperExitLevel` | RSI-Level, das Long-Positionen bei Abwärtskreuzungen schließt. | 70 |

Alle Parameter werden als `StrategyParam<T>`-Objekte angezeigt, sodass sie im StockSharp-Designer optimiert werden können.

## Risikomanagement
- `StartProtection()` wird in `OnStarted` aufgerufen, um die integrierte Positionsüberwachung des Frameworks zu aktivieren.
- Die Strategie schließt das bestehende Risiko immer dann, wenn RSI die konfigurierten Exit-Grenzen überschreitet, bevor neue Signale berücksichtigt werden.
- Marktaufträge kehren die Position automatisch um, indem sie den absoluten Wert des aktuellen Engagements zum konfigurierten Volumen addieren.

## Konvertierungshinweise
- Bei der Candlestick-Mittelung wird `SimpleMovingAverage` mit absoluten Kerzenkörpern gespeist, was den `AvgBody`-Helfer aus der MQL5-Quelle widerspiegelt.
- Die RSI-Bestätigung basiert auf den Werten der beiden vorherigen Kerzen und reproduziert die `RSI(1)`- und `RSI(2)`-Prüfungen des ursprünglichen Experten.
- Alle Kommentare im Code wurden in Englisch umgeschrieben und die Struktur folgt den Repository-Anforderungen von dateibezogenen Namespaces mit Tabulatoreinzug.

## Nutzung
1. Hängen Sie die Strategie an ein Wertpapier in StockSharp an und wählen Sie den gewünschten Kerzentyp aus.
2. Konfigurieren Sie RSI und Ausstiegsschwellenwerte, um sie an den Handelsplatz oder die Volatilität des Instruments anzupassen.
3. Führen Sie die Strategie zunächst im Papierhandel aus, um die Mustererkennung zu validieren, bevor Sie zum Live-Handel oder zur Optimierung übergehen.
4. Verwenden Sie die bereitgestellten Parameter während der Optimierung, um die RSI-Werte und die durchschnittliche Körperlänge für verschiedene Märkte zu optimieren.

## Haftungsausschluss
Diese Strategie dient ausschließlich Bildungszwecken. Testen Sie gründlich anhand historischer Daten und in simulierten Umgebungen, bevor Sie es in Live-Kapital einsetzen.
