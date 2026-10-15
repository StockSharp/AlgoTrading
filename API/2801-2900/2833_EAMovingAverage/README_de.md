# EA Moving Average-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertiert vom MetaTrader Expert Advisor **"EA Moving Average"** (barabashkakvn-Edition).
- Verwendet vier unabhängige gleitende Durchschnitte zur Steuerung von Long- und Short-Einstiegen und -Ausstiegen.
- Konzipiert für ein einzelnes Symbol im Netting-Modus. Der Standard-Kerzentyp ist der 15-Minuten-Zeitrahmen, aber jeder reguläre Kerzentyp kann ausgewählt werden.
- Die Strategie öffnet höchstens eine Position gleichzeitig. Während eine Position aktiv ist, werden nur die Ausstiegsregeln ausgewertet.

## Handelslogik
### Long-Einstieg
1. Die aktuelle Kerze muss nach dem Öffnen unterhalb des *Buy Open*-Gleitdurchschnitts oberhalb davon schließen (echter Kreuzungsübergang innerhalb einer einzelnen Kerze).
2. `UseBuy` muss aktiviert sein.
3. Wenn `ConsiderPriceLastOut` aktiviert ist, muss der aktuelle Preis kleiner oder gleich dem Preis des letzten geschlossenen Trades sein. Dies verhindert den Kauf oberhalb des letzten Ausstiegs.
4. Wenn die Bedingungen erfüllt sind, gibt die Strategie eine Markt-Kauforder aus, die durch das Risikomodell dimensioniert wird.

### Long-Ausstieg
1. Aktiv nur, während die Nettoposition long ist.
2. Die Kerze muss oberhalb des *Buy Close*-Gleitdurchschnitts öffnen und darunter zurückschließen, was ein bärisches Kreuzungssignal anzeigt.
3. Wenn ausgelöst, wird die gesamte Position mit einer Marktorder geschlossen.

### Short-Einstieg
1. Die Kerze muss unterhalb des *Sell Open*-Gleitdurchschnitts schließen, nachdem sie darüber geöffnet hat.
2. `UseSell` muss aktiviert sein.
3. Wenn `ConsiderPriceLastOut` aktiviert ist, muss der aktuelle Preis größer oder gleich dem letzten Ausstiegspreis sein. Dies vermeidet das Shorten unterhalb der vorherigen Eindeckung.
4. Eine Markt-Verkaufsorder wird mit dem risikobasierten Volumen eingereicht.

### Short-Ausstieg
1. Aktiv nur, während die Position short ist.
2. Die Kerze muss unterhalb des *Sell Close*-Gleitdurchschnitts öffnen und darüber schließen.
3. Die Short-Position wird vollständig zu Marktpreisen eingedeckt.

## Risiko und Positionsgrößenbestimmung
- `MaximumRisk` drückt das Risikokapital pro Trade als Bruchteil des Portfolio-Eigenkapitals aus. Die Strategie teilt diesen Risikobetrag durch den aktuellen Preis, um eine Rohvolumenschätzung zu erhalten.
- `DecreaseFactor` emuliert die ursprüngliche MetaTrader-Losreduzierung. Nach zwei oder mehr aufeinanderfolgenden Verlust-Trades wird das Volumen proportional zur Verluststrecke geteilt durch `DecreaseFactor` reduziert.
- Volumen werden am Instrument-Volumenschritt ausgerichtet und fallen nie unter einen Schritt. Wenn die Risikoberechnung fehlschlägt, ist der Fallback die `Volume`-Eigenschaft der Strategie (Standard 1 Kontrakt/Lot).

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `MaximumRisk` | `0.02` | Anteil des Eigenkapitals, der pro Trade riskiert wird. |
| `DecreaseFactor` | `3` | Lot-Reduktionsfaktor nach aufeinanderfolgenden Verlusten. `0` zum Deaktivieren. |
| `BuyOpenPeriod` | `30` | Periode des gleitenden Durchschnitts für Long-Einstiege. |
| `BuyOpenShift` | `3` | Vorwärtsverschiebung (Kerzen) des Long-Einstiegs-Gleitdurchschnitts. |
| `BuyOpenMethod` | `Exponential` | Methode des gleitenden Durchschnitts für Long-Einstiege (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). |
| `BuyOpenPrice` | `Close` | Preiseingang für den Long-Einstiegs-Gleitdurchschnitt. |
| `BuyClosePeriod` | `14` | Periode des Long-Ausstiegs-Gleitdurchschnitts. |
| `BuyCloseShift` | `3` | Verschiebung (Kerzen) des Long-Ausstiegs-Gleitdurchschnitts. |
| `BuyCloseMethod` | `Exponential` | Methode des Long-Ausstiegs-Gleitdurchschnitts. |
| `BuyClosePrice` | `Close` | Preiseingang für den Long-Ausstiegs-Gleitdurchschnitt. |
| `SellOpenPeriod` | `30` | Periode des Short-Einstiegs-Gleitdurchschnitts. |
| `SellOpenShift` | `0` | Verschiebung (Kerzen) des Short-Einstiegs-Gleitdurchschnitts. |
| `SellOpenMethod` | `Exponential` | Methode des Short-Einstiegs-Gleitdurchschnitts. |
| `SellOpenPrice` | `Close` | Preiseingang für den Short-Einstiegs-Gleitdurchschnitt. |
| `SellClosePeriod` | `20` | Periode des Short-Ausstiegs-Gleitdurchschnitts. |
| `SellCloseShift` | `2` | Verschiebung (Kerzen) des Short-Ausstiegs-Gleitdurchschnitts. |
| `SellCloseMethod` | `Exponential` | Methode des Short-Ausstiegs-Gleitdurchschnitts. |
| `SellClosePrice` | `Close` | Preiseingang für den Short-Ausstiegs-Gleitdurchschnitt. |
| `UseBuy` | `true` | Long-Trades aktivieren oder deaktivieren. |
| `UseSell` | `true` | Short-Trades aktivieren oder deaktivieren. |
| `ConsiderPriceLastOut` | `true` | Preisverbesserung gegenüber dem letzten Ausstieg vor dem Wiedereinstieg verlangen. |
| `CandleType` | 15m-Zeitrahmen | Für Berechnungen verwendete Kerzenserie. |

## Zusätzliche Hinweise
- Der letzte Ausstiegspreis und der Zähler für aufeinanderfolgende Verluste werden aus Trade-Ausführungen verfolgt und spiegeln das MetaTrader-Verhalten wider.
- Da StockSharp auf fertigen Kerzen ausführt, vergleicht der Einstiegspreisfilter mit dem Kerzenschlusskurs, was den ursprünglichen tickbasierten Ask/Bid-Vergleich annähert.
- Die Strategie setzt ein Netting-Konto voraus; das gleichzeitige Absichern mehrerer Positionen wird nicht unterstützt.
- Validieren Sie die Konfiguration immer mit historischen Tests, bevor Sie mit realem Kapital handeln.
