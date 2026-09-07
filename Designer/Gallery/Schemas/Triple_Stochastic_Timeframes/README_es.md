# Diagrama de estrategia estocástica de tres marcos temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama combina el impulso formado de Stochastic(5,3) en velas de 60, 15 y 5 minutos. Una vez por hora sincroniza las tres diferencias %K-%D y opera un giro del impulso de cinco minutos que coincide con los dos marcos superiores.

![schema](schema.svg)

## Resumen de la estrategia

- Tres flujos de velas terminadas aportan datos de 60, 15 y 5 minutos y pueden construirse desde marcos menores.
- Cada flujo calcula Stochastic %K(5), lo suaviza con SMA(3) para obtener %D y resta %D de %K.
- La diferencia horaria toma los últimos valores de los tres flujos; Sync libera un grupo completo de tres valores en cada cierre horario.
- La diferencia previa sincronizada de cinco minutos detecta un giro en la línea cero, mientras la posición actual impide acumular más de una unidad.
- Ambas acciones son operaciones a mercado de una unidad y Strategy trades envía cada ejecución al gráfico.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando la diferencia de entrada previa es mayor que cero, la actual es menor o igual que cero, las dos diferencias superiores son positivas y la posición no es larga, comprar una unidad a mercado.
- **Entrada en corto**: Cuando la diferencia de entrada previa es menor que cero, la actual es mayor o igual que cero, las dos diferencias superiores son negativas y la posición no es corta, vender una unidad a mercado.
- **Salida**: No hay una rama de salida separada. Una acción contraria válida de una unidad cierra hasta cero una posición opuesta de una unidad; una señal válida posterior puede abrir la otra dirección. El diagrama no tiene stop-loss, take-profit ni periodo de espera.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Higher Candles Series | 01:00:00 | Velas terminadas de 60 minutos para el cálculo superior y el pulso de decisión horario. |
| Higher Stochastic %K Length | 5 | Longitud de Stochastic %K del marco superior. |
| Higher Stochastic %K Source | Not set | No se selecciona otro campo de entrada del indicador; las velas superiores se conectan directamente. |
| Higher Stochastic %D SMA Length | 3 | Longitud de suavizado aplicada al %K superior para obtener %D. |
| Higher Stochastic %D SMA Source | Not set | No se selecciona otro campo de entrada del indicador; el %K superior se conecta directamente. |
| Middle Candles Series | 00:15:00 | Velas terminadas de 15 minutos para el cálculo del marco intermedio. |
| Middle Stochastic %K Length | 5 | Longitud de Stochastic %K del marco intermedio. |
| Middle Stochastic %K Source | Not set | No se selecciona otro campo de entrada del indicador; las velas intermedias se conectan directamente. |
| Middle Stochastic %D SMA Length | 3 | Longitud de suavizado aplicada al %K intermedio para obtener %D. |
| Middle Stochastic %D SMA Source | Not set | No se selecciona otro campo de entrada del indicador; el %K intermedio se conecta directamente. |
| Entry Candles Series | 00:05:00 | Velas terminadas de 5 minutos para el cálculo de entrada y el gráfico. |
| Entry Stochastic %K Length | 5 | Longitud de Stochastic %K del marco de entrada. |
| Entry Stochastic %K Source | Not set | No se selecciona otro campo de entrada del indicador; las velas de entrada se conectan directamente. |
| Entry Stochastic %D SMA Length | 3 | Longitud de suavizado aplicada al %K de entrada para obtener %D. |
| Entry Stochastic %D SMA Source | Not set | No se selecciona otro campo de entrada del indicador; el %K de entrada se conecta directamente. |
| Order Volume | 1 | Volumen de mercado fijo utilizado por ambas acciones. |

## Detalles del diagrama

- Todos los indicadores emiten únicamente valores formados. SMA(3) recibe el %K correspondiente, por lo que cada diferencia es exactamente %K menos su media de tres valores.
- La diferencia horaria activa tres retenedores numéricos antes de que sus valores entren en Sync; así, los retenedores intermedio y de entrada aportan sus últimas lecturas disponibles.
- Sync vacía cada grupo completo y emite tres valores alineados. El bloque Previous value conserva una diferencia de entrada sincronizada para la siguiente comparación horaria.
- La posición se toma junto con la decisión sincronizada. Una posición menor o igual que cero permite comprar y una mayor o igual que cero permite vender, evitando acumular en la misma dirección.
- El gráfico recibe cinco flujos: velas de cinco minutos, las tres diferencias %K-%D sin muestrear y todas las ejecuciones de la estrategia.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
