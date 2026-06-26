# Starter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Starter-Strategie** ist eine Konvertierung des MetaTrader 5-Experten "Starter (barabashkakvn's edition)". Das System wartet darauf,
dass der Commodity Channel Index (CCI) aus extremem überverkauftem oder überkauftem Bereich zurückprallt, und bestätigt die Bewegung
mit der Steigung eines langfristigen gleitenden Durchschnitts. Wenn der Momentum mit dem Trendfilter übereinstimmt, öffnet die Strategie
eine einzelne Marktposition, deren Größe durch einen konfigurierbaren Risikoanteil des Portfolios bestimmt wird. Schutz-Stops und ein
optionaler Trailing-Mechanismus reproduzieren die Geldmanagement-Regeln des Original-Experten.

## Handelslogik

- **Trendfilter** — ein konfigurierbarer gleitender Durchschnitt (MA) muss schneller als `MaDelta` steigen, um Long-Trades zu erlauben,
  und schneller als `MaDelta` fallen, um Short-Trades zu erlauben. Die Strategie unterstützt dieselben Glättungsmethoden wie die
  MQL-Version (einfach, exponentiell, geglättet, linear gewichtet).
- **CCI-Bestätigung** — der Commodity Channel Index muss von unten wieder über `-CciLevel` kreuzen, um Long-Einträge auszulösen, und
  von oben unter `CciLevel` kreuzen, um Shorts auszulösen. Der Indikator wird nur auf abgeschlossenen Kerzen ausgewertet und spiegelt
  die ursprüngliche bar-by-bar-Verarbeitung wider.
- **Einzelpositionsmodell** — der Algorithmus hält maximal eine offene Position. Neue Signale werden ignoriert, bis der aktuelle Trade
  geschlossen ist, entsprechend der MetaTrader-Logik, die nach Magic Number und Symbol filtert.

### Einstiegsregeln

1. Auf den Kerzenschluss warten.
2. Die neuesten und vorherigen Werte des gleitenden Durchschnitts bei den konfigurierten Verschiebungen berechnen.
3. Die aktuellen und vorherigen CCI-Lesewerte berechnen.
4. **Long gehen**, wenn:
   - Die Steigung des gleitenden Durchschnitts `MaDelta` übersteigt (aktueller MA minus vorheriger MA).
   - Der aktuelle CCI-Wert größer als der vorherige ist.
   - Der CCI durch `-CciLevel` nach oben kreuzt (vorheriger unter dem Schwellenwert, aktueller darüber).
5. **Short gehen**, wenn:
   - Die Steigung des gleitenden Durchschnitts unter `-MaDelta` liegt.
   - Der aktuelle CCI-Wert kleiner als der vorherige ist.
   - Der CCI durch `CciLevel` nach unten kreuzt (vorheriger über dem Schwellenwert, aktueller darunter).

### Ausstiegsregeln

- **Initialer Stop-Loss** — wenn `StopLossPips` größer als null ist, wird der ausgeführte Einstiegspreis um `StopLossPips * PriceStep`
  verschoben, um einen anfänglichen Schutz-Stop zu berechnen.
- **Trailing Stop** — wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` positiv sind, wird der Stop vorgerückt, sobald der
  Preis sich um mindestens den konfigurierten Schritt verbessert. Long-Trades bewegen den Stop auf `Close - TrailingStop`, Shorts auf
  `Close + TrailingStop`.
- **Manuelle Schließung** — wenn der Preis das Stop-Niveau innerhalb der Kerzenspanne berührt, schließt die Strategie die Position
  mit einer Marktorder und setzt den Schutzzustand zurück.

## Risikomanagement

- **Positionsgrößenbestimmung** — das Basisvolumen beträgt `Portfolio.CurrentValue * MaximumRisk / price`. Wenn der Broker oder das
  Backend einen ungültigen Eigenkapitalwert meldet, greift die Strategie auf die manuelle Eigenschaft `Volume` zurück (Standard 1).
- **Verluststrähnen-Reduzierung** — nach zwei oder mehr aufeinanderfolgenden Verlust-Trades wird das Volumen um
  `volume * losses / DecreaseFactor` reduziert, was die ursprüngliche `DecreaseFactor`-Regel nachahmt. Jeder profitable Trade
  setzt den Verlust-Zähler zurück.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `MaximumRisk` | `0.02` | Anteil des Eigenkapitals, der pro Trade riskiert wird, wenn die Position dimensioniert wird. |
| `DecreaseFactor` | `3` | Lot-Reduktionsdivisor, der nach zwei oder mehr aufeinanderfolgenden Verlust-Trades angewendet wird. |
| `CciPeriod` | `14` | Anzahl der Balken, die vom Commodity Channel Index verwendet werden. |
| `CciLevel` | `100` | Überverkauft/Überkauft-Schwellenwert für CCI-Kreuzungen. |
| `CciCurrentBar` | `0` | Verschiebung des aktuellen CCI-Werts (0 = neueste Kerze). |
| `CciPreviousBar` | `1` | Verschiebung des vorherigen CCI-Werts. |
| `MaPeriod` | `120` | Periode des Trendfilter-gleitenden Durchschnitts. |
| `MaMethod` | `Simple` | Glättungsmethode des gleitenden Durchschnitts (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaCurrentBar` | `0` | Auf den gleitenden Durchschnittswert angewendete Verschiebung. |
| `MaDelta` | `0.001` | Minimaler Steigungsunterschied zwischen aktuellen und vorherigen MA-Lesewerten. |
| `StopLossPips` | `0` | Initialer Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). |
| `TrailingStopPips` | `5` | Basis-Trailing-Stop-Abstand in Pips (0 deaktiviert das Trailing). |
| `TrailingStepPips` | `5` | Minimale Pip-Verbesserung, bevor der Trailing Stop vorgerückt wird. |
| `CandleType` | `30m`-Zeitrahmen | Primäre Kerzensubskription, die von der Strategie verarbeitet wird. |

## Implementierungshinweise

- Indikatorpuffer werden intern zwischengespeichert, damit die Strategie auf historische Werte mit beliebigen Verschiebungen zugreifen
  kann, was den MQL-Ansatz der Indizierung von Indikator-Arrays repliziert.
- Die Pip-Größe wird aus `Security.PriceStep` abgeleitet. Wenn das Instrument keinen gültigen Preisschritt meldet, werden die Stop-
  und Trailing-Abstände als null behandelt.
- Alle Kommentare im Code sind gemäß den Repository-Richtlinien in Englisch verfasst.
- Die Python-Version ist absichtlich ausgelassen, wie angefordert.
