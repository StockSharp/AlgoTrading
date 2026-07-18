# Kanalhandel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konträre Kanalstrategie, die Donchian-Kanal-Extreme ausblendet, wenn die Bandbreite unverändert bleibt. Das System vergleicht das neueste Hoch/Tief mit den vorherigen Kanalgrenzen und einem aus dem vorherigen Schlusskurs berechneten Pivot, um zu entscheiden, ob es den Bewegung ausblendet. Schutzstopp basiert auf ATR-Abstand und ein optionaler Trailing Stop sichert Gewinne, sobald der Preis zugunsten der Position läuft.

## Details

- **Einstiegskriterien**:
  - Short: oberes Kanalband unverändert und entweder das letzte Kerzenhoch berührte das obere Band oder der vorherige Schlusskurs liegt zwischen dem Pivot und dem oberen Band.
  - Long: unteres Kanalband unverändert und entweder das letzte Kerzentief berührte das untere Band oder der vorherige Schlusskurs liegt zwischen dem Pivot und dem unteren Band.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Long schließen, wenn das obere Band flach ist und der Preis es berührt, oder wenn der ATR-Stop oder Trailing Stop ausgelöst wird.
  - Short schließen, wenn das untere Band flach ist und der Preis es berührt, oder wenn der ATR-Stop oder Trailing Stop ausgelöst wird.
- **Stops**:
  - Anfangsstopp für Longs bei `support - ATR` und für Shorts bei `resistance + ATR`.
  - Der Trailing Stop bewegt sich hinter dem besten Preis, sobald der Gewinn die `TrailingStopPips`-Distanz übersteigt (in Preisschritte umgerechnet).
- **Standardwerte**:
  - `ChannelPeriod` = 20 (Donchian-Lookback)
  - `AtrPeriod` = 4 (ATR-Glättung)
  - `Volume` = 1 Kontrakt/Lot
  - `TrailingStopPips` = 30 Preisschritte
  - `CandleType` = 1-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Kanal / Mean Reversion
  - Richtung: Long und Short
  - Indikatoren: Donchian Channel, ATR
  - Stops: ATR-Hartstopp + Trailing Stop
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Hinweise

- Der Pivot entspricht `(oberes Band + unteres Band + vorheriger Schlusskurs) / 3` und entspricht der ursprünglichen MQL-Implementierung.
- Die Strategie hält nur eine Nettoposition und wechselt die Richtung erst nachdem der vorherige Trade vollständig geschlossen wurde.
- Der Trailing-Abstand wird in Preisschritten ("Pips") angegeben; er wird mit dem `PriceStep` des Instruments multipliziert, um den tatsächlichen Preisabstand zu erhalten.
