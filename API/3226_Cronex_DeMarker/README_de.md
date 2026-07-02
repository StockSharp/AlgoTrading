# Strategie Cronex DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Cronex-DeMarker-Strategie** reproduziert den klassischen Cronex-Experten-Advisor, der den DeMarker-Oszillator mit einem doppelten Glättungs-Stack kombiniert. Zunächst werden die DeMarker-Werte durch einen schnellen einfachen gleitenden Durchschnitt geglättet, dann wird das Ergebnis durch einen langsameren Durchschnitt noch einmal geglättet. Die Distanz und relative Anordnung dieser zwei Linien liefern reversal-artige Einstiegssignale.

Die ursprüngliche MQL5-Implementierung erlaubt Handelsrichtungs-Umschaltungen und funktioniert auf höheren Zeitrahmen. Dieser StockSharp-Port behält dieselbe Philosophie: Er reagiert, wenn die schnelle Linie durch die langsame kreuzt, und schließt sofort jede entgegengesetzte Position. Da das System konträr ist, öffnet ein Kreuzen unter der langsamen Linie eine Long-Position, während ein Kreuzen darüber eine Short öffnet. Beide Richtungen können über Parameter unabhängig deaktiviert werden, was die Strategie für verschiedene Portfolio-Allokationen flexibel macht.

## Funktionsweise

1. Kerzen für den ausgewählten Zeitrahmen anfordern (standardmäßig 4H).
2. Den DeMarker-Oszillator berechnen und mit einem schnellen SMA glätten (Standard 14 Balken).
3. Einen zweiten SMA (Standard 25 Balken) auf die schnelle Linie anwenden, um die Signallinie zu erhalten.
4. Wenn die schnelle Linie bei der vorherigen Kerze über der langsamen lag und jetzt darunter fällt, kauft die Strategie (konträre Umkehr). Jede bestehende Short-Position wird geglättet.
5. Wenn die schnelle Linie bei der vorherigen Kerze unter der langsamen lag und jetzt darüber steigt, verkauft die Strategie und schließt jede offene Long-Position.
6. Die Positionsgröße wird durch die `Volume`-Eigenschaft definiert; Umkehrungen verwenden die absolute Position, um sofort umzukehren.

Diese Logik ermöglicht dem Experten, kurzfristige Erschöpfungsbewegungen nach starken Momentum-Schüben zu erfassen, was ihn zu einem Mean-Reversion-Werkzeug macht, das Range- oder Chop-Märkte bevorzugt.

## Standardparameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `DeMarkerPeriod` | 25 | Anzahl der Balken, die vom DeMarker-Oszillator verwendet werden. |
| `FastPeriod` | 14 | Länge des ersten Glättungs-SMA, der auf DeMarker-Werte angewendet wird. |
| `SlowPeriod` | 25 | Länge des Signal-SMA, der auf die schnelle Linie angewendet wird. |
| `CandleType` | 4 Stunden | Kerzenserie für Indikatorberechnungen. |
| `EnableLongEntry` | true | Konträre Long-Einstiege erlauben, wenn die schnelle Linie unter die langsame kreuzt. |
| `EnableShortEntry` | true | Short-Einstiege erlauben, wenn die schnelle Linie über die langsame kreuzt. |
| `EnableLongExit` | true | Bestehende Long-Positionen schließen, wenn bärische Bedingungen auftreten. |
| `EnableShortExit` | true | Bestehende Short-Positionen schließen, wenn bullische Bedingungen auftreten. |

## Filter & Tags

- **Kategorie**: Mean Reversion, Oszillator-basiert
- **Richtung**: Long & Short (konfigurierbar)
- **Indikatoren**: DeMarker, Einfacher gleitender Durchschnitt (doppelte Glättung)
- **Stops**: Keine (vollständig signalgesteuert)
- **Zeitrahmen**: Swing Trading (H4 standardmäßig, anpassbar)
- **Komplexität**: Mittel aufgrund der sequentiellen Indikatorkette
- **Risikoprofil**: Mittel — konträre Einstiege können auf anhaltende Trends treffen
- **Automatisierung**: Vollständig automatisiert über die StockSharp High-Level-API

## Verwendungshinweise

- Die Strategie verarbeitet nur abgeschlossene Kerzen, um Neuzeichnungsprobleme zu vermeiden.
- Umkehrorders verwenden die absolute Positionsgröße wieder und garantieren sofortiges Glätten vor dem Einsteigen in die neue Richtung.
- Chartausgabe zeichnet die zwei geglätteten Linien und Trade-Marker, was bei der diskretionären Validierung hilft.
- Für Portfolios, die nur eine Richtung erlauben, unerwünschte Einstiege und Ausstiege über die bereitgestellten Parameter deaktivieren.
- Externe Risikokontrollen (Stop-Loss, Trailing-Ausstieg) in Betracht ziehen, wenn auf volatilen Assets eingesetzt.
