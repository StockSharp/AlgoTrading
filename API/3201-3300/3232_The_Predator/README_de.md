# The Predator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine High-Level-Übersetzung des MQL-Expert-Advisors **„The Predator"** für StockSharp. Das ursprüngliche System mischt Trendrichtungsfilter mit Momentum, Bollinger-Bändern und stochastischen Oszillatoren. Zwei unabhängige Einstiegsvorlagen (Strategie 1 und Strategie 2) sind verfügbar und replizieren die auswählbaren Modi innerhalb der MQL-Implementierung.

Die Konvertierung konzentriert sich auf kerzenbasierte Verarbeitung unter Verwendung von StockSharp-Abonnements und Indikatorbindungen. Alle Berechnungen werden auf einer einzigen konfigurierbaren Kerzenserie durchgeführt.

## Kernindikatoren

- **Linear gewichtete gleitende Durchschnitte (LWMA)** – schnelle/langsame Struktur zur Bestätigung des kurzfristigen Trends.
- **Direktionaler Bewegungsindex + Durchschnittlicher Direktionaler Index (DMI/ADX)** – direktionale Stärke und Trendbestätigung.
- **Momentum (Periode standardmäßig 14)** – misst den Abstand vom neutralen 100-Niveau für Ausbruchs- und Rücklauflogik.
- **Bollinger-Bänder** – zwei Hüllkurven (eng und weit) zur Kontexterkennung und vorherigen Kerzenposition, besonders für Strategie 2.
- **Stochastischer Oszillator** – zusätzlicher Filter für Strategie 2 zur Anforderung von Momentum-Erschöpfungszonen.
- **MACD** – Trendmomentum-Bestätigung durch Vergleich der MACD-Linie mit ihrer Signallinie.

## Handelslogik

### Gemeinsame Filter

1. Nur abgeschlossene Kerzen verarbeiten.
2. Erfordern, dass die ausgewählten Indikatoren vor dem Handel geformt sind (`IsFormedAndOnlineAndAllowTrading`).
3. ADX muss größer als der konfigurierte Schwellenwert sein.
4. Der Momentum-Abweichungsverlauf wird für die letzten drei Werte geführt, um die MQL-Prüfungen ohne Aufruf von `GetValue` auf Indikatoren nachzubilden.

### Strategie 1

- **Long-Einstiege** wenn:
  - ADX über Schwellenwert und +DI übersteigt −DI.
  - Schneller LWMA über langsamem LWMA.
  - Momentum-Abweichung über dem Kauf-Schwellenwert bei einem der letzten drei Werte.
  - MACD-Linie über ihrer Signallinie.
- **Short-Einstiege** spiegeln das oben Genannte mit umgekehrten Vorzeichen wider.

### Strategie 2

- **Long-Einstiege** erfordern zusätzlich:
  - Vorheriger Kerzenschluss an oder über der vorherigen unteren Bollinger-Grenze des engen Bandes.
  - Stochastische Signal- und Hauptlinien beide über dem oberen Schwellenwert.
  - Momentum-Abweichung unter dem Kauf-Schwellenwert bei einem der letzten drei Werte (Suche nach Rücksetzern innerhalb von Trends).
- **Short-Einstiege** erfordern:
  - Vorheriger Kerzenschluss an oder unter der vorherigen oberen Bollinger-Grenze des engen Bandes.
  - Stochastische Signallinie über dem oberen Schwellenwert, während die Hauptlinie unter dem unteren Schwellenwert liegt.
  - Momentum-Abweichung unter dem Verkauf-Schwellenwert bei einem der letzten drei Werte.

### Positionshandling

- Die Strategie storniert alle ausstehenden aktiven Orders, bevor ein neuer Trade geöffnet wird.
- Wenn ein Umkehrsignal auftritt, schließt die Strategie das aktuelle Engagement und öffnet eine neue Position in der entgegengesetzten Richtung mit einer kombinierten Market-Order.

## Risikomanagement

- `StartProtection` konfiguriert:
  - Anfängliche Stop-Loss-Distanz in Pips.
  - Anfängliche Take-Profit-Distanz in Pips.
  - Optionaler Trailing-Stop, der einen festen Pip-Betrag folgt, sobald er aktiviert ist.
- Risikodistanzen werden mit dem Sicherheitspreisschritt in absolute Preiseinheiten umgerechnet.
- Die geldbasierten Break-Even- und Trailing-Module des ursprünglichen EA werden durch diese pip-basierten Schutzmaßnahmen ersetzt (dokumentierter Unterschied unten).

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Mode` | Wählt Strategie 1 (Trendausbruch) oder Strategie 2 (Rücksetzer mit stochastischen Filtern). |
| `FastMaLength`, `SlowMaLength` | LWMA-Längen zur Bestimmung der Trendrichtung. |
| `DmiPeriod`, `AdxSmoothing` | Parameter des Direktionalen Bewegungsindex. |
| `MomentumPeriod` | Lookback des Momentum-Indikators. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Mindestabweichung von 100 zur Signalakzeptanz. |
| `AdxThreshold` | Mindest-ADX-Niveau für einen handelbaren Trend. |
| `BollingerPeriod`, `TightBandWidth`, `WideBandWidth` | Bollinger-Band-Einstellungen für Kontextfilter. |
| `StochasticLength`, `StochasticSmooth`, `StochasticUpper`, `StochasticLower` | Parameter für den stochastischen Oszillator in Strategie 2. |
| `TradeVolume` | Volumen bei Market-Orders. |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Risikodistanzen (mit Instrumentenschritt in Preiseinheiten umgerechnet). |
| `CandleType` | Von der Strategie verwendete Datenserie. |

## Unterschiede zur MQL-Version

- Geldbasierte Take-Profit-, Stop-Loss- und Trailing-Werte werden in Pip-Distanzen übersetzt, die von `StartProtection` verwaltet werden.
- Break-Even-Anpassungen und Benachrichtigungs-E-Mails/-Push-Nachrichten sind nicht portiert (in der High-Level-API nicht verfügbar).
- Der MQL-Experte rief MACD und Momentum auf höheren Zeitrahmen auf. In StockSharp läuft die Logik nur auf der konfigurierten Kerzenserie; Multi-Timeframe-Daten können bei Bedarf durch zusätzliche Abonnements hinzugefügt werden.
- Order-Volumen-Optimierung und martingaleartige Größenbestimmung sind nicht implementiert; die StockSharp-Version verwendet einen festen `TradeVolume`-Parameter.

## Verwendung

1. Connector und Portfolio wie in anderen StockSharp-Beispielen erstellen.
2. `ThePredatorStrategy` instanziieren, `Security`, `Portfolio` und gewünschte Parameter zuweisen.
3. Strategie starten. Visualisierung ist optional, aber verfügbar wenn ein Diagrammbereich bereitgestellt wird.

Die Übersetzung hält den Entscheidungsbaum getreu zum Original, während StockSharp-Best-Practices wie Indikatorbindung und `StartProtection` für Risiko übernommen werden. Schwellenwerte an das gewählte Instrument und den Zeitrahmen anpassen.
