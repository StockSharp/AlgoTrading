# S7 Up Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsystem, das nach nahezu gleichen Hochs oder Tiefs sucht, gefolgt von einer scharfen Preisbewegung.
Wenn zwei aufeinanderfolgende Tiefs fast gleich sind und der Preis um `Span Price` steigt, geht der Bot long.
Es geht short, wenn sich zwei Hochs angleichen und der Preis um `Span Price` fällt.
Positionen werden mit optionalen Take-Profit-, Stop-Loss-, Trailing-Stop- und Frühaustiegsfunktionen geschützt.

## Details

- **Einstiegskriterien:**
  - **Kauf:** Differenz zwischen aktuellem und vorherigem Tief liegt unter `HL Divergence` und der Preis liegt `Span Price` über dem Tief.
  - **Verkauf:** Differenz zwischen aktuellem und vorherigem Hoch liegt unter `HL Divergence` und der Preis liegt `Span Price` unter dem Hoch.
- **Long/Short:** Beide.
- **Ausstiegskriterien:**
  - Take-Profit oder Stop-Loss.
  - Trailing-Stop oder Null-Trailing-Anpassung.
  - Frühaustieg, wenn der Preis das vorherige Hoch/Tief kreuzt (`Exit At Extremum`) oder sich dem Umkehrniveau nähert (`Exit At Reversal`).
- **Stops:** Absolute Take-Profit und Stop-Loss mit optionalem Trailing.
- **Filter:** Keine.

## Parameter

- `Take Profit` – Gewinnziel in Preiseinheiten.
- `Stop Loss` – Verlustlimit in Preiseinheiten, 0 für automatischen extremwertbasierten Stop.
- `HL Divergence` – maximal zulässige Differenz zwischen zwei aufeinanderfolgenden Hochs oder Tiefs.
- `Span Price` – Abstand vom Extremwert zum Preis für den Einstieg.
- `Max Trades` – maximale gleichzeitige Trades.
- `Use Trailing Stop` – Trailing-Stop-Mechanismus aktivieren.
- `Trail Stop` – Trailing-Stop-Abstand.
- `Zero Trailing` – Stop in Richtung Preis bewegen, sobald Position profitabel ist.
- `Step Trailing` – minimaler Schritt zur Anpassung des Null-Trailings.
- `Exit At Extremum` – schließen, wenn Preis das vorherige Hoch/Tief kreuzt.
- `Exit At Reversal` – schließen, wenn Preis sich dem gegenüberliegenden Extremwert nähert.
- `Span To Revers` – Abstand vom Extremwert zum Auslösen des Umkehrausstiegs.
- `Candle Type` – Zeitrahmen für die Analyse.
- `Order Volume` – Menge pro Trade.
