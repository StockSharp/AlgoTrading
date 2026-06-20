# Estrategia MACD + DMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina el Moving Average Convergence Divergence con el Directional Movement Index para operar solo cuando se confirma la fuerza de la tendencia. El sistema espera un cruce de MACD y verifica que la línea direccional dominante supere la línea opuesta mientras el ADX está por encima de un nivel clave.

La estrategia está diseñada para posiciones largas y cortas. Al combinar filtros de momentum y tendencia, busca evitar las señales falsas en mercados laterales. Los stops de protección basados en volatilidad mantienen el riesgo controlado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La línea MACD cruza por encima de la señal, +DI > -DI, y ADX por encima del nivel clave.
  - **Corto**: La línea MACD cruza por debajo de la señal, -DI > +DI, y ADX por encima del nivel clave.
- **Criterios de salida**:
  - Señal inversa o stop de volatilidad alcanzado.
- **Indicadores**:
  - MACD (rápida 12, lenta 26, señal 9)
  - Directional Movement Index (longitud 14, suavizado ADX 14)
- **Stops**: Utiliza stop-loss y take-profit incorporados mediante StartProtection.
- **Valores predeterminados**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **Filtros**:
  - Seguimiento de tendencia
  - Funciona en múltiples marcos temporales
  - Indicadores: MACD, DMI
  - Stops: Sí
  - Complejidad: Moderado
