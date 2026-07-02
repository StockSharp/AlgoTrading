# Fibonacci Strategie für potenzielle Einträge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie reproduziert das Verhalten des ursprünglichen **EA_PUB_FibonacciPotentialEntries** Expertenberaters. Es platziert zwei Limit-Orders auf den Retracement-Levels 50 % und 61 % Fibonacci und verwaltet ihren Lebenszyklus mithilfe des hohen Levels StockSharp API.

## Handelslogik

1. **Erstplatzierung**
   - Sobald Geld-/Briefkurse verfügbar sind, berechnet die Strategie den aktuellen Spread und übermittelt zwei Limit-Orders:
     - Order Nr. 1: platziert auf dem 50 %-Niveau mit einem Schutzstopp unterhalb (oder über dem 61 %-Niveau) (bei Shorts).
     - Order Nr. 2: platziert auf dem 61 %-Niveau mit einem Schutzstopp auf halber Höhe des 100 %-Niveaus.
   - Die Volumina sind so bemessen, dass der erste Trade 0,7 % des Portfolios riskiert und der zweite Trade den verbleibenden Teil des `RiskPercent`-Parameters riskiert.

2. **Zielhandhabung**
   - Wenn der Preis das `TargetPrice`-Niveau erreicht, schließt die Strategie die Hälfte jeder gefüllten Position mithilfe von Marktaufträgen.
   - Nach einem teilweisen Ausstieg ist das verbleibende Volumen zum Break-Even (Einstiegspreis) geschützt. Wenn der Markt auf dieses Niveau zurückkehrt, wird der Rest der Position automatisch geschlossen.

3. **Richtung**
   - `IsBullish = true` schafft Kauflimits (ursprüngliche bullische Vorlage).
   - `IsBullish = false` spiegelt das Verhalten mit Verkaufslimits und umgekehrten Stop/Ziel-Prüfungen wider.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `PriceOn50Level` | Preisniveau für die erste Limit-Order. |
| `PriceOn61Level` | Preisniveau für die zweite Limit-Order. |
| `PriceOn100Level` | Referenzniveau, das zur Berechnung des zweiten Handelsstopps verwendet wird. |
| `TargetPrice` | Gemeinsames Gewinnziel für beide Positionen. |
| `RiskPercent` | Gesamtprozentsatz des Portfolio-Eigenkapitals, das bei beiden Einträgen riskiert wurde. |
| `IsBullish` | Wählt zwischen langen und kurzen Setups. |

## Konvertierungshinweise

- Es werden nur High-Level-Helfer (`SubscribeLevel1`, `BuyLimit`, `SellLimit`, `BuyMarket`, `SellMarket`) verwendet, genau wie in den Repository-Richtlinien gefordert.
- Teilweise Ausstiege und Break-Even-Stopp-Anpassungen werden mit Marktaufträgen reproduziert und entsprechen dem Verhalten des MQL-Roboters, ohne auf Befehlsänderungsaufrufe auf niedriger Ebene angewiesen zu sein.
- Positionsvolumina werden auf den Instrumentenvolumenschritt normalisiert, um den StockSharp-Konventionen zu entsprechen.
