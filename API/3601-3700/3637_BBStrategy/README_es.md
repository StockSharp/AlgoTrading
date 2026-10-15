# Estrategia BBStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

BBStrategy es un Bollinger sistema de ruptura de bandas convertido del MetaTrader asesor experto "BBStrategy". La estrategia rastrea dos conjuntos de Bollinger bandas con el mismo período pero diferentes multiplicadores de desviación. Cuando el precio atraviesa la banda exterior, el algoritmo arma una operación, pero la entrada real se pospone hasta que el precio regresa a la banda interior. Este comportamiento intenta evitar comprar rupturas demasiado extendidas o vender condiciones de sobreventa profunda y, al mismo tiempo, capturar el movimiento de continuación después de una expansión de la volatilidad.

## Lógica principal

1. Suscríbete a velas y calcula dos Bollinger Bandas:
   - **Banda exterior** utiliza un multiplicador de desviación configurable (predeterminado 3.0).
   - **La banda interior** utiliza una desviación más baja (predeterminada 2.0).
2. Detectar cuando el precio de cierre termina fuera de la banda exterior:
   - Por encima de la banda exterior superior hay una configuración larga.
   - Debajo de los brazos de la banda exterior inferior hay una configuración corta.
3. Ingrese solo si la siguiente vela completa se cierra nuevamente dentro de la banda interna en la dirección de la ruptura. Mientras el precio espera volver a entrar, la estrategia permanece en estado de "espera" en la dirección correspondiente.
4. Envíe una orden de mercado único cuando las condiciones se alineen y no haya posiciones abiertas ni órdenes activas. Las posiciones opuestas existentes se cierran aumentando el volumen de la orden de mercado.
5. Las distancias opcionales de toma de ganancias y límite de pérdidas (expresadas en puntos) se convierten en compensaciones de precios absolutos y se administran a través del asistente de protección incorporado.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| **Volumen de pedido** | Tamaño de la operación para cada posición. |
| **Bollinger Período** | Número de velas utilizadas para ambos cálculos de banda Bollinger. |
| **Desviación interna** | Multiplicador de desviación para la banda interior que valida los retrocesos. |
| **Desviación exterior** | Multiplicador de desviación para la banda exterior que detecta rupturas. |
| **Puntos de limitación de pérdidas** | Distancia de parada de protección en puntos (0 desactiva la parada). |
| **Puntos de obtención de beneficios** | Distancia de toma de ganancias en puntos (0 desactiva el objetivo). |
| **Tipo de vela** | Plazo de vela para los cálculos. |

## Notas

- La estrategia negocia una sola posición a la vez e ignora las nuevas señales mientras las órdenes están activas.
- Para la gestión de riesgos, el asistente convierte MetaTrader "puntos" en incrementos de precio reales según el tamaño del tick del instrumento.
- Los dibujos del gráfico incluyen velas, tanto Bollinger bandas como las operaciones propias de la estrategia para facilitar la depuración visual.
