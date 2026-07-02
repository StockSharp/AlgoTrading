# Estrategia de cuadrícula ganadora de Waddah Attar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Waddah Attar Win Grid** replica el asesor experto MetaTrader 4 del script `MQL/8210`. Mantiene continuamente una escalera simétrica de órdenes límite de compra y venta en torno a la oferta/demanda actual. Cuando el precio se desplaza hacia el nivel de cuadrícula más reciente, la estrategia automáticamente acumula una nueva orden pendiente un paso más lejos, aumentando opcionalmente el volumen de cada orden adicional. El beneficio flotante se supervisa en cada actualización del libro de órdenes y, una vez que se alcanza la ganancia de capital configurada, todas las posiciones y órdenes de trabajo se cierran simultáneamente.

## como funciona

1. **Inicialización**
   - Suscríbase a las actualizaciones del libro de pedidos para reaccionar instantáneamente a los cambios de oferta/demanda.
   - Registra el valor actual de la cartera para utilizarlo como referencia de capital de referencia.
   - Inicia el subsistema de protección de riesgos integrado de StockSharp.
2. **Gestión de línea base**
   - Siempre que no haya órdenes activas y la posición neta sea plana, el último valor de la cartera se convierte en el nuevo saldo de referencia. Esto refleja el asesor experto original, que almacenaba el saldo de la cuenta corriente en cada tick.
3. **Ubicación inicial de la cuadrícula**
   - Tan pronto como se permite el comercio y no hay órdenes activas, la estrategia coloca dos órdenes pendientes:
     - Un límite de compra `Step Points` por debajo del precio de venta actual.
     - Un límite de venta `Step Points` por encima del precio de oferta actual.
   - Ambos pedidos utilizan el valor `First Volume`.
4. **Acumulación de nuevos pedidos**
   - Cuando el precio de venta se mueve dentro de cinco pasos de precio del último límite de compra, la estrategia coloca un nuevo límite de compra un paso completo por debajo del nivel anterior.
   - Cuando el precio de oferta se mueve dentro de cinco pasos de precio del último límite de venta, la estrategia coloca un nuevo límite de venta un paso completo por encima del nivel anterior.
   - Cada nueva orden pendiente aumenta el volumen en `Increment Volume`, lo que permite realizar una pirámide estilo martingala si lo desea.
5. **Captura de beneficios**
   - El beneficio flotante se calcula como la diferencia entre el capital actual de la cartera y el saldo de referencia almacenado.
   - Una vez que esta ganancia excede `Min Profit`, cada orden activa se cancela y todas las posiciones abiertas se nivelan con una sola llamada `CloseAll`.
   - El valor de referencia se actualiza, lo que permite que la red se reinicie desde cero.

## Características de la estrategia

- **Datos de mercado**: opera exclusivamente en instantáneas de la cartera de pedidos de nivel 1 (mejor oferta/demanda).
- **Tipos de órdenes**: utiliza solo órdenes limitadas; no se generan paradas ni entradas de mercado automáticamente.
- **Exposición**: puede mantener posiciones largas y cortas simultáneas en carteras habilitadas para cobertura.
- **Control de riesgos**: carece de límites de pérdidas estrictos; se basa en el objetivo de beneficios flotantes y en normas de riesgo externas.
- **Reingreso**: después de aplanar o cancelar manualmente las órdenes, la cuadrícula inicial se recrea automáticamente la próxima vez que se ejecuta el ciclo de datos del mercado.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Step Points` | `120` | Distancia entre niveles de cuadrícula consecutivos, expresada en puntos de precio (múltiplos de pasos de precio). |
| `First Volume` | `0.1` | Volumen utilizado para el primer par de órdenes pendientes. |
| `Increment Volume` | `0.0` | Volumen adicional agregado a cada pedido recién acumulado; establezca en cero para mantener todos los pedidos del mismo tamaño. |
| `Min Profit` | `450` | Se requiere beneficio flotante (en la moneda de la cuenta) para cerrar todas las posiciones abiertas y órdenes pendientes. |

## Notas y limitaciones

- Asegúrese de que el `PriceStep` del instrumento esté configurado correctamente; la estrategia multiplica `Step Points` por `PriceStep` para obtener precios reales.
- Debido a que el algoritmo cancela y reemplaza órdenes con frecuencia, se deben considerar los límites del corredor o de la bolsa en el recuento de órdenes pendientes.
- No existe una protección contra caídas incorporada; considere combinar la estrategia con gestión de riesgos externa o paradas a nivel de cartera.
- La red puede expandirse indefinidamente si los precios evolucionan bruscamente sin alcanzar el objetivo de ganancias; elija `Increment Volume` cuidadosamente para controlar el uso del margen.

## Archivos

- `CS/WaddahAttarWinGridStrategy.cs` — Implementación en C# de la lógica comercial.
- `README.md` — esta documentación (inglés).
- `README_ru.md` — Traducción al ruso con contenido idéntico.
- `README_zh.md` — Traducción al chino con contenido idéntico.
