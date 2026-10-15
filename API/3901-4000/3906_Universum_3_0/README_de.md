# Universum 3.0 Originalstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den ursprünglichen **Universum_3_0** MQL4-Expertenberater unter Verwendung des StockSharp-High-Level-API.
Es kombiniert ein einfaches DeMarker-Schwellenwerteingabemodell mit einer Martingal-ähnlichen Positionsgrößenregel, die sich anpasst
Losgröße nach verlorenen Trades.

## Handelslogik

- **Indikator**: klassischer DeMarker-Oszillator mit konfigurierbarer Periode.
- **Signalerzeugung**:
  - Eröffnen Sie eine Long-Position, wenn `DeMarker > 0.5` am Ende einer fertigen Kerze liegt.
  - Eröffnen Sie eine Short-Position, wenn `DeMarker < 0.5` am Ende einer fertigen Kerze liegt.
  - Es kann jeweils nur eine Position aktiv sein; Neue Signale werden ignoriert, solange ein Trade offen ist.
- **Exit-Management**:
  - Schützende Stop-Loss- und Take-Profit-Level werden durch absolute Preisversätze, gemessen in Punkten, festgelegt.
  - Durch diese Schutzniveaus werden Positionen automatisch geschlossen; Die Strategie ändert sich nicht sofort.
- **Geldmanagement**:
  - Nach einem profitablen Handel wird das Volumen auf das Basislos zurückgesetzt.
  - Nach einem Verlusthandel wird das Volumen mit `(TakeProfitPoints + StopLossPoints) / (TakeProfitPoints - SpreadPoints)` multipliziert.
  - Der Spread-Wert wird aus Live-Kursen der Stufe 1 entnommen und unter Verwendung der Symbolgenauigkeit in „Punkte“ umgewandelt.
  - Aufeinanderfolgende Verluste werden gezählt; Das Erreichen des Limits stoppt die Strategie, den ursprünglichen Verlustschutz zu emulieren.
  - Durch die Einstellung `FastOptimize = true` wird die adaptive Größenregel deaktiviert und immer das Basislos verwendet, was die Optimierung beschleunigt.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Zeitrahmen, der für DeMarker-Berechnungen verwendet wird. | Zeitrahmen von 1 Minute |
| `DemarkerPeriod` | Rückblickperiode des DeMarker-Oszillators. | `10` |
| `TakeProfitPoints` | Take-Profit-Distanz ausgedrückt in Punkten (intern in absoluten Preis umgerechnet). | `50` |
| `StopLossPoints` | Stop-Loss-Distanz ausgedrückt in Punkten. | `50` |
| `BaseVolume` | Anfängliches Handelsvolumen, das nach jedem profitablen Handel verwendet wird. | `1` |
| `LossesLimit` | Maximale Anzahl aufeinanderfolgender Verluste, bevor die Strategie stoppt. | `1,000,000` |
| `FastOptimize` | Wenn `true` die adaptive Größenanpassung für schnelle Optimierungsdurchläufe deaktiviert. | `true` |

## Implementierungshinweise

- Daten der Stufe 1 sind erforderlich, um den aktuellen Spread abzuschätzen und den ursprünglichen Lot-Multiplikator zu reproduzieren.
- Bei der Lautstärkenormalisierung werden die Mindestlautstärke, die Höchstlautstärke und die Schrittgröße des Instruments berücksichtigt.
- Stop-Loss- und Take-Profit-Offsets passen sich durch Anpassung der Punktgröße automatisch an 3/5-stellige Instrumente an.
- Die Diagrammvisualisierung stellt Kerzen, den DeMarker-Indikator und ausgeführte Trades zur einfacheren Validierung dar.

## Nutzungstipps

1. Stellen Sie zusätzlich zu den Kerzen Bid-/Ask-Daten der Stufe 1 bereit, um sicherzustellen, dass der Spread-basierte Multiplikator ordnungsgemäß funktioniert.
2. Verwenden Sie `FastOptimize = true` bei groben Parametersuchen und deaktivieren Sie es dann für präzise Backtests und Live-Handel.
3. Überwachen Sie den fortlaufenden Verlustzähler, wenn Sie mit aggressiven Multiplikatoren arbeiten, um eine Überschreitung der Broker-Limits zu vermeiden.
4. Passen Sie `TakeProfitPoints` und `StopLossPoints` an das Originalsymbol oder Ihr Risikoprofil an, bevor Sie live handeln.
