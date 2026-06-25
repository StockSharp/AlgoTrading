# XDidi Index Cloud Duplex Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die XDidi Index Cloud Duplex Strategie repliziert die duale Long/Short-Signallogik des ursprünglichen MQL5-Experten *Exp_XDidi_Index_Cloud_Duplex*. Zwei unabhängige XDidi-Index-Konfigurationen werden auf konfigurierbaren Zeitrahmen ausgewertet. Jede Konfiguration berechnet ein Verhältnis zwischen schnellen/mittleren und langsamen/mittleren gleitenden Durchschnitten. Kreuzungen zwischen diesen Verhältnissen lösen Markteinstiege aus, während anhaltende Divergenzen Ausstiege auslösen.

## Handelslogik
1. **Indikatorberechnung**
   - Drei gleitende Durchschnitte werden für jeden Block (schnell, mittel, langsam) auf einer ausgewählten Preisquelle berechnet.
   - Die XDidi-Verhältnisse werden als `fast / medium` und `slow / medium` abgeleitet. Optionale Inversion entspricht der ursprünglichen `Revers`-Option.
2. **Signalerzeugung**
   - Long-Block: Wenn der vorherige Balken `fast > slow` hatte und der Signalbalken mit `fast <= slow` schließt, wird ein Long-Einstieg angefordert. Wenn der vorherige Balken `fast < slow` hatte, wird ein Long-Ausstieg angefordert.
   - Short-Block: Wenn der vorherige Balken `fast < slow` hatte und der Signalbalken mit `fast >= slow` schließt, wird ein Short-Einstieg angefordert. Wenn der vorherige Balken `fast > slow` hatte, wird ein Short-Ausstieg angefordert.
   - Signalbalken-Offsets reproduzieren die ursprünglichen `SignalBar`-Eingaben.
3. **Orderverwaltung**
   - Einstiege werden mit dem Strategie-Volumen ausgeführt. Entgegengesetzte Positionen werden vor der Umkehr geschlossen.
   - Optionale Stop-Loss- und Take-Profit-Level werden über `StartProtection` mit Preisschritt-Distanzen angewendet.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `LongCandleType`, `ShortCandleType` | Kerzen-Zeitrahmen für jeden Block. |
| `LongFastMethod` / `Medium` / `Slow` & `ShortFastMethod` / `Medium` / `Slow` | Glättungsmethoden des gleitenden Durchschnitts für schnelle, mittlere und langsame Kurven. Nicht unterstützte Legacy-Glätter fallen auf exponentielles Averaging zurück. |
| `LongFastLength`, `LongMediumLength`, `LongSlowLength` | Perioden für die gleitenden Durchschnitte des Long-Blocks. |
| `ShortFastLength`, `ShortMediumLength`, `ShortSlowLength` | Perioden für die gleitenden Durchschnitte des Short-Blocks. |
| `LongAppliedPrice`, `ShortAppliedPrice` | Preisquelle für jeden Block (Schluss, Eröffnung, typisch, Demark, usw.). |
| `EnableLongEntries`, `EnableShortEntries` | Neue Long-/Short-Positionen umschalten. |
| `EnableLongExits`, `EnableShortExits` | Automatische Ausstiege umschalten. |
| `LongSignalBar`, `ShortSignalBar` | Historischer Versatz (Balken zurück), der für Kreuzungen ausgewertet wird. |
| `LongReverse`, `ShortReverse` | Verhältnisse invertieren (spiegelt `Revers`-Flag in MQL wider). |
| `StopLossPoints`, `TakeProfitPoints` | Schutz-Distanzen in Preisschritten (auf null setzen zum Deaktivieren). |
| `Volume` (Basis-Strategie-Eigenschaft) | Definiert die Standard-Handelsgröße. |

## Implementierungshinweise
- Gleitende Durchschnitte werden aus der StockSharp-Indikator-Bibliothek entnommen. Fortgeschrittene Glätter (`JJMA`, `JurX`, `ParMA`, `VIDYA`) verwenden standardmäßig exponentielles Glätten, da direkte Äquivalente nicht verfügbar sind.
- Indikatorwerte werden nur auf fertigen Kerzen verarbeitet, was dem ursprünglichen `IsNewBar`-Verhalten entspricht.
- Signalwarteschlangen pflegen nur die erforderliche Anzahl historischer Verhältniswerte und vermeiden schwere Sammlungen.
- Schutz-Stops sind optional; wenn beide Distanzen null sind, ruft die Strategie trotzdem `StartProtection()` auf, um dem Framework-Lebenszyklus zu entsprechen.

## Verwendungstipps
- Richten Sie Kerzentypen an dem in Ihrem Connector verfügbaren Daten-Abonnement aus.
- Optimieren Sie Längen der gleitenden Durchschnitte und angewendete Preise für das gehandelte Instrument.
- Bei asymmetrischen Zeitrahmen (Long/Short) werden beide Abonnements auf separaten Diagrammbereichen zur besseren Übersicht visualisiert.

## Einschränkungen im Vergleich zur MQL5-Version
- Geldverwaltungsmodi (`MM`, `MarginMode`) werden nicht repliziert; die Handelsgröße folgt der StockSharp-`Volume`-Eigenschaft.
- Einige exotische Glättungsalgorithmen aus `SmoothAlgorithms.mqh` werden mit exponentiellen gleitenden Durchschnitten approximiert.
- Stop-/Limit-Orders werden in generische Schutzlevel umgewandelt statt in individuelle Order-Parameter.
