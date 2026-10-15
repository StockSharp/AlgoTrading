# Ichimoku Strategie 2005
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine direkte Portierung des MetaTrader-Expertenberaters `ichimok2005`, der auf den StockSharp-Experten API zugeschnitten ist. Es konzentriert sich auf die Identifizierung entscheidender Ausbrüche über oder unter der Senkou Span B-Linie Ichimoku und bestätigt die Dynamik durch aufeinanderfolgende Kerzenkörper.

## Handelslogik

### Lange Einrichtung
1. Bewerten Sie die letzten `Shift + 2` abgeschlossenen Kerzen (der Standardwert für `Shift` ist `1`, sodass der Algorithmus die vorherigen drei Balken berücksichtigt).
2. Fordern Sie Folgendes:
   - Die älteste Referenzkerze (`Shift + 2`) öffnete sich unterhalb von Senkou Span B.
   - Die mittlere Referenzkerze (`Shift + 1`) öffnete über Senkou Span B und schloss darüber.
   - Die letzte Referenzkerze (`Shift`) öffnete und schloss über Senkou Span B.
   - Die letzten beiden Referenzkerzen sind bullisch (Schlusskurs ist höher als Eröffnungspreis).
3. Stellen Sie sicher, dass der Ichimoku Chinkou Span nicht in der Wolke gefangen ist, wenn Senkou Span A unter Senkou Span B liegt. Dies imitiert den ursprünglichen Expert Advisor-Filter, der überlastete Marktphasen vermeidet.
4. Wenn die Strategie derzeit eine Short-Position hält, wird diese geschlossen. Andernfalls wird ein neuer Long-Trade eröffnet, sofern das vorherige Signal nicht bereits Long war.

### Kurze Einrichtung
1. Spiegeln Sie die Long-Bedingungen in die entgegengesetzte Richtung:
   - Kerze `Shift + 2` muss sich über Senkou Span B öffnen.
   - Kerze `Shift + 1` muss unterhalb von Senkou Span B öffnen und schließen.
   - Kerze `Shift` muss unterhalb von Senkou Span B öffnen und schließen.
   - Die letzten beiden Referenzkerzen sind bärisch (Schlusskurs ist niedriger als Eröffnungspreis).
2. Die Chinkou-Spanne muss außerhalb der Wolke bleiben, wenn Senkou-Spanne A unter Senkou-Spanne B liegt.
3. Schließen Sie alle vorhandenen Long-Positionen und eröffnen Sie dann eine neue Short-Position, wenn das vorherige Signal nicht Short war.

Positionen werden mit den Schutzanordnungen von StockSharp verwaltet. Stop-Loss und Take-Profit werden in Preisschritten gemessen und mithilfe des `PriceStep` des Instruments in absolute Abstände umgerechnet. Schutzaufträge werden bei Marktaustritten registriert, um das MetaTrader-Verhalten bei der Verwendung von Marktstopps zu reproduzieren.

## Positionsgrößen

Der ursprüngliche Advisor unterstützte zwei Größenmodi:
- **Festes Volumen** (`UseMoneyManagement = false`): Trades werden mit dem Parameter `OrderVolume` ausgeführt (Standard 0,1 Lots).
- **Geldmanagement** (`UseMoneyManagement = true`): Die Strategie verwendet den aktuellen Wert des Portfolios und den Prozentsatz `MaximumRisk`, um die Auftragsgröße abzuleiten. Das Ergebnis wird an der Losstufe des Wertpapiers festgehalten und fällt nie unter eine einzelne Stufe.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. | 30 |
| `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten. | 60 |
| `Shift` | Anzahl der Balken, die bei der Validierung der Breakout-Struktur als Offset verwendet werden. | 1 |
| `OrderVolume` | Handelsgröße behoben, wenn die Geldverwaltung deaktiviert ist. | 0,1 |
| `MaximumRisk` | Portfolio-Prozentsatz, der zur Größenbestimmung von Aufträgen verwendet wird, wenn die Geldverwaltung aktiviert ist. | 10 |
| `UseMoneyManagement` | Ermöglicht eine risikobasierte Positionsgrößenbestimmung. | falsch |
| `TenkanPeriod` | Tenkan-sen-Periode des Ichimoku-Indikators. | 9 |
| `KijunPeriod` | Kijun-Sen-Periode des Ichimoku-Indikators. | 26 |
| `SenkouBPeriod` | Senkou Span B-Periode des Ichimoku-Indikators. | 52 |
| `CandleType` | Zeitrahmen für alle Berechnungen (standardmäßig stündliche Kerzen). | 1 Stunde |

## Notizen

- Es werden nur abgeschlossene Kerzen verarbeitet, wodurch garantiert wird, dass die Ichimoku-Werte endgültig sind.
- Die Strategie verfolgt die zuletzt ausgeführte Richtung (`_lastSignal`), um die Wiederholung identischer Befehle bei aufeinanderfolgenden Signalen zu vermeiden, was dem Expertenverhalten von MetaTrader entspricht.
- Wenn das Instrument `PriceStep` nicht veröffentlicht, werden die Stop-Loss- und Take-Profit-Abstände als absolute Preiswerte behandelt.
