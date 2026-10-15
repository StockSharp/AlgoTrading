# Mittlere Reverse-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Mean Reverse Strategy repliziert den Expertenberater „MeanReversionTrendEA“. Es kombiniert ein Crossover-Trendmodul mit gleitendem Durchschnitt mit einem Mean-Reversion-Overlay, das durch Volatilitätsbänder des Average True Range (ATR) gesteuert wird. Die Idee besteht darin, eine Position zu eröffnen, wenn der Preis entweder eine zinsbullische oder bärische Trendwende bestätigt oder sich um eine volatilitätsbereinigte Distanz zu weit vom langsameren gleitenden Durchschnitt entfernt.

## Handelslogik
- **Trendkomponente**: Ein Long-Setup erscheint, wenn der schnelle einfache gleitende Durchschnitt (SMA) den langsamen SMA überschreitet. Ein kurzer Setup wird ausgelöst, wenn der schnelle SMA den langsamen SMA unterschreitet.
- **Mean-Reversion-Komponente**: Ein Long-Setup wird immer dann aktiviert, wenn der Schlusskurs um mehr als `ATR × Multiplier` unter den langsamen SMA fällt. Ein Short-Setup tritt auf, wenn der Preis um mehr als die gleiche Distanz über den langsamen SMA steigt.
- **Signalkombination**: Wenn entweder das Trendmodul oder das Mean-Reversion-Modul eine Long-Position (Short-Position) signalisiert, während keine Position offen ist, geht die Strategie eine Long-Position (Short-Position) mit dem konfigurierten Volumen ein.

## Handelsmanagement
- **Stop-Loss**: Unmittelbar nach dem Einstieg setzt die Strategie ein Preisniveau bei `entry − StopLossPoints × Step` für Long-Positionen oder `entry + StopLossPoints × Step` für Short-Positionen. Wenn die Extremwerte der Kerze dieses Niveau erreichen, wird die Position geschlossen.
- **Take-Profit**: Das Gewinnziel liegt bei `entry + TakeProfitPoints × Step` für Long-Trades oder bei `entry − TakeProfitPoints × Step` für Short-Trades. Eine Berührung des jeweiligen Kerzenhochs oder -tiefs schließt die Position.
- **Einzelpositionsbeschränkung**: Der Algorithmus behält höchstens eine offene Position. Neue Signale werden ignoriert, bis der aktuelle Trade geschlossen wird.
- **Sicherheitsmodul**: Der integrierte `StartProtection()`-Aufruf spiegelt die Safety-Trade-Validierungsebene des ursprünglichen Expert Advisors wider und schützt vor unerwarteten Positionszuständen.

## Indikatoren
- **Einfacher gleitender Durchschnitt (SMA)** mit Zeitraum `FastMaPeriod`.
- **Einfacher gleitender Durchschnitt (SMA)** mit Zeitraum `SlowMaPeriod`.
- **Durchschnittliche wahre Reichweite (ATR)** mit Zeitraum `AtrPeriod`.

Alle Indikatoren werden aus demselben Kerzenabonnement aktualisiert, das durch `CandleType` definiert ist.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `FastMaPeriod` | Rückblick auf den schnellen SMA, der sowohl in der Trenderkennung als auch in den Mean-Reversion-Bändern verwendet wird. | 20 |
| `SlowMaPeriod` | Rückblick auf das langsame SMA, das den Gleichgewichtsmittelwert darstellt. | 50 |
| `AtrPeriod` | Anzahl der Kerzen für die Volatilitätsberechnung von ATR. | 14 |
| `AtrMultiplier` | Multiplikator für Entfernungsprüfungen auf ATR angewendet. | 2,0 |
| `StopLossPoints` | Stop-Loss-Distanz, gemessen in `Security.Step` Einheiten. | 500 |
| `TakeProfitPoints` | Take-Profit-Distanz, gemessen in `Security.Step` Einheiten. | 1000 |
| `TradeVolume` | Mit jeder Marktorder gesendetes Volumen. | 1 |
| `CandleType` | Kerzendatentyp, der die Indikatoren speist. | 1-stündiger Zeitrahmen |

## Notizen
- Die Standardkerzengröße beträgt eine Stunde, um die Logik des „aktuellen Zeitrahmens“ der Version MetaTrader widerzuspiegeln. Passen Sie es an den ursprünglichen Diagrammzeitraum an.
- ATR-basierte Umschläge verwenden den Schlusskurs der Kerze als Referenzpreis und spiegeln den ursprünglichen Mittelpunkt zwischen Geld- und Briefkurs wider.
- Verwenden Sie die an die Parameter angehängten Optimierungsflags, um das System für verschiedene Märkte zu kalibrieren.
