# Genie Pivot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie setzt die **Genie Pivot** Umkehr-Scalping-Idee um, die ursprünglich in MQL4 geschrieben wurde. Sie wartet auf ein Pivot-Muster, das aus sieben aufeinanderfolgenden Kerzen besteht, und verwaltet die offene Position mit einem festen Take-Profit und einem Trailing-Stop.

## Strategielogik

1. **Mustererkennung** – ein Long-Signal erscheint, wenn die sieben vorherigen Tiefs streng fallend sind und die letzte abgeschlossene Kerze ein höheres Tief mit einem Schluss oberhalb des vorherigen Hochs bildet. Ein Short-Signal wird durch die gespiegelte Bedingung bei den Hochs generiert.
2. **Orderausführung** – sobald ein Signal bestätigt ist, öffnet die Strategie eine Marktorder mit dem Volumen, das aus dem Kontokapital und den konfigurierten Risikoparametern berechnet wird.
3. **Trade-Management** – nach dem Einstieg werden ein Take-Profit und ein Trailing-Stop gesetzt. Der Trailing-Stop wird nur aktiviert, wenn der Gewinn die Trailing-Distanz überschreitet. Kehrt der Preis bei der folgenden Kerze um (Bärenkerze für Long, Bullenkerze für Short), wird die Position sofort geschlossen.
4. **Volumenreduktion** – aufeinanderfolgende Verlustgeschäfte reduzieren das gehandelte Volumen gemäß dem Parameter `Decrease Factor`.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `TakeProfit` | Gewinnziel in Preisschritten vom Einstiegspreis. |
| `TrailingStop` | Trailing-Stop-Abstand in Preisschritten. |
| `MaximumRisk` | Anteil des Kontowerts für die Positionsgrößenbestimmung. |
| `DecreaseFactor` | Reduziert das Volumen nach aufeinanderfolgenden Verlusten. |
| `BaseVolume` | Ausweichvolumen, wenn der Portfoliowert unbekannt ist. |
| `CandleType` | Zeitrahmen der zu analysierenden Kerzen. |

## Hinweise

Die Strategie verarbeitet nur abgeschlossene Kerzen. Eine Python-Version ist noch nicht vorhanden.
