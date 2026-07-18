# Tunnel Method Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Tunnel Method Strategie ist ein StockSharp-Port des Expertenberaters "Tunnel Method", der ursprünglich für MetaTrader 5 veröffentlicht wurde. Sie verwendet drei verschobene einfache gleitende Durchschnitte (SMA), um gerichtete Ausbrüche zu erkennen. Der schnelle Durchschnitt muss einen Preis-"Tunnel", der durch die langsamen und mittleren Durchschnitte mit einem konfigurierbaren Einzug erstellt wird, durchdringen, um einen Trade zu bestätigen. Die Strategie enthält Positionsmanagement-Regeln identisch zur MQL-Version, einschließlich fester pip-basierter Stop-Loss- und Take-Profit-Niveaus, eines Trailing Stops, der Gewinne mit einem Schrittfilter sichert, und einer minimalen Wartezeit zwischen Einstiegsbewertungen.

## Strategielogik

- **Indikatoren**: drei einfache gleitende Durchschnitte auf demselben Instrument und Zeitrahmen.
  - *Erste SMA* (langsame Linie): langer Zeitraum ohne Verschiebung. Definiert die untere Grenze des bullischen Tunnels und die obere Grenze des bärischen Tunnels.
  - *Zweite SMA* (mittlere Linie): mittlerer Zeitraum mit positiver Verschiebung. Wird hauptsächlich für Short-Signale verwendet und schafft eine vorwärtsprojizierte Barriere.
  - *Dritte SMA* (schnelle Linie): kurzer Zeitraum mit der größten positiven Verschiebung. Ausbrüche dieser Linie durch den Tunnel aktivieren Aufträge.
- **Einzug**: Die gleitenden Durchschnitte müssen durch mindestens `IndentPips` (in Preiseinheiten umgerechnet) getrennt sein, um choppy Bedingungen zu vermeiden. Der schnelle Durchschnitt muss von unten über die langsame Durchschnitt plus die Hälfte des Einzugs kreuzen, um Longs zu eröffnen, und von oben unter die mittlere Durchschnitt minus die Hälfte des Einzugs, um Shorts zu eröffnen.
- **Einstiegsrhythmus**: Ein neues Signal wird nur ausgewertet, wenn seit der vorherigen Auswertung `PauseSeconds` vergangen sind. Dies spiegelt den Original-EA wider, der die OnTick-Verarbeitung drosselt, um Rauschen zu reduzieren.
- **Einzelpositionsmodus**: Die Strategie hält gleichzeitig nur eine Position. Ein neuer Auftrag wird ignoriert, wenn bereits eine andere Position offen ist.

## Risikomanagement

- **Stop Loss**: optionaler fester Abstand unterhalb (für Longs) oder oberhalb (für Shorts) des Einstiegspreises, gemessen in Pips über `StopLossPips`.
- **Take Profit**: optionales festes Ziel in Pips über `TakeProfitPips`.
- **Trailing Stop**: aktiviert, wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` positiv sind. Sobald sich der Preis zugunsten des Trades um `TrailingStopPips + TrailingStepPips` bewegt, wird der Stop auf `TrailingStopPips` hinter dem aktuellen Schlusskurs gezogen. Der Trailing Stop aktualisiert sich nur, wenn der Preis mindestens den Trailing-Schritt voranbewegt, um übermäßig häufige Anpassungen zu verhindern.
- **Positionsausstieg**: Die Strategie schließt Positionen zum Marktpreis, wenn Stops, Take Profits oder Trailing-Niveaus verletzt werden. Dies repliziert, wie der Original-EA reagieren würde, nachdem der Broker Schutzaufträge ausführt.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `TradeVolume` | 1 | Auftragsvolumen pro Trade. |
| `StopLossPips` | 50 | Stop-Loss-Abstand in Pips. `0` verwenden, um zu deaktivieren. |
| `TakeProfitPips` | 50 | Take-Profit-Abstand in Pips. `0` verwenden, um zu deaktivieren. |
| `TrailingStopPips` | 5 | Basis-Trailing-Abstand in Pips. Erfordert `TrailingStepPips > 0`. |
| `TrailingStepPips` | 5 | Minimaler inkrementeller Gewinn, bevor der Trailing Stop sich bewegen kann. |
| `FirstMaPeriod` | 160 | Zeitraum der langsamen SMA. |
| `FirstMaShift` | 0 | Vorwärtsverschiebung der langsamen SMA. |
| `SecondMaPeriod` | 80 | Zeitraum der mittleren SMA für Short-Signale. |
| `SecondMaShift` | 1 | Vorwärtsverschiebung der mittleren SMA. |
| `ThirdMaPeriod` | 20 | Zeitraum der schnellen SMA. |
| `ThirdMaShift` | 2 | Vorwärtsverschiebung der schnellen SMA. |
| `IndentPips` | 1 | Minimale Lücke zwischen Durchschnitten zur Validierung eines Ausbruchs. |
| `PauseSeconds` | 45 | Verzögerung zwischen aufeinanderfolgenden Einstiegsprüfungen. |
| `CandleType` | 5-Minuten-Zeitrahmen | Kerzenserie für Indikatorberechnungen. |

Alle pip-basierten Parameter werden automatisch in Preiseinheiten umgerechnet unter Verwendung des `PriceStep` des Instruments und der Dezimalpräzision, mit spezieller Behandlung für 3- und 5-stellige FX-Symbole wie in der MetaTrader-Version.

## Praktische Hinweise

1. **Instrumentenkonfiguration**: Stellen Sie sicher, dass das der Strategie zugewiesene `Security` korrekte `PriceStep`- und `Decimals`-Werte hat. Die konvertierten Pip-Abstände werden sonst ungenau.
2. **Zeitrahmenausrichtung**: Der Standard-`CandleType` verwendet 5-Minuten-Kerzen, aber Sie können ihn mit dem in MetaTrader verwendeten Zeitrahmen (z. B. M1) ausrichten, indem Sie den Parameter ändern.
3. **Volumenbehandlung**: `TradeVolume` definiert die Gesamtgröße pro Einstieg. Die Strategie schließt Positionen mit symmetrischen Marktaufträgen, sodass die Positionsgröße konsistent bleibt.
4. **Trailing-Anforderungen**: Der Konstruktor setzt die Regel des Original-EA durch: Wenn `TrailingStopPips` positiv ist, während `TrailingStepPips` null ist, wirft die Strategie einen Initialisierungsfehler, um inkonsistente Einstellungen zu verhindern.
5. **Optimierung**: Das Parameterdesign folgt StockSharp-Konventionen. Jeder Parameter kann optimiert oder an UI-Steuerelemente in Designer gebunden werden, was die Feinabstimmung von Perioden, Einzug oder Trailing-Werten erleichtert.

## Dateien

- `CS/TunnelMethodStrategy.cs` – Kern-Strategieimplementierung.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_ru.md` – Russische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.

Die Python-Übersetzung wird absichtlich ausgelassen, entsprechend der Anforderung, in dieser Phase nur die C#-Version zu liefern.
