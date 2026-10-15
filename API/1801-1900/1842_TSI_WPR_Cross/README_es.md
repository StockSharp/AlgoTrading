# Estrategia de Cruce TSI WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cruce del True Strength Index (TSI) calculado a partir del oscilador Williams %R.
Cuando el TSI cruza por encima de su línea de señal suavizada, la estrategia entra en posición larga. Cuando el TSI cruza por debajo de la línea de señal, entra en posición corta.

## Parámetros
- **Candle Type**: Marco temporal de las velas utilizadas para el cálculo.
- **Williams %R Period**: Número de barras para el indicador Williams %R.
- **Short Length**: Longitud EMA corta utilizada en el cálculo del TSI.
- **Long Length**: Longitud EMA larga utilizada en el cálculo del TSI.
- **Signal Length**: Longitud EMA aplicada al TSI para formar la línea de señal.

## Reglas de trading
1. Calcular el valor de Williams %R de cada vela completada.
2. Introducir este valor en el indicador True Strength Index.
3. Suavizar el TSI con una EMA para obtener la línea de señal.
4. **Comprar** cuando el TSI cruza por encima de la línea de señal.
5. **Vender** cuando el TSI cruza por debajo de la línea de señal.
6. Las posiciones existentes en dirección opuesta se cierran con una nueva señal.

## Notas
- La estrategia utiliza la API de alto nivel con suscripciones automáticas a velas.
- StartProtection se lanza al inicio para la gestión básica del riesgo.
- Se crean áreas de gráfico para visualizar el TSI, su línea de señal y las operaciones ejecutadas.
