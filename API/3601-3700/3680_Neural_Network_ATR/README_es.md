# Estrategia de red neuronal ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia replica el asesor experto "Neurotest" combinando un sistema neuronal ligero
capa de red con administración de dinero basada en ATR dentro de StockSharp. El modelo consume
última vela M15 completada y la transforma en cinco características normalizadas: cerca de
impulso de cierre, rango intradiario, cuerpo de vela, expansión de volumen y volatilidad (ATR a
relación de precios). Una única capa oculta con una salida sigmoidea produce una puntuación de probabilidad
que se escala mediante una tasa de aprendizaje dinámica. La puntuación se compara con la definida por el usuario.
Umbrales de compra y venta para abrir o invertir posiciones.

## Reglas de trading

1. Suscríbase a velas de 15 minutos (configurables) y calcule ATR del mismo período.
2. Construya las cinco características normalizadas de la vela anterior y la finalizada actual.
vela, luego evalúe la red neuronal.
3. Cuando la predicción ajustada está por encima del umbral de compra y la posición actual es
no largo, ingrese una operación larga (cerrando la exposición corta si es necesario).
4. Cuando la predicción ajustada está por debajo del umbral de venta y la posición actual es
no corto, ingrese una operación corta.
5. Cada entrada adjunta órdenes de stop-loss y take-profit basadas en ATR. Si ATR no está formado,
Se utiliza una distancia de retroceso en puntos.
6. Si el diferencial actual excede el límite configurado, la vela se ignora.

## Gestión del riesgo

- El tamaño de la posición se calcula a partir del capital de la cartera y la distancia de parada ATR para que el
la pérdida en el stop equivale a `Max Risk %` del capital.
- Las órdenes de protección utilizan un multiplicador de riesgo-recompensa configurable.
- El comercio se detiene automáticamente cuando la reducción diaria o total excede sus límites.
- Un sistema de penalización disminuye la tasa de aprendizaje en un 10 % (hasta un mínimo) cuando el nivel diario
no se alcanza el objetivo de beneficios, lo que amortigua las señales futuras hasta el siguiente día de negociación.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **% de riesgo máximo** | Riesgo máximo por operación como porcentaje del capital. |
| **% de pérdida diaria** | Umbral de reducción diario que deja de operar. |
| **% de pérdida total** | Umbral de reducción global que deja de cotizar. |
| **% de beneficio diario** | Objetivo de beneficio diario antes de saltarse la penalización. |
| **Tasa de aprendizaje** | Factor de escala aplicado a la salida neuronal. |
| **Capa oculta** | Número de neuronas en la capa oculta. |
| **Umbral de compra/umbral de venta** | Niveles de activación para entradas largas y cortas. |
| **Tipo de vela** | Tipo de vela y período de tiempo utilizado para las señales. |
| **ATR Período** | Período del indicador ATR. |
| **Difusión máxima** | Spread máximo permitido en pasos de precios. |
| **Recompensa por riesgo** | Multiplicador de toma de ganancias en relación con la distancia de parada. |
| **Parada de reserva** | Distancia de parada en puntos cuando ATR no está disponible. |

## Notas

- Se requiere suscripción a Level1 para monitorear el diferencial de oferta/demanda antes de cada decisión.
- Los pesos de la red neuronal se inicializan aleatoriamente pero son deterministas (semilla 42). el
La modulación de la tasa de aprendizaje emula el comportamiento adaptativo del experto original MQL.
- Asegúrese de que la cartera conectada proporcione `CurrentValue`, `StepPrice` y límites de volumen
para que el dimensionamiento de posiciones y las órdenes de protección funcionen correctamente.
