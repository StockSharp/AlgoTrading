# Ein MA-Kanal-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Ein MA-Kanal-Ausbruch-Strategie** repliziert den MetaTrader 5-Expertenberater *One MA EA* unter Verwendung von StockSharp's High-Level-Strategie-API. Das System zeichnet einen verschobenen gleitenden Durchschnitt und umgibt ihn mit einem konfigurierbaren pip-basierten Kanal. Wenn der Preis nach einem Test des Kanals auf derselben Kerze außerhalb des Kanals öffnet, eröffnet die Strategie eine Position in Ausbruchsrichtung, während optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen das Risiko automatisch verwalten.

Hauptmerkmale:
- Unterstützt mehrere Berechnungsmethoden für gleitende Durchschnitte (SMA, EMA, SMMA, LWMA).
- Ermöglicht die Wahl des Kerzenpreises (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet), der in den gleitenden Durchschnitt fließt.
- Wendet unabhängige Verschiebungen auf den gleitenden Durchschnittswert und auf die für die Signalauswertung verwendete Kerze an, was den `Current Bar`-Steuerungen des ursprünglichen EA entspricht.
- Konvertiert Pip-Abstände in absolute Preiserhöhungen unter Verwendung des `PriceStep` des Instruments und der Dezimalpräzision (3/5-Dezimalinstrumente werden automatisch auf klassische FX-Pips abgebildet).

## Handelslogik
1. **Indikatorvorbereitung**
   - Ein gleitender Durchschnitt mit Periode `MaPeriod`, Methode `MaMethodParam`, Verschiebung `MaShift` und angewendetem Preis `AppliedPriceType` wird aus der abonnierten Kerzenserie (`CandleType`) berechnet.
   - Kanal-Offsets werden von Pips in Preiserhöhungen konvertiert: `ChannelHighPips` oberhalb und `ChannelLowPips` unterhalb des verschobenen gleitenden Durchschnitts.
   - Historische Puffer erlauben die Referenzierung früherer Balken (`MaBarShift` für die MA-Serie, `PriceBarShift` für OHLC-Daten) genau wie in der MQL-Version.

2. **Signalerzeugung**
   - **Bullischer Ausbruch**: Das Tief der inspizierten Kerze bleibt zwischen der MA-Basislinie und dem oberen Kanal, während ihre Eröffnung über dem oberen Kanal liegt. Wenn kein Long-Exposure besteht (`Position <= 0`), kauft die Strategie.
   - **Bärischer Ausbruch**: Das Hoch der inspizierten Kerze bleibt zwischen der MA-Basislinie und dem unteren Kanal, während ihre Eröffnung unter dem unteren Kanal erscheint. Wenn kein Short-Exposure besteht (`Position >= 0`), verkauft die Strategie.
   - Das Ordervolumen entspricht dem konfigurierten `TradeVolume` plus der Menge, die benötigt wird, um eine entgegengesetzte Position aufzulösen, was das Hedge-to-Net-Verhalten des Quellexperten widerspiegelt.

3. **Risikomanagement**
   - `StopLossPips` und `TakeProfitPips` werden in absolute Preisabstände übersetzt und an `StartProtection` übergeben, um automatisierte Exit-Orders für jede Position zu aktivieren.
   - Bei Null-Werten wird die jeweilige Schutzorder deaktiviert.

Keine zusätzliche Exit-Logik wird angewendet; Positionen schließen nur über das Schutzmodul oder durch Umkehr in das entgegengesetzte Signal.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `MaPeriod` | Länge des gleitenden Durchschnitts. Muss > 0 sein. |
| `MaShift` | Horizontale Verschiebung des gleitenden Durchschnitts in Balken. Positive Werte verschieben den MA nach rechts. |
| `MaMethodParam` | Berechnungstyp des gleitenden Durchschnitts (`Sma`, `Ema`, `Smma`, `Lwma`). |
| `AppliedPriceType` | Kerzenpreis, der in den MA einfließt (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `MaBarShift` | Welcher historische MA-Wert verwendet werden soll (0 = aktueller verarbeiteter Balken). |
| `PriceBarShift` | Welche historische Kerze für OHLC-Werte inspiziert werden soll. |
| `ChannelHighPips` | Abstand (in Pips) vom MA zur oberen Kanalgrenze. |
| `ChannelLowPips` | Abstand (in Pips) vom MA zur unteren Kanalgrenze. |
| `StopLossPips` | Schützender Stop-Abstand in Pips. Null deaktiviert den Stop. |
| `TakeProfitPips` | Gewinnzielabstand in Pips. Null deaktiviert das Ziel. |
| `TradeVolume` | Ordergröße in Strategie-Volumeneinheiten (auf `Strategy.Volume` abgebildet). |
| `CandleType` | Für Berechnungen und Signale verwendete Kerzendatenserie. |

## Implementierungshinweise
- Pip-zu-Preis-Konvertierung verwendet `PriceStep` und `Decimals`. Bei Symbolen mit 3 oder 5 Dezimalstellen entspricht der Pip-Wert `PriceStep * 10`, andernfalls `PriceStep`.
- Historische Puffer werden mit Gleitfenstern fester Größe implementiert, sodass die Strategie per Index auf Balken zugreifen kann, ohne sich auf Indikator-`GetValue`-Aufrufe zu verlassen, was den Projektrichtlinien entspricht.
- Die Strategie basiert ausschließlich auf fertigen Kerzen; unfertige Kerzen werden ignoriert, um vorzeitige Signale zu vermeiden.
- Optionales Chart-Rendering zeichnet Preiskerzen und ausgeführte Trades, wenn ein Chart-Bereich in der Host-Anwendung verfügbar ist.

## Nutzungstipps
- Sicherstellen, dass das abonnierte Wertpapier gültige `PriceStep`/`Decimals`-Daten bereitstellt; andernfalls pip-basierte Parameter manuell anpassen.
- `MaPeriod`, Kanalabstände und Balkenverschiebungen optimieren, um das Ausbruchsverhalten an spezifische Märkte oder Zeitrahmen anzupassen.
- Mit Risikokontrolle auf Portfolioebene kombinieren, wenn live eingesetzt wird, da die Strategie immer eine Nettoposition pro Instrument hat.
