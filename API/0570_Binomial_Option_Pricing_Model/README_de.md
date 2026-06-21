# Binomial-Optionspreismodell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Modul berechnet den theoretischen Preis einer Option mithilfe eines zweistufigen Binomialbaums. Es unterstützt amerikanische oder europäische Stile sowie Call- oder Put-Optionen für verschiedene Anlageklassen. Die Volatilität wird anhand der Standardabweichung der Schlusskurse geschätzt.

Es werden keine Handelssignale erzeugt; die Strategie protokolliert den berechneten Optionspreis für jede abgeschlossene Kerze.

## Details
- **Funktion**: Optionspreisberechnung (keine Trades)
- **Parameter**: Strike Price, Risk Free Rate, Dividend Yield, Asset Class, Option Style, Option Type, Minutes/Hours/Days to expiry, Timeframe
- **Indikatoren**: Standard Deviation
- **Long/Short**: N/A
- **Stops**: Keine
