# Estrategia Exp Oracle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto a C# del asesor experto de MetaTrader **Exp_Oracle**. Se basa en un indicador personalizado *Oracle* que combina el Índice de Fuerza Relativa (RSI) y el Índice de Canal de Materias Primas (CCI) para pronosticar la dirección del mercado varios bares adelante. El indicador genera dos líneas:

- **Línea Oracle** – combinación bruta de los extremos de CCI y RSI.
- **Línea de señal** – media móvil suavizada de la línea Oracle.

La estrategia proporciona tres modos de trading para interpretar estas líneas:

1. **Breakdown** – abre posiciones cuando la línea de señal cruza el nivel cero.
2. **Twist** – reacciona a los puntos de inflexión locales de la línea de señal.
3. **Disposition** – opera en los cruces entre la línea de señal y la línea Oracle.

## Parámetros

- `OraclePeriod` – período para los cálculos de RSI y CCI.
- `Smooth` – número de barras utilizadas para suavizar la línea de señal.
- `Mode` – algoritmo utilizado para generar señales de trading (`Breakdown`, `Twist` o `Disposition`).
- `CandleType` – marco temporal de las velas entrantes.
- `AllowBuy` – habilita entradas largas.
- `AllowSell` – habilita entradas cortas.
- `Volume` – volumen de la estrategia heredado de la clase base `Strategy`.

## Reglas de entrada y salida

### Breakdown
- **Comprar** cuando la línea de señal cruza por encima de cero.
- **Vender** cuando la línea de señal cruza por debajo de cero.

### Twist
- **Comprar** cuando la línea de señal gira hacia arriba después de una caída.
- **Vender** cuando la línea de señal gira hacia abajo después de una subida.

### Disposition
- **Comprar** cuando la línea de señal cruza por encima de la línea Oracle.
- **Vender** cuando la línea de señal cruza por debajo de la línea Oracle.

Las posiciones existentes se cierran y se revierten cuando aparece una señal opuesta. La estrategia usa órdenes de mercado por simplicidad.

## Lógica del indicador

Para cada barra:
1. Calcular RSI y CCI con el `OraclePeriod` especificado.
2. Construir cuatro valores de divergencia usando diferencias entre los valores recientes de CCI y RSI.
3. La línea Oracle es la suma de la divergencia máxima y mínima.
4. La línea de señal es la media móvil simple de la línea Oracle sobre `Smooth` barras.

Este enfoque intenta predecir el movimiento de precio a corto plazo combinando información de momentum (RSI) y de canal (CCI).

## Notas

- La estrategia opera únicamente sobre velas completadas.
- Los stops de protección no están implementados; use controles de riesgo externos si es necesario.
