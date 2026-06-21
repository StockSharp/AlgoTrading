# CorrectedAverage-Ausbruchstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche relativ zu einem **CorrectedAverage**-gleitenden Durchschnitt. Der Indikator glättet den Preis mithilfe eines gleitenden Durchschnitts und passt den Glättungsfaktor basierend auf der Standardabweichung der Preisveränderungen an.

Wenn der Preis den korrigierten Durchschnitt um eine bestimmte Anzahl von Punkten überschreitet und dann zum Ausbruchsniveau zurückkehrt, öffnet die Strategie eine Long-Position. Die umgekehrte Logik gilt für Short-Trades. Stop-Loss und Take-Profit werden in absoluten Preispunkten angewendet.

## Parameter

- `Candle Type` – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- `Length` – Periode für den gleitenden Durchschnitt und die Standardabweichung.
- `MA Type` – Art des gleitenden Durchschnitts (SMA, EMA, SMMA, LWMA).
- `Level Points` – Ausbruchsabstand vom korrigierten Durchschnitt in Preisschritten.
- `Stop Loss Points` – Stop-Loss-Abstand vom Einstiegspreis in Preisschritten.
- `Take Profit Points` – Take-Profit-Abstand vom Einstiegspreis in Preisschritten.
- `Enable Long` – Long-Positionen öffnen erlauben.
- `Enable Short` – Short-Positionen öffnen erlauben.

## Handelslogik

1. Gleitenden Durchschnitt und Standardabweichung berechnen.
2. Den korrigierten Durchschnitt mithilfe vorheriger Werte und des Varianzverhältnisses aufbauen, um plötzliche Sprünge zu glätten.
3. Ausbrüche erkennen, wenn der vorherige Balken über oder unter dem korrigierten Durchschnitt plus/minus dem konfigurierten Niveau schließt.
4. Nach einem Ausbruch auf die Rückkehr der nächsten Kerze zum Ausbruchsniveau warten und eine Position in Ausbruchsrichtung eröffnen.
5. Entgegengesetzte Positionen schließen, wenn ein neues Ausbruchssignal erscheint.
6. Stop-Loss- und Take-Profit-Schutz anwenden.

## Hinweise

Diese Strategie ist eine Konvertierung des MQL-Skripts *Exp_CorrectedAverage.mq5*. Sie ist für Bildungszwecke gedacht und erfordert weitere Tests vor dem Einsatz im Live-Handel.
