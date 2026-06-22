# Estrategia del Sistema Very Blonde
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia contratendencia basada en cuadrícula inspirada en el "Very Blonde System" original para MetaTrader. La estrategia busca una gran distancia entre el precio actual y los extremos recientes y opera en la dirección contraria.

## Lógica de la estrategia
1. Calcular el máximo más alto y el mínimo más bajo durante las últimas *Count Bars* velas.
2. Cuando no hay posiciones abiertas:
   - Si la distancia desde el máximo reciente al precio actual supera *Limit* ticks, comprar a mercado.
   - Si la distancia desde el precio actual al mínimo reciente supera *Limit* ticks, vender a mercado.
   - Tras entrar en una posición, colocar cuatro órdenes límite adicionales cada *Grid* ticks, duplicando el volumen en cada nivel.
3. Cuando existe una posición:
   - Si el beneficio total supera *Amount* unidades de divisa, cerrar la posición y cancelar todas las órdenes pendientes.
   - Si *Lock Down* es mayor que cero, una vez que el precio se mueve a favor ese número de ticks, la estrategia activa una protección de punto de equilibrio. Si el precio regresa al nivel de entrada, se cierran todas las posiciones.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `CountBars` | Número de velas para buscar máximos y mínimos. |
| `Limit` | Distancia mínima desde el extremo en ticks para abrir una operación. |
| `Grid` | Distancia en ticks entre órdenes de cuadrícula adicionales. |
| `Amount` | Beneficio objetivo en divisa para cerrar todas las posiciones. |
| `LockDown` | Distancia en ticks para activar la protección de punto de equilibrio. |
| `CandleType` | Tipo de vela utilizado para los cálculos. |

La estrategia usa órdenes de mercado para entradas iniciales y órdenes límite para los niveles de la cuadrícula. Todos los comentarios en el código están escritos en inglés.
