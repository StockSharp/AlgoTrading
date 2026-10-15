# Kanalstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine direkte Portierung des Expertenberaters „MetaTrader 4 „Channels“, der in der öffentlichen Bibliothek von Gordago enthalten ist. Es kombiniert einen sehr schnellen exponentiellen gleitenden Durchschnitt (EMA) mit drei EMA-basierten Hüllkurven, um Momente zu erkennen, in denen der Preis aus komprimierten Zonen austritt. Sobald eine einzelne Position offen ist, stützt sich die Strategie auf Stop-Orders und optionale Trailing-Stops, um Exits zu verwalten, genau wie die ursprüngliche MQL-Implementierung.

## Handelslogik

- Die Strategie abonniert standardmäßig stündliche Kerzen und berechnet:
  - Ein schneller EMA (Länge 2) mit **Schlusspreisen** der Kerze.
  - Ein zweites schnelles EMA (Länge 2) mit **Eröffnungspreisen** der Kerze, erforderlich durch die Short-Einstiegsregeln des Fachberaters.
  - Ein langsamer EMA (Länge 220) bei Schlusskursen, der als Basis für drei Umschlagabweichungen dient: ±1,0 %, ±0,7 % und ±0,3 %.
- Eine **Long-Position** wird eröffnet, wenn der schlussbasierte Fast-EMA eine der sechs historischen Gegenprüfungen erfüllt:
  1. Es verläuft nach oben durch die äußere, um 1 % niedrigere Hülle.
  2. Es kreuzt nach oben durch die untere Hüllkurve von 0,7 %.
  3. Er verbringt zwei aufeinanderfolgende Balken unterhalb der 0,3 %-Untergrenze (überverkaufter Zustand).
  4. Es kreuzt nach oben durch das langsame EMA selbst.
  5. Es kreuzt nach oben durch die obere 0,3 %-Grenze.
  6. Es durchquert die obere Grenze von 0,7 % nach oben.
- Eine **Short**-Position wird eröffnet, wenn der auf Eröffnung basierende schnelle EMA eine der symmetrischen Short-Regeln auslöst:
  1. Es verläuft nach unten durch die äußere 1 %-Oberhülle.
  2. Es kreuzt nach unten durch die obere Hüllkurve von 0,7 %.
  3. Es durchquert die obere 0,3 %-Grenze nach unten.
  4. Es kreuzt nach unten durch den langsamen EMA.
  5. Es kreuzt nach unten durch die 0,3 % untere Hüllkurve.
  6. Es kreuzt nach unten durch die 0,7 % untere Hüllkurve.
- Es kann immer nur eine Marktposition existieren. Ein neues Signal wird ignoriert, während ein Handel aktiv ist, was dem Verhalten des MetaTrader-Experten entspricht.

## Risikomanagement

- Für Long- und Short-Trades können individuelle Stop-Loss- und Take-Profit-Distanzen konfiguriert werden. Wenn dieser Wert auf Null gesetzt ist, werden diese Schutzbefehle übersprungen, wodurch der standardmäßig deaktivierte Status der ursprünglichen Quelle repliziert wird.
- Optionale Trailing-Stops verschärfen die Schutzorder, sobald sich der Preis um mehr als die in Punkten gemessene Trailing-Distanz zugunsten der Position bewegt.
- Alle Schutzaufträge werden automatisch gelöscht, wenn die Position abgeflacht wird oder die Strategie stoppt.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `Candle Type` | Für die Preisanalyse verwendeter Zeitrahmen (Standard: 1 Stunde). |
| `Volume` | Für alle Einträge verwendete Bestellgröße. |
| `Fast EMA` / `Slow EMA` | Zeiträume für die schnellen und langsamen EMAs. |
| `Envelope 1%`, `Envelope 0.7%`, `Envelope 0.3%` | Prozentuale Breite der drei Umschlagbänder. |
| `Buy Stop-Loss`, `Sell Stop-Loss` | Abstand in Punkten zwischen dem Einstiegspreis und dem anfänglichen Stop-Loss für Long- oder Short-Trades. |
| `Buy Take-Profit`, `Sell Take-Profit` | Distanz in Punkten für die optionalen festen Take-Profit-Levels. |
| `Buy Trailing`, `Sell Trailing` | Trailing-Stop-Distanz in Punkten für Long- oder Short-Positionen. |
| `Use Trading Hours` | Aktiviert den Zeitfensterfilter. |
| `From Hour`, `To Hour` | Inklusive Stundengrenzen für die Eröffnung neuer Positionen. Das Fenster wird um Mitternacht umgebrochen, wenn `From` größer als `To` ist. |

## Nutzungshinweise

1. Da die Stoppentfernungen in Punkten definiert sind, werden sie intern mit der Sicherheit `PriceStep` multipliziert. Stellen Sie sicher, dass dieser Schritt mit dem für den Handel verwendeten Instrument übereinstimmt.
2. Die schnelle EMA-Länge ist absichtlich sehr kurz, um den MT4-Experten widerzuspiegeln. Durch Erhöhen ändert sich die Signalfrequenz dramatisch.
3. Der ursprüngliche Berater erlaubte auch das Whitelisting von Konten und akustische Warnungen. Diese wurden weggelassen, da sie plattformspezifisch sind und keinen Einfluss auf die Bestelllogik haben.
