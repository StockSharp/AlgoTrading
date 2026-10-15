# Binario 31-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie, konvertiert aus dem MetaTrader-Skript **binario_31**. Der Algorithmus berechnet zwei exponentielle gleitende Durchschnitte mit 144 Perioden auf den Hoch- und Tiefpreisen der Kerzen und bildet so einen dynamischen Kanal. Solange der aktuelle Preis innerhalb des Kanals liegt, bereitet die Strategie Stop-Einstiegsaufträge vor:

- ein Kauf-Stop über dem EMA-Hoch plus einem konfigurierbaren Versatz;
- ein Verkauf-Stop unter dem EMA-Tief minus demselben Versatz.

Wenn der Preis eines dieser Levels durchbricht, wird eine Position in Ausbruchsrichtung eröffnet. Ein schützender Stop wird auf der gegenüberliegenden Seite des Kanals platziert und ein Take-Profit-Ziel relativ zum Einstieg berechnet. Ein optionaler Trailing Stop kann aktiviert werden, um Gewinne zu sichern.

## Parameter

- **EMA Length** – Periode für beide EMAs auf Hoch und Tief.
- **Pip Difference** – Abstand vom EMA-Level bis zum Ausbruchs-Einstieg in Preisschritten.
- **Take Profit** – Abstand vom Einstieg bis zum Take Profit in Preisschritten.
- **Trailing Stop** – Trailing-Stop-Abstand in Preisschritten. Auf null setzen, um zu deaktivieren.
- **Volume** – Auftragsvolumen.
- **Candle Type** – Kerzentyp, den die Strategie abonniert.
