# Estrategia de Balance de Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia mide el equilibrio entre los movimientos alcistas y bajistas dentro de cada vela. Un balance positivo sugiere que los compradores dominan la barra, mientras que un balance negativo apunta a presión vendedora.

El sistema suaviza este balance con una media móvil. Cuando tanto el balance actual como su promedio están por encima de cero, la estrategia entra en una posición larga. Cuando ambos caen por debajo de cero, entra en corto.

## Detalles

- **Criterios de entrada**: balance > 0 y promedio > 0 para largo; balance < 0 y promedio < 0 para corto.
- **Criterios de salida**: la señal opuesta desencadena la reversión de la posición.
- **Indicadores**: bar balance personalizado, SMA.
- **Largo/Corto**: ambos.
- **Stop-loss**: ninguno.
