# MA-Kreuzung Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den Expert Advisor "MA Cross" aus MetaTrader 5 (Datei `MA Cross.mq5`) innerhalb des StockSharp-Frameworks. Das System beobachtet zwei konfigurierbare gleitende Durchschnitte und gibt Marktorders aus, wenn der schnelle Durchschnitt den langsamen kreuzt. Die Implementierung behält das ursprüngliche Maß an Flexibilität bei, indem sie die Methode des gleitenden Durchschnitts, den angewandten Preis und den Indikator-Shift für beide Kurven zugänglich macht.

## Strategielogik
1. Abonnieren eines einzelnen Kerzenstroms, der durch den Parameter `CandleType` definiert wird.
2. Berechnung des schnellen und langsamen gleitenden Durchschnitts auf jeder abgeschlossenen Kerze. Jeder gleitende Durchschnitt kann eine von vier Methoden verwenden (einfach, exponentiell, geglättet oder linear gewichtet) und liest einen der MetaTrader-Stil angewandten Preise (Schlusskurs, Eröffnung, Hoch, Tief, Median, typisch oder gewichtet).
3. Speicherung der jüngsten Indikatorwerte unter Berücksichtigung des konfigurierten Shifts, sodass Kreuzungstests auf Werten früherer Bars durchgeführt werden können, wenn erforderlich.
4. Erkennung eines bullischen Kreuzungspunkts, wenn sich der schnelle Durchschnitt von unter den geshifteten langsamen Durchschnitt nach oben bewegt. Erkennung eines bearischen Kreuzungspunkts beim umgekehrten Bewegung.
5. Ausgabe von Marktorders nur nachdem beide Indikatoren vollständig gebildet sind und die Strategie online ist. Long-Signale schließen eine bestehende Short-Position und öffnen eine Long-Position von `OrderVolume`. Short-Signale schließen jede bestehende Long-Position und öffnen eine Short-Position derselben Größe.

Die Strategie arbeitet ausschließlich mit abgeschlossenen Kerzen und inspiziert niemals unfertige Daten. Die Schutzlogik wird durch `StartProtection()` aktiviert, um sicherzustellen, dass StockSharp die offene Position auf abnormale Bedingungen überwacht.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `FastPeriod` | 3 | Periode des schnellen gleitenden Durchschnitts. |
| `SlowPeriod` | 13 | Periode des langsamen gleitenden Durchschnitts. |
| `FastMethod` | Simple | Gleitender Durchschnitt-Methode für die schnelle Linie (einfach, exponentiell, geglättet oder linear gewichtet). |
| `SlowMethod` | LinearWeighted | Methode des gleitenden Durchschnitts für die langsame Linie. |
| `FastPriceType` | Close | Angewandter Preis für die schnelle Linie (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet). |
| `SlowPriceType` | Median | Angewandter Preis für die langsame Linie. |
| `FastShift` | 0 | Anzahl abgeschlossener Bars zum Verschieben des schnellen Durchschnitts nach links. |
| `SlowShift` | 0 | Anzahl abgeschlossener Bars zum Verschieben des langsamen Durchschnitts nach links. |
| `OrderVolume` | 1 | Volumen für jede Marktorder. |
| `CandleType` | 1-Minuten-Zeitrahmen | Von der Strategie verarbeitete Kerzendatenserie. |

Alle Parameter können innerhalb von StockSharp optimiert werden, da der Konstruktor sie mit `StrategyParam`-Helfern registriert.

## Handelsregeln
- **Long-Einstieg:** Ausgelöst, wenn der schnelle Durchschnitt den langsamen Durchschnitt gemäß den shift-bereinigten Werten von unten nach oben kreuzt. Wenn die Strategie bereits short ist, sendet sie eine einzelne Kauf-Marktorder, die die Short-Exposition schließt und eine neue Long-Position eröffnet. Wenn keine Position vorhanden ist, kauft sie genau `OrderVolume`.
- **Short-Einstieg:** Ausgelöst, wenn der schnelle Durchschnitt den langsamen von oben kreuzt. Bestehende Long-Exposition wird über eine einzelne Verkaufs-Marktorder umgekehrt; andernfalls eröffnet die Strategie einen neuen Short-Trade mit `OrderVolume`.
- **Kein zusätzliches Skalieren:** Einmal positioniert werden gleichgerichtete Signale ignoriert, bis die entgegengesetzte Kreuzung erfolgt.
- **Ausführungsstil:** Orders werden mit `BuyMarket` oder `SellMarket` gesendet. Die Strategie konfiguriert keine Stop-Loss- oder Take-Profit-Niveaus; Risikomanagement kann bei Bedarf durch andere StockSharp-Module ergänzt werden.

## Konvertierungshinweise
- Die Indikatorerstellung spiegelt die MetaTrader `iMA`-Aufrufe wider. Die benutzerdefinierte `MovingAverageMethods`-Enumeration ordnet `MODE_SMA`, `MODE_EMA`, `MODE_SMMA` und `MODE_LWMA` den StockSharp-Klassen `SimpleMovingAverage`, `ExponentialMovingAverage`, `SmoothedMovingAverage` und `WeightedMovingAverage` zu.
- Die Behandlung des angewandten Preises reproduziert die MetaTrader `ENUM_APPLIED_PRICE`-Optionen, indem Median-, typische und gewichtete Preise direkt aus den Kerzendaten berechnet werden.
- Die Shift-Parameter verwenden die ursprüngliche Logik wieder: Die Strategie puffert Indikatorwerte und ruft die Einstiegs- und Ausstiegsvergleiche aus früheren Bars ab, wenn `FastShift` oder `SlowShift` positiv sind.
- Die Positionsverwaltungslogik entspricht dem ursprünglichen Ansatz, bei dem entgegengesetzte Signale zuerst die bestehende Position schließen und dann eine Position in die neue Richtung auf derselben Bar eröffnen.
