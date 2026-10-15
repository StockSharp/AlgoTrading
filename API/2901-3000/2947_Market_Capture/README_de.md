# Markterfassungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Markterfassungs-Strategie reproduziert die Logik des ursprünglichen MetaTrader 5-Experten. Der Algorithmus baut ein dynamisches Grid rund um einen beweglichen Zentrumspreis auf und eröffnet Hedge-artige Trades, wenn der Preis um dieses Zentrum schwingt. Positionen werden ober- und unterhalb des Zentrums mit festen Gewinnzielen verteilt, während Eigenkapital-Meilensteine des Kontos kontrollieren, wann die am stärksten verlierenden Trades liquidiert werden.

## Handelsregeln
- **Zentrumslinie** – die Strategie speichert ein internes Zentrumsniveau, das beim Schlusskurs der ersten verarbeiteten Kerze beginnt. Wenn der Markt weiter als der konfigurierte Grid-Abstand abweicht, wird das Zentrum schrittweise verschoben, um dem Preis zu folgen.
- **Initialer Short** – eine optionale Short-Position kann unmittelbar nach dem Start eröffnet werden, um das Verhalten des MQL-Skripts zu entsprechen.
- **Long-Einstiege** – ein Long-Trade ist erlaubt, wenn der letzte Schlusskurs über dem Zentrum liegt und die vorherige Kerze darunter gehandelt hat. Eine Näheprüfung stellt sicher, dass kein anderer Long-Trade in der Nähe desselben Niveaus aktiv ist.
- **Short-Einstiege** – ein Short-Trade ist erlaubt, wenn der letzte Schlusskurs unter dem Zentrum liegt und die vorherige Kerze darüber gehandelt hat. Derselbe Nähefilter verhindert das Stapeln identischer Shorts.
- **Take Profit** – jeder Trade speichert ein Zielniveau, das ein festes Vielfaches des Instrument-Preisschritts vom Einstiegspreis entfernt ist. Kerzenhochs (für Longs) oder Kerzenchiefs (für Shorts), die das Ziel erreichen, lösen einen Marktausstieg aus.
- **Eigenkapital-Management** – die Strategie überwacht das Portfolio-Eigenkapital. Nach einem konfigurierbaren prozentualen Gewinn schließt sie eine Anzahl der am schlechtesten laufenden Trades, um Gewinne zu sichern. Ein weiterer Prozentschwellenwert definiert, wann das Risiko während eines Rückgangs durch Liquidierung verlierender Trades reduziert werden soll. Jedes Mal, wenn ein Schwellenwert ausgelöst wird, wird die Eigenkapital-Basislinie neu berechnet.

## Parameter
- `Enable Long` / `Enable Short` – Trades in jede Richtung erlauben oder blockieren.
- `Grid Steps` – Abstand zwischen Grid-Niveaus, gemessen in Preisschritten.
- `Take Profit Steps` – Take-Profit-Distanz in Preisschritten.
- `Open Initial Short` – die erste Short-Order, die sofort nach dem Start platziert wird, aktivieren.
- `Use Equity Target` – Eigenkapitalwachstumsregel zum Trimmen verlierender Trades aktivieren.
- `Track Drawdown` – Rückgangsregel zum Trimmen verlierender Trades aktivieren.
- `Equity Gain %` / `Equity Loss %` – Eigenkapitaländerungsprozentzahlen, die die obigen Regeln auslösen.
- `Loss Trades Up` / `Loss Trades Down` – maximale Anzahl verlierender Trades, die beim Auslösen jeder Regel geschlossen werden.
- `Candle Type` – Zeitrahmen oder benutzerdefinierter Kerzentyp für den Entscheidungsprozess.
- `Volume` (Strategie-Eigenschaft) – Handelsgröße für jede Marktorder.

## Hinweise
- Die Strategie führt ein internes Register offener Trades, um den Hedging-Stil des ursprünglichen Experten nachzuahmen, während sie mit dem Netto-Positionsmodell von StockSharp arbeitet.
- Abstandsparameter werden mit dem Instrument-Preisschritt multipliziert; stellen Sie sicher, dass das ausgewählte Instrument einen gültigen `PriceStep`-Wert exponiert.
- Die Logik arbeitet nur mit abgeschlossenen Kerzen. Wählen Sie einen Kerzentyp, der dem beabsichtigten Handelshorizont entspricht, von sehr kurzfristigen Grids bis zu breiteren Swing-Grids.
