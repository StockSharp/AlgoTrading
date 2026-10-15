# Estrategia VR Smart Grid Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia VR Smart Grid Lite** replica la lógica del asesor experto MetaTrader con el mismo nombre. La estrategia construye una cuadrícula de promedio estilo martingala utilizando órdenes de mercado. El tamaño de la posición comienza a partir de un volumen base y se duplica cada vez que el precio se mueve contra la posición existente en una distancia definida por el usuario. La estrategia admite dos modos de salida: cerrar las operaciones extremas a un precio de obtención de beneficios ponderado o reducir parcialmente la exposición manteniendo la red activa.

## Parámetros
- **Take Profit (pips)** – distancia en pips utilizada para salir cuando solo hay una posición activa.
- **Volumen inicial**: volumen de orden inicial para la primera operación en cada dirección.
- **Volumen máximo**: límite máximo para cualquier pedido único abierto por la red.
- **Modo de cierre**: `Average` cierra las órdenes más antiguas y más nuevas en un objetivo ponderado; `PartClose` cierra parte del pedido más nuevo y todo el pedido más antiguo.
- **Paso de la orden (pips)**: distancia mínima del precio que se debe recorrer respecto de la posición antes de abrir una nueva operación.
- **Beneficio mínimo (pips)**: margen de beneficio adicional agregado al precio de salida promedio ponderado.
- **Deslizamiento (pips)**: parámetro de marcador de posición conservado del EA original para que esté completo.
- **Tipo de vela**: período de tiempo utilizado para impulsar la toma de decisiones (la vela completada anteriormente determina el sesgo comercial).

## Algoritmo
1. En cada vela terminada, la estrategia evalúa la dirección de la vela anterior.
2. Si la vela anterior cerró alcista y no existen operaciones largas o el precio bajó en el paso configurado, se coloca una nueva orden de **compra en el mercado**.
3. Si la vela anterior cerró bajista y no existen operaciones cortas o el precio subió en el paso configurado, se coloca una nueva orden de **mercado de venta**.
4. Los volúmenes se calculan a partir de la posición con el precio más bajo en la dirección y se duplican en cada nuevo nivel, respetando el volumen máximo y los niveles de volumen del corredor.
5. Cuando sólo queda una posición, la estrategia aplica la distancia de toma de ganancias simple y sale al contacto.
6. Con múltiples posiciones, la estrategia calcula promedios ponderados utilizando las entradas extremas:
   - **Modo promedio** cierra ambos extremos cuando el precio alcanza el objetivo ponderado más el margen de beneficio mínimo.
   - **Modo PartClose** cierra una parte del pedido más nuevo igual al volumen inicial y cierra completamente el pedido más antiguo, lo que permite que la cuadrícula siga funcionando con una exposición reducida.
7. Se realiza un seguimiento de todas las posiciones ocupadas y cerradas para mantener el estado de la red interna sincronizado con la cartera en vivo.

## Notas
- La estrategia se basa en órdenes de mercado, por lo que la calidad real de la ejecución y el deslizamiento dependen de las condiciones del corredor.
- Asegúrese de que las restricciones de volumen del instrumento (volumen mínimo y paso de volumen) sean compatibles con el volumen inicial seleccionado.
- Como ocurre con cualquier enfoque de cuadrícula o martingala, el riesgo puede crecer rápidamente cuando los mercados tienen una fuerte tendencia contraria a la posición; Utilice una gestión prudente del dinero.
