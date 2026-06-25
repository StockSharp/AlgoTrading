# Strategie mit Blau TStoch Indikator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Port des MetaTrader 5 Expert Advisors `Exp_BlauTStochI` zur StockSharp High-Level-API.
- Handelt den Blau Triple Stochastic Index (William Blau) auf konfigurierbaren Zeitrahmen.
- Unterstützt zwei Ausführungsmodi: **Breakdown** (Nulllinienausbrüche) und **Twist** (Steigungsumkehrungen).
- Positionsberechtigungen reproduzieren die ursprünglichen Expert Advisor-Flags (unabhängige Schalter zum Öffnen/Schließen von Long- und Short-Trades).

## Indikatoraufbau
- Berechnet eine Momentumreihe als `angewendeter Preis - Tiefstkurs` über `MomentumLength` Balken und deren Bereich `Höchstkurs - Tiefstkurs`.
- Wendet drei aufeinanderfolgende Glättungsstufen sowohl auf Zähler als auch Nenner an.
- Unterstützte Glättungsmethoden: Exponentiell (EMA), Einfach (SMA), Geglättet/Laufend (SMMA) und Linear Gewichtet (LWMA).
- Die ursprünglichen MQL-Optionen (JJMA, JurX, ParMA, T3, VIDYA, AMA) werden **nicht** reproduziert; der `Phase`-Parameter wird aus Kompatibilitätsgründen beibehalten, aber ignoriert.
- Angewendete Preisoptionen entsprechen den MQL-Enumerationen (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet, Einfach, Quartil, Trendfolgevarianten, DeMark).
- Endgültiger Indikatorwert: `100 * geglättetesStoch / geglättetesRange - 50`.

## Handelsregeln
### Breakdown-Modus
- Inspiziert den Indikator auf dem Balken, der durch `SignalBar` definiert ist (Standard 1, d. h. die letzte geschlossene Kerze).
- **Long-Einstieg:** vorheriger Wert (`SignalBar+1`) über null **und** aktueller Wert (`SignalBar`) kreuzt unter oder gleich null.
- **Short-Einstieg:** vorheriger Wert unter null **und** aktueller Wert kreuzt über oder gleich null.
- **Long-Ausstieg:** vorheriger Wert unter null und Long-Ausstiege erlaubt.
- **Short-Ausstieg:** vorheriger Wert über null und Short-Ausstiege erlaubt.

### Twist-Modus
- **Long-Einstieg:** Indikator steigt (`value[SignalBar+1] < value[SignalBar+2]`) und der neueste Wert nicht niedriger als der vorherige.
- **Short-Einstieg:** Indikator fällt (`value[SignalBar+1] > value[SignalBar+2]`) und der neueste Wert nicht höher als der vorherige.
- **Long-Ausstieg:** Indikatorsteigung dreht nach unten (`value[SignalBar+1] > value[SignalBar+2]`).
- **Short-Ausstieg:** Indikatorsteigung dreht nach oben (`value[SignalBar+1] < value[SignalBar+2]`).

### Positionsmanagement
- Einstiege kehren bestehende entgegengesetzte Positionen um, indem die absolute Positionsgröße zum konfigurierten `Volume` hinzugefügt wird.
- Ausstiege schließen die gesamte bestehende Position mit Marktorders.
- Die Handelsverarbeitung erfolgt nur bei abgeschlossenen Kerzen und nachdem der Indikator vollständig gebildet ist.

## Risikomanagement
- Optionaler Stop-Loss und Take-Profit gemessen in Preisschritten (`StopLossPoints`, `TakeProfitPoints`).
- Beide werden über `StartProtection` implementiert und können durch Setzen des Abstands auf null deaktiviert werden.

## Parameter
| Parameter | Beschreibung | Standardwert |
|-----------|--------------|-------------|
| `CandleType` | Datentyp/Zeitrahmen für Berechnungen. | 4-Stunden-Kerzen |
| `Smoothing` | Glättungsmethode (EMA/SMA/SMMA/LWMA). | EMA |
| `MomentumLength` | Rückblickfenster für Hoch-/Tiefserkennung. | 20 |
| `FirstSmoothing` | Länge der Glättungsstufe 1. | 5 |
| `SecondSmoothing` | Länge der Glättungsstufe 2. | 8 |
| `ThirdSmoothing` | Länge der Glättungsstufe 3. | 3 |
| `Phase` | Zur Kompatibilität beibehalten (ignoriert). | 15 |
| `PriceType` | Angewendete Preiskonstante. | Close |
| `SignalBar` | Balkenversatz für Signalbewertung (>= 1). | 1 |
| `Mode` | Handelsmodus (Breakdown/Twist). | Twist |
| `AllowLongEntries` | Long-Einstiege aktivieren. | true |
| `AllowShortEntries` | Short-Einstiege aktivieren. | true |
| `AllowLongExits` | Schließen von Long-Trades aktivieren. | true |
| `AllowShortExits` | Schließen von Short-Trades aktivieren. | true |
| `TakeProfitPoints` | Take-Profit-Abstand in Schritten (0 deaktiviert). | 2000 |
| `StopLossPoints` | Stop-Loss-Abstand in Schritten (0 deaktiviert). | 1000 |

## Unterschiede zum MT5-Experten
- Erweiterte Glättungsalgorithmen aus SmoothAlgorithms.mqh sind nicht implementiert; wählen Sie aus EMA/SMA/SMMA/LWMA.
- Money Management (Lot-Sizing) ist vereinfacht: die Strategie nutzt die StockSharp `Volume`-Eigenschaft.
- Signalbewertung erfolgt nur bei abgeschlossenen Kerzen; es gibt keine Intrabar-Ausführung.

## Verwendungshinweise
- Stellen Sie sicher, dass `SignalBar` mindestens 1 bleibt; die Implementierung pflegt ausreichend Indikatorhistorie automatisch.
- Das Erhöhen der Glättungslängen erhöht die Formationszeit, da jede Stufe das vollständige Fenster benötigt.
- Für Umkehrhandel auf höheren Zeitrahmen sollten Sie Stop/Take-Abstände vergrößern oder eine Seite über Berechtigungen deaktivieren.
