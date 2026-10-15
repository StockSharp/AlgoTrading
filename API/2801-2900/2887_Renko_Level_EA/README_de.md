# Renko Level EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertiert vom MetaTrader-Expertenberater **Renko Level EA.mq5**.
- Emuliert den ursprünglichen Indikator durch die Beibehaltung eines oberen und unteren Renko-Niveaus, das aus dem `BrickSize`-Parameter abgeleitet wird.
- Wertet abgeschlossene Kerzen aus, die von `CandleType` geliefert werden (Standard: 1-Minuten-Zeitrahmen) und reagiert, wenn das Renko-Raster verschoben wird.
- Verwendet keine festen Stops oder Ziele; jeder Ausstieg erfolgt durch ein entgegengesetztes Signal.

## Handelslogik
1. Bei der ersten abgeschlossenen Kerze wird der Schlusskurs auf das Renko-Raster gerundet, um obere und untere Niveaus zu initialisieren.
2. Für jede nachfolgende Kerze:
   - Wenn der Schlusskurs zwischen den aktuellen Grenzen bleibt, bleibt das Raster unverändert.
   - Ein Schlusskurs über dem oberen Niveau hebt den Renko-Block nach oben auf den nächsten Rasterwert an.
   - Ein Schlusskurs unter dem unteren Niveau drückt den Block nach unten.
3. Eine Änderung des oberen Renko-Niveaus wird als direktionaler Ausbruch interpretiert.
   - Steigendes oberes Niveau → bullisches Signal (es sei denn, `ReverseSignals` ist aktiviert).
   - Fallendes oberes Niveau → bärisches Signal.
4. Signale können optional umgekehrt (`ReverseSignals`) oder pyramidisiert (`AllowIncrease`) werden, um das Verhalten des ursprünglichen EA zu entsprechen.

## Order-Management
- Vor dem Long-Einstieg wird jede Short-Position geschlossen; das Gegenteil passiert vor dem Short-Einstieg.
- Wenn `AllowIncrease = false`, öffnet die Strategie einen neuen Trade nur, wenn keine Position in dieser Richtung existiert.
- Wenn `AllowIncrease = true`, sind zusätzliche Orders der Größe `OrderVolume` erlaubt, auch wenn eine Position bereits offen ist.
- Es gibt keinen dedizierten Stop-Loss oder Take-Profit; Positions-Reversals dienen als Ausstiegsmechanismus.
- `StartProtection()` wird einmal aufgerufen, um die Risikoschutzmechanismen mit dem Basis-Framework abzustimmen.

## Parameter
| Name | Beschreibung | Standard | Optimierbar |
| --- | --- | --- | --- |
| `BrickSize` | Renko-Blockgröße gemessen als Vielfaches von `Security.PriceStep`. Definiert, wie weit sich der Preis bewegen muss, um das Raster zu verschieben. | `30` | Ja (10 → 100 Schritt 10) |
| `OrderVolume` | Volumen, das mit jeder Marktorder übermittelt wird. | `1` | Nein |
| `ReverseSignals` | Invertiert bullische und bärische Aktionen. Spiegelt die *Reverse*-Eingabe des EA wider. | `false` | Nein |
| `AllowIncrease` | Erlaubt das Hinzufügen zu einer bestehenden Position, anstatt auf eine flache Position zu warten. Spiegelt den *Increase*-Flag des EA wider. | `false` | Nein |
| `CandleType` | Kerzenquelle für die Berechnungen. Standardmäßig 1-Minuten-Zeitrahmen-Kerzen, aber jede unterstützte Serie kann angegeben werden. | `TimeFrameCandleMessage(1m)` | Nein |

## Praktische Hinweise
- `BrickSize` passt sich automatisch dem gehandelten Instrument an, da es den börsendefinierten `PriceStep` multipliziert.
- Die Entscheidung basiert ausschließlich auf Schlusskursen; Intrabar-Bewegungen sind nur wichtig, wenn sie den endgültigen Schluss bilden.
- Die Kombination von `ReverseSignals` und `AllowIncrease` ermöglicht das Testen sowohl kontra-trend als auch pyramidisierender Varianten des EA.
- Funktioniert auf jedem Markt, wo Renko-ähnliche Ausbruchslogik relevant ist, einschließlich Forex, Futures und Krypto-Instrumente.

## Klassifizierung
- **Regime**: Trendfolge (Renko-Ausbruch).
- **Richtung**: Long/Short.
- **Komplexität**: Moderat (benutzerdefiniertes Niveau-Tracking, minimale Abstimmung).
- **Stops**: Keine; Ausstiege bei Umkehrsignalen.
- **Zeitrahmen**: Konfigurierbar über `CandleType`.
- **Indikatoren**: Benutzerdefinierte Renko-Niveau-Projektion.
