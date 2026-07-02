# Estrategia del mapa rico de Kohonen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de mapas Rich Kohonen es una conversión del asesor experto MetaTrader 4 "Rich.mq4". El sistema original crea un mapa autoorganizado (red Kohonen) sobre vectores de características derivados de los cálculos de pivote de Tom DeMark y clasifica la siguiente barra como una oportunidad de compra, venta o retención. El puerto StockSharp conserva el enfoque de aprendizaje mientras se integra con la estrategia de alto nivel API, operando exclusivamente con velas completadas y órdenes de mercado.

## Datos de mercado
- **Instrumento**: configurado a través del `Security` vinculado en la aplicación host.
- **Tipo de vela** – parámetro `CandleType` (predeterminado: período de tiempo de 1 hora). La estrategia requiere al menos siete velas terminadas antes de producir señales para que se puedan ensamblar los vectores de características actuales y anteriores.

## Lógica comercial
1. Mantenga una ventana móvil de las últimas siete velas completadas.
2. Construya dos vectores de siete elementos en cada vela terminada:
   - El **vector actual** utiliza la apertura más reciente junto con las proyecciones pivote de Tom DeMark calculadas a partir de las cinco velas anteriores.
   - El **vector anterior** desplaza la ventana una barra y representa la barra que acaba de cerrar. Este vector se utiliza para entrenamiento.
3. Compare el vector actual con tres mapas de Kohonen (comprar, vender, mantener) y registrar la distancia euclidiana a cada unidad que mejor coincida.
4. Seleccione la acción con la distancia más pequeña y establezca la posición objetivo:
   - Comprar → exposición larga igual al volumen calculado.
   - Vender → exposición corta de la misma magnitud.
   - Mantener → sin posición.
La estrategia envía órdenes de mercado por la diferencia entre la posición actual y la objetivo para que la exposición final coincida con la decisión.
5. Calcule el movimiento de apertura a apertura (en pips) entre las dos últimas velas y entrene el mapa:
   - Movimiento positivo dentro de `[MinPips, MaxPips]` → agrega el vector anterior al mapa de compra.
   - Movimiento negativo dentro de `[-MaxPips, -MinPips]` → agrega el vector anterior al mapa de venta.
   - De lo contrario → almacene el vector en el mapa de espera.
6. El tamaño de la posición se determina dinámicamente a partir del saldo de la cartera: `floor(balance / 50) / 10`. Si esto produce cero, se utiliza el parámetro alternativo `Lots` en su lugar.

## Parámetros
- `MinPips`: límite inferior (en pips) para considerar un movimiento positivo de apertura a apertura como ejemplo de entrenamiento de compra.
- `MaxPips`: límite superior (en pips) para muestras de capacitación de compra/venta.
- `TakeProfit`, `StopLoss`: conservado del experto MQL con fines de documentación. La implementación de alto nivel cierra o revierte posiciones mediante órdenes de mercado en lugar de aplicar paradas.
- `Lots`: volumen de reserva que se aplica cuando la fórmula basada en el saldo arroja cero.
- `Slippage`: reservado para el ajuste manual de pedidos (no utilizado directamente por los ayudantes de alto nivel API).
- `MapPath`: ruta del archivo binario utilizada para conservar los tres mapas de Kohonen entre ejecuciones.
- `EAName`: comentario opcional almacenado como referencia.
- `CandleType`: suscripción de vela utilizada para la extracción de funciones.

## Almacenamiento de mapas persistente
La estrategia almacena el mapa entrenado en un archivo binario definido por `MapPath` (predeterminado `rl.bin` dentro del directorio de trabajo). El archivo contiene las matrices de compra, venta y retención de forma secuencial. Al inicio, las matrices se cargan y la estrategia cuenta las filas no vacías para reanudar el entrenamiento desde el estado anterior. Los archivos faltantes se ignoran, lo que hace que los mapas comiencen desde una memoria llena de ceros.

## Diferencias con el experto MQL original
- Las órdenes se emiten a través de StockSharp ayudantes (`BuyMarket` / `SellMarket`) y apuntan a la exposición final deseada en lugar de forzar un cierre completo y una reapertura en cada barra. Esto mantiene el comportamiento efectivo y al mismo tiempo reduce las transacciones duplicadas en el entorno administrado.
- Los niveles de stop-loss y take-profit permanecen como parámetros para la documentación, pero no se registran como órdenes separadas. Las salidas de posición ocurren cuando el clasificador selecciona el lado opuesto o la acción de retención.
- El manejo de archivos utiliza ayudantes de E/S .NET; el formato del mapa sigue siendo compatible (valores de doble precisión ordenados de forma idéntica).

## Notas de uso
- Asegúrese de que el valor seleccionado exponga un `PriceStep` válido para que las diferencias de pips se calculen correctamente. Si el paso falta o es cero, la estrategia vuelve a un paso unitario.
- Los mapas de Kohonen pueden crecer (hasta 10.000 entradas de compra/venta y 25.000 entradas de retención). Mantenga la ruta predeterminada en un dispositivo de almacenamiento con capacidad suficiente (~2,5 MB cuando esté lleno).
- Debido a que el algoritmo se entrena continuamente, ejecutar la estrategia con datos históricos antes de la implementación en vivo ayuda a completar el mapa con muestras representativas.
