# CDC PL MFI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **CDC PL MFI Strategy** reproduziert den MetaTrader-Expertenberater `Expert_ADC_PL_MFI` (MQL/299) in StockSharp. Es sucht nach den Zwei-Kerzen-Umkehrmustern **Dark Cloud Cover** und **Piercing Line** und validiert jedes Signal mit dem **Money Flow Index (MFI)**-Oszillator. Die Strategie verwendet die gleichen Indikatorperioden und Niveauschwellen wie der ursprüngliche Experte, fügt optionalen Stop-Loss- und Take-Profit-Schutz in Pip-Einheiten hinzu und schließt Positionen, wenn der MFI konfigurierbare Umkehrniveaus überschreitet.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzentyp (standardmäßig einstündige Kerzen) und berechnen Sie einen Geldflussindex mit dem angegebenen Zeitraum. Behalten Sie einfache gleitende Durchschnitte der Kerzenkörpergröße und Schlusskurse bei, um die ursprünglichen Trend- und Volatilitätsfilter nachzubilden.
2. Wenn sich ein zinsbullisches **Piercing Line**-Muster bildet (Lücke unter dem vorherigen Tief, zinsbullischer Schlusskurs über dem Mittelpunkt der vorherigen bärischen Kerze, beide Kerzen größer als der durchschnittliche Körper und der vorherige Schlusskurs unter dem Trenddurchschnitt) *und* der aktuelle MFI-Wert unter dem **LongEntryLevel** (Standard `40`) liegt, gehen Sie eine Long-Position ein oder wechseln Sie zu einer Long-Position.
3. Wenn sich ein bärisches **Dark Cloud Cover**-Muster bildet (Lücke über dem vorherigen Hoch, bärischer Schlusskurs unter dem Mittelpunkt der vorherigen bullischen Kerze, beide Kerzen größer als der durchschnittliche Körper und der vorherige Schlusskurs über dem Trenddurchschnitt) *und* der aktuelle MFI-Wert über dem **ShortEntryLevel** (Standard `60`) liegt, gehen Sie eine Short-Position ein oder wechseln Sie zu einer Short-Position.
4. Überwachen Sie das MFI, um Positionen proaktiv zu schließen:
   - Schließen Sie Short-Positionen, wenn der MFI **ExitLowerLevel** (`30`) oder **ExitUpperLevel** (`70`) überschreitet.
   - Schließen Sie Long-Positionen, wenn der MFI unter **ExitUpperLevel** (`70`) oder **ExitLowerLevel** (`30`) fällt.
5. Schutzanordnungen sind optional. Wenn **TakeProfitPips** oder **StopLossPips** größer als Null sind, ruft die Strategie `StartProtection` mit den entsprechenden Preisoffsets auf (Pip-Abstand multipliziert mit dem Wertpapierpreisschritt).

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Kerzendatentyp, der zur Mustererkennung verwendet wird. | `1 hour` Zeitrahmen |
| `MfiPeriod` | Länge des Money-Flow-Index-Oszillators. | `49` |
| `BodyAveragePeriod` | Zeitraum des gleitenden Durchschnitts des Kerzenkörpers, der zur Qualifizierung „langer“ Kerzen verwendet wird. | `11` |
| `LongEntryLevel` | MFI-Schwellenwert, der bullische Piercing-Line-Setups bestätigt. | `40` |
| `ShortEntryLevel` | MFI-Schwellenwert, der eine rückläufige Entwicklung der dunklen Wolkendecke bestätigt. | `60` |
| `ExitLowerLevel` | Niedrigeres MFI-Niveau, das die Deckung von Short-Positionen auslöst. | `30` |
| `ExitUpperLevel` | Oberes MFI-Niveau, das die Schließung von Long-Positionen auslöst. | `70` |
| `StopLossPips` | Optionaler Stop-Loss-Abstand in Pips (0 deaktiviert den Schutz). | `50` |
| `TakeProfitPips` | Optionale Take-Profit-Distanz in Pips (0 deaktiviert den Schutz). | `50` |

## Notizen
- Das Volumen beträgt standardmäßig `1` Lot. When the strategy flips direction it sends a single market order sized to close the existing position and open the new one, matching the MQL behavior.
- Die Mustererkennung spiegelt die MetaTrader-Logik wider: Es werden nur abgeschlossene Kerzen ausgewertet, Lücken müssen über das vorherige Hoch/Tief hinausgehen und ein einfacher gleitender Durchschnitt erzwingt die vorherrschende Trendbedingung.
- Die Werte des Geldflussindex stammen direkt vom gebundenen Indikator. Es ist keine manuelle Pufferung des Indikatorverlaufs erforderlich. Die Strategie speichert nur die aktuellsten Werte, um Schwellenwertüberschreitungen zu erkennen.
- Es wird kein Python-Port bereitgestellt. In diesem Verzeichnis ist nur die C#-Implementierung enthalten.
