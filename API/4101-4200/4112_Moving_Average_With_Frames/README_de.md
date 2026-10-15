# Gleitender Durchschnitt mit Frames
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Umrechnung des MetaTrader 5 Expert Advisors **„Moving Average with Frames“**. Das ursprüngliche System wertet die Beziehung zwischen den Eröffnungs-/Schlusskursen jeder Kerze und einem verschobenen einfachen gleitenden Durchschnitt (SMA) aus und zeigt gleichzeitig mehrere Optimierungs-„Frames“ in Diagrammen an. Dieser StockSharp-Port konzentriert sich auf die Handelslogik: Er reagiert nur einmal pro abgeschlossenem Balken, eröffnet eine einzelne Netting-Position und spiegelt die Geldverwaltungsregeln aus dem Quellcode wider.

## Handelslogik

- **Datenquelle** – Die Strategie abonniert den konfigurierten Zeitrahmen (`CandleType`) und verarbeitet nur fertige Kerzen, wodurch die MetaTrader-Einschränkung `if(rt[1].tick_volume>1) return;` reproduziert wird.
- **Indikator** – ein einfacher gleitender Durchschnitt mit der Periode `MovingPeriod`. Die Indikatorausgabe wird um `MovingShift` abgeschlossene Kerzen nach vorne verschoben, indem ein Puffer vergangener Werte beibehalten wird.
- **Aufwärmphase** – Der Handel wird ausgesetzt, bis mindestens 100 abgeschlossene Kerzen gesammelt wurden, was dem ursprünglichen `Bars(_Symbol,_Period)>100`-Guard entspricht.
- **Eintrittsbedingungen**
  - Gehen Sie **long**, wenn die Kerze unterhalb des verschobenen SMA öffnet und darüber schließt.
  - Gehen Sie **short**, wenn die Kerze über dem verschobenen SMA öffnet und darunter schließt.
  - Der Motor erzwingt eine einzelne Position: Die entgegengesetzte Belichtung wird abgeflacht, bevor in die neue Richtung eingetreten wird.
- **Ausstiegsbedingungen** – eine bestehende Long-Position wird geschlossen, wenn der Eröffnungspreis über und der Schlusskurs unter dem verschobenen SMA liegt; Shorts sind auf der gegenüberliegenden Kreuzung geschlossen. Neue Trades werden nach einem Ausstieg nicht auf demselben Balken eröffnet, genau wie beim ursprünglichen Experten.

## Positionsgröße und Risiko

- **MaximumRisk** – bestimmt das rohe Auftragsvolumen als `Portfolio.CurrentValue * MaximumRisk / price`, wenn Portfoliodaten verfügbar sind. Wenn der Broker-Feed keine Aktieninformationen bereitstellt, greift die Strategie auf die manuelle Eigenschaft `Volume` zurück.
- **DecreaseFactor** – nach mehr als einem Verlusthandel in Folge wird die nächste Positionsgröße um `volume * losses / DecreaseFactor` reduziert, was die Lot-Reduktionslogik von MetaTrader nachahmt. Jeder gewinnbringende Handel setzt den Zähler zurück.
- **Volumenausrichtung** – die berechnete Größe wird auf `VolumeStep` des Instruments normalisiert, zwischen `MinVolume` und `MaxVolume` eingeklemmt und auf zwei Dezimalstellen gerundet, wenn die Börse keinen Schritt veröffentlicht.

## Zusätzliche Hinweise

- Die MetaTrader-Visualisierung „Frames“ wird nicht portiert, da StockSharp bereits umfangreiche Optimierungs-Dashboards bereitstellt. Die Handelslogik, das Signal-Timing und das Größenverhalten bleiben der Quelle treu.
- Alle Indikatorwerte werden direkt vom `Bind`-Callback verbraucht; Es werden keine manuellen `GetValue`-Aufrufe verwendet.
- Die fortlaufende Verlustverfolgung ist in `OnOwnTradeReceived` implementiert, sodass die Strategie korrekt auf Teilfüllungen und Netting-Verhalten reagieren kann.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `MaximumRisk` | `0.02` | Anteil des Portfolio-Eigenkapitals, das bei jedem Einstieg riskiert wird. |
| `DecreaseFactor` | `3` | Divisor wird verwendet, um die Positionsgröße nach zwei oder mehr aufeinanderfolgenden Verlusten zu verkleinern. |
| `MovingPeriod` | `12` | Länge des einfachen gleitenden Durchschnitts, der auf die Schlusskurse angewendet wird. |
| `MovingShift` | `6` | Anzahl der abgeschlossenen Kerzen, die zum zeitlichen Versatz von SMA nach vorne verwendet werden. |
| `CandleType` | `1h time frame` | Von der Strategie verarbeitete primäre Kerzenserie. |

## Nutzungstipps

1. Hängen Sie die Strategie in StockSharp Designer oder Code an ein Wertpapier und Portfolio an.
2. Passen Sie den Kerzentyp an den gewünschten Diagrammzeitraum von MetaTrader an.
3. Passen Sie `MaximumRisk` und `DecreaseFactor` an Ihre Kontogröße und die gewünschte Risikotoleranz an.
4. Führen Sie Backtests durch, um zu überprüfen, ob die Crossover-Signale mit den ursprünglichen MetaTrader-Ergebnissen übereinstimmen.
