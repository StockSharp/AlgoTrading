# Clouds Trade 2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Portierung des Expert Advisors "cloud's trade 2" von Vladimir Karputov. Sie handelt Ausbrüche, die durch zwei aktuelle Bill Williams-Fraktale und einen Überkauft/Überverkauft-Crossover am Stochastik-Oszillator bestätigt werden. Das Trade-Management spiegelt die ursprünglichen Eingaben mit konfigurierbarem Stop-Loss, Take-Profit, Trailing Stop und Mindestgewinn-Sicherungen wider.

## Handelslogik

- **Daten**: Einzelne Zeitrahmen-Kerzen (Standard 15 Minuten).
- **Indikatoren**:
  - Stochastik-Oszillator mit dem konfigurierten %K-Lookback, Verlangsamung und %D-Glättung.
  - Rollierendes Fünf-Kerzen-Hoch/Tief-Fenster zur Rekonstruktion oberer und unterer Fraktale.
- **Einstieg**:
  - **Long**: Zwei aufeinanderfolgende untere Fraktale erscheinen aktueller als ein oberes Fraktal **oder** das stochastische %D fällt unter 20 und kreuzt unter %K. Es darf keine Position offen sein und der optionale Ein-Trade-pro-Tag-Filter muss einen neuen Eintritt erlauben.
  - **Short**: Zwei aufeinanderfolgende obere Fraktale erscheinen zuerst **oder** das stochastische %D steigt über 80 und kreuzt über %K.
- **Ausstiege & Schutz**:
  - Statische Stop-Loss- und Take-Profit-Abstände vom Einstiegspreis.
  - Optionaler Trailing Stop, der sich nur bewegt, wenn der aktuelle Gewinn die konfigurierte Trailing-Distanz plus Schritt überschreitet.
  - Positionen schließen, sobald entweder ein geldbasiertes oder ein preis-distanzbasiertes Gewinnziel erreicht wird.
  - Stops werden durch Inspektion von Kerzenhochs/-tiefs emuliert, entsprechend dem broker-verwalteten Verhalten in der MQL-Version.

## Parameter

- **Order Volume**: Basis-Ordergröße für Einstiege.
- **Stop/Take Offsets**: Absolute Preisabstände; an den Tick-Wert des Instruments anpassen, um die ursprünglichen pip-basierten Eingaben zu reproduzieren.
- **Trailing Stop & Step**: Abstände in Preiseinheiten, die bestimmen, wann der Stop verschoben wird.
- **Min Profit (Currency / Points)**: Trades schließen, sobald der unrealisierte Gewinn diese Schwellenwerte überschreitet.
- **Use Fractals / Use Stochastic**: Unabhängige Aktivierung jeder Signalkomponente.
- **One Trade Per Day**: Mehrere Einstiege am selben Handelstag verhindern.
- **Stochastic Settings**: %K-Lookback, %K-Verlangsamung und %D-Glättungslängen.
- **Candle Type**: Zeitrahmen für das Kerzen-Abonnement der Strategie.

## Hinweise

- Positions-Gewinnprüfungen approximieren die ursprünglichen Provisions-/Swap-Anpassungen durch Verwendung der Preisbewegung mal Positionsgröße.
- Die Trailing-Logik folgt der MQL-Implementierung, indem erforderlich ist, dass der Gewinn die Trailing-Distanz plus den Schritt überschreitet, bevor der Stop verschoben wird.
- Um die Standard-MQL-pip-basierten Eingaben bei Forex-Paaren zu imitieren, die Stop/Take-Abstände auf den gewünschten Pip-Wert multipliziert mit dem Punkt-Wert des Instruments setzen (zum Beispiel 50 Pips ≈ 0.005 für fünfstellige Notierungen).
