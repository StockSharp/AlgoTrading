# TradingLabs beste MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Expertenberater „TradingLab_Best_MACD_Strategy“ unter Verwendung des übergeordneten API von StockSharp. Es kombiniert eine gleitende Durchschnittsstruktur, MACD-Crossovers und dynamische Unterstützungs-/Widerstandsprüfungen, um direktionale Trades zu eröffnen, die sich an der Dynamik und den jüngsten Preisreaktionen orientieren.

## Kernlogik

- **Kerzenquelle** – Verwendet den konfigurierbaren Parameter `CandleType`, um fertige Kerzen zu abonnieren. Nur abgeschlossene Kerzen generieren Handelsentscheidungen.
- **Trendfilter** – Ein einfacher gleitender Durchschnitt über 200 Perioden definiert den vorherrschenden Trend. Bei Long-Trades muss der Schlusskurs über dem Durchschnitt bleiben, bei Short-Trades muss der Schlusskurs unter diesem Durchschnitt bleiben.
- **Unterstützungs- und Widerstandsbox** – Ein 20-Perioden-Höchst-/Tiefstfenster emuliert den benutzerdefinierten „Box“-Indikator. Durch Berühren des vorherigen Widerstands- oder Unterstützungsniveaus werden kurze oder lange Setups für eine begrenzte Anzahl von Kerzen aktiviert, die von `SignalValidity` gesteuert werden.
- **MACD Crossovers** – Ein Standard-MACD (standardmäßig 12, 26, 9) muss seine Signallinie auf der vorherigen Kerze kreuzen und auf der erforderlichen Seite der Nulllinie bleiben. Jeder gültige Crossover hält sein Signal für `SignalValidity` Kerzen am Leben und spiegelt die Countdown-Logik der Quelle EA wider.
- **Einstiegszeitpunkt** – Eine Position wird eröffnet, wenn sowohl der MACD als auch die entsprechende Unterstützungs-/Widerstandsberührung noch gültig sind und mindestens einer von ihnen bei der aktuellen Kerze ausgelöst wurde.
- **Exit-Logik** – Beim Einstieg werden dynamische Stop-Loss- und Take-Profit-Ziele im Verhältnis zur gleitenden Durchschnittsentfernung berechnet. Die Take-Profit-Distanz beträgt das `RiskRewardMultiplier`-fache der angepassten Distanz, die für den Stopp verwendet wird. Schutzausgänge überwachen nachfolgende Kerzen und rufen `ClosePosition()` auf, sobald der Preis die gespeicherten Niveaus überschreitet.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Bei jeder Marktorder wird ein festes Volumen gesendet. |
| `SignalValidity` | Anzahl der Kerzen, die MACD und Unterstützungs-/Widerstandsauslöser aktiv halten. |
| `MaLength` | Zeitraum des einfachen Trendfilters für gleitende Durchschnitte. |
| `BoxPeriod` | Lookback-Länge für das höchste/tiefste Feld, das den aktuellen Widerstand und die Unterstützung verfolgt. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD schnelle, langsame und Signalperioden. |
| `StopDistancePoints` | Abstand vom gleitenden Durchschnitt zum Stop-Loss, ausgedrückt in Punkten im MetaTrader-Stil (multipliziert mit dem Symbolpreisschritt). |
| `RiskRewardMultiplier` | Der Multiplikator wird auf den angepassten MA-Abstand angewendet, um das Take-Profit-Ziel zu ermitteln. |
| `CandleType` | Datentyp, der die zu abonnierende Kerzenserie beschreibt (Standard: 1-Stunden-Zeitrahmen). |

## Notizen

- Die Unterstützungs- und Widerstandserkennung folgt der ursprünglichen Idee und beobachtet, ob die vorherige Kerze die höchsten/tiefsten 20-Perioden-Niveaus durchbricht. Bei jeder Berührung werden die Gültigkeitszähler neu gestartet.
- Stopps und Ziele werden für jeden neuen Eintrag neu berechnet und mit dem Hoch/Tief jeder fertigen Kerze verglichen, um die Intrabar-Überwachung von MetaTrader auf deterministische Weise nachzuahmen.
- Das Schutzmanagement basiert auf dem Instrument `PriceStep`. Wenn ein Instrument einen Nullschritt meldet, wird ein Fallback von 0,0001 verwendet.
