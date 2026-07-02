# Grid EA Pro-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Grid EA Pro-Strategie** reproduziert das Kernverhalten des ursprünglichen MetaTrader 4-Expertenberaters. Die Strategie kombiniert gitterbasierte Skalierung mit RSI oder zeitgesteuerten Breakout-Einträgen und virtuellen Risikomanagementfunktionen wie Break-Even und Trailing Stops. Es ist für Netting-Portfolios konzipiert, das heißt, es arbeitet immer mit einer einzigen Nettoposition und räumt die Gegenrichtung automatisch auf, wenn ein neuer Trade eröffnet wird.

## Handelslogik
- **Eingabemodus** – Wählen Sie zwischen RSI Schwellenwerten, zeitgesteuerten Ausbrüchen oder vollständig manuellem Betrieb. Im manuellen Modus verwaltet die Strategie nur bestehende Positionen und Rasterskalierung.
- **Richtungsfilter** – Beschränken Sie den Handel auf Long-, Short- oder beide Richtungen.
- **Rasterskalierung** – Nach dem ersten Einstieg kann die Strategie Positionen hinzufügen, wenn der Preis um eine konfigurierbare Anzahl von Punkten zurückgeht. Sowohl der Schritt als auch das Auftragsvolumen können geometrisch wachsen.
- **Risikokontrollen** – virtuelle Stop-Loss-, Take-Profit-, Break-Even-, Trailing-Stop- und Sitzungsfilter spiegeln das ursprüngliche Verhalten des Expert Advisors wider.
- **Überlappende Ausgänge** – Parameter werden der Vollständigkeit halber bereitgestellt, aber aufgrund des Nettopositionsmodells können nicht beide Richtungen gleichzeitig gehalten werden. Die Überlappungslogik bleibt daher inaktiv und die Ebenen werden aus Gründen der Vorwärtskompatibilität dokumentiert.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Mode` | Zulässige Handelsrichtung (Kauf, Verkauf, Beides). |
| `EntryMode` | Signalquelle (RSI, FixedPoints, Manuell). |
| `RsiPeriod`, `RsiUpper`, `RsiLower` | RSI-Konfiguration, die im RSI-Modus verwendet wird. |
| `CandleType` | Kerzenabonnement für Signale und Risikomanagement. |
| `Distance`, `TimerSeconds` | Ausbruchsdistanz und Aktualisierungsintervall für Festkomma-Einträge. |
| `InitialVolume`, `FromBalance`, `Risk %` | Geldverwaltungsblock. Wenn `Risk %` > 0, wird die Positionsgröße aus dem Kontokapital und der Stop-Loss-Distanz abgeleitet, andernfalls wird ein saldobasierter oder fester Lot verwendet. |
| `LotMultiplier`, `MaxLot` | Multiplikator und Obergrenze für Rasterergänzungen. |
| `Step`, `StepMultiplier`, `MaxStep` | Rasterabstandseinstellungen in Punkten. |
| `OverlapOrders`, `OverlapPips` | Reserviert für abgesicherte Überlappungslogik (in dieser Implementierung deaktiviert). |
| `Stop Loss`, `Take Profit` | Anfängliche Schutzstufen in Punkten (`-1` deaktiviert). |
| `Break Even Stop`, `Break Even Step` | Bewegen Sie den Stopp auf die Gewinnschwelle, nachdem sich der Preis um den definierten Schritt bewegt hat. |
| `Trailing Stop`, `Trailing Step` | Trailing-Stop-Konfiguration. |
| `Start Time`, `End Time` | Handelssitzungsfenster in lokaler Plattformzeit (HH:mm). |

## Diagramme
Wenn der Diagrammbereich verfügbar ist, zeichnet die Strategie Preiskerzen, die RSI-Linie und alle eigenen Trades entsprechend dem Layout des Quell-Expert Advisors auf.

## Notizen
- Die Strategie löscht ausstehende Ausbruchsniveaus automatisch, sobald sie erreicht sind oder wenn die Richtung deaktiviert wird.
- Da StockSharp saldierte Positionen verwendet, kann jeweils nur eine Seite des Marktes offen sein. Durch die Eröffnung einer Long-Position werden bestehende Short-Positionen aufgelöst und umgekehrt.
- Stellen Sie sicher, dass die Instrumenteneigenschaften (`PriceStep`, `StepPrice`) so konfiguriert sind, dass punktbasierte Parameter mit den ursprünglichen MT4-Einstellungen übereinstimmen.
