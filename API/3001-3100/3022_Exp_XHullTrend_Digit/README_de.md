# Exp XHullTrend Digit Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MQL5-Experten `Exp_XHullTrend_Digit.mq5` aus `MQL/22117`.
- Verwendet die StockSharp High-Level-API mit dem benutzerdefinierten `XHullTrendDigitIndicator`, der die originale XHullTrend Digit-Logik repliziert.
- Fokussiert auf mittelfristiges Trendfolgen auf dem konfigurierten Indikator-Zeitrahmen (standardmäßig 8 Stunden).

## Indikatorlogik
1. Der Preis wird aus der ausgewählten Kerzenquelle genommen (standardmäßig Close).
2. Zwei gleitende Durchschnitte mit den Längen `BaseLength` und `BaseLength / 2` werden mit der gewählten Glättungsmethode berechnet (einfach, exponentiell, geglättet oder gewichtet).
3. Eine Hull-Projektion `2 * shortMA - longMA` wird zweimal geglättet: erst durch `SignalLength`, dann durch `sqrt(BaseLength)`.
4. Beide resultierenden Linien werden auf das nächste Vielfache des Instrument-Schritts, skaliert mit `10^RoundingDigits`, gerundet, um die Ziffernrundung der MQL5-Version zu imitieren.
5. Wenn die Rundung gleiche Werte produziert, während sich die Rohwerte unterscheiden, wird die schnellere Linie um einen Schritt in Richtung der Differenz verschoben, damit der Übergang erkennbar bleibt.

## Handelsregeln
- Signale werden nur auf geschlossenen Kerzen ausgewertet.
- `SignalBar` definiert, wie viele Bars zurück für die Kreuzerkennung verwendet werden (1 = die vorherige abgeschlossene Bar gegen die Bar davor).
- Long-Einstieg: Vorherige schnelle Linie über der langsamen **und** die schnelle Linie der ausgewählten Bar auf oder unter der langsamen (Aufwärtskreuzung). Short-Positionen werden optional gleichzeitig geschlossen.
- Short-Einstieg: Vorherige schnelle Linie unter der langsamen **und** die schnelle Linie der ausgewählten Bar auf oder über der langsamen (Abwärtskreuzung). Long-Positionen werden optional gleichzeitig geschlossen.
- Long-Ausstieg: Wenn die vorherige schnelle Linie unter die langsame fällt.
- Short-Ausstieg: Wenn die vorherige schnelle Linie über die langsame steigt.
- Erscheint ein Umkehrsignal bei einer Gegenposition, sendet die Strategie den Schließauftrag gefolgt von einem Auftrag, der die Position in die neue Richtung umdreht.

## Parameter
- `OrderVolume` – Volumen für Markteinstiege.
- `StopLoss` / `TakeProfit` – optionale Schutzabstände in Preisschritten (konvertiert zu StockSharp `UnitTypes.Step`).
- `EnableBuyEntry`, `EnableSellEntry` – neue Positionen in jeder Richtung erlauben oder blockieren.
- `EnableBuyExit`, `EnableSellExit` – automatische Ausstiege für Long- und Short-Seiten steuern.
- `CandleType` – Zeitrahmen für Indikatorberechnungen (standardmäßig 8-Stunden-Zeitrahmen).
- `BaseLength` – Basis-Glättungslänge für den Indikator (entspricht `XLength` in MQL5).
- `SignalLength` – Länge der intermediären Hull-Glättung (`HLength` in MQL5).
- `PriceSource` – für Berechnungen verwendeter Kerzenpreis (Close/Open/High/Low/Typical/Weighted/Median/Average).
- `SmoothMethod` – gleitender Durchschnittstyp für alle Glättungsstufen (einfach, exponentiell, geglättet, gewichtet).
- `Phase` – für Kompatibilität beibehalten; kein Effekt bei den unterstützten Glättungstypen.
- `RoundingDigits` – Anzahl zusätzlicher Ziffernkorrekturen beim Runden.
- `SignalBar` – Bar-Offset für Signalauswertung (0 = aktuelle geschlossene Bar, 1 = vorherige Bar, etc.).

## Risikomanagement
- Optionaler Stop-Loss und Take-Profit werden vom eingebauten `StartProtection`-Helper mit schrittbasierten Abständen verwaltet.
- Das Volumen kann über `OrderVolume` angepasst werden, um der Zielinstrumentgröße zu entsprechen.

## Hinweise
- Der benutzerdefinierte Indikator reproduziert das Rundungsverhalten des Originalskripts; stellen Sie sicher, dass `Security.PriceStep` für genaues Runden konfiguriert ist.
- Nur SMA, EMA, SMMA (RMA) und LWMA-Glättung sind implementiert, da die StockSharp-Standardbibliothek diese von Haus aus bietet. Andere exotische Glättungsmodi aus der MQL5-Quelle können bei Bedarf später hinzugefügt werden.
- Funktioniert auf jedem Instrument, das Kerzen für den gewählten Zeitrahmen liefert. Passen Sie Rundungsziffern und Basislänge an, wenn Sie zwischen Assets mit unterschiedlichen Tick-Größen wechseln.
