# Estrategia de precio único Stop-Loss/Take-Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de utilidad replica el script MetaTrader "One Price SL TP" dentro de StockSharp. En lugar de abrir operaciones, el algoritmo observa la posición actual en el instrumento configurado y se asegura de que ambas órdenes de protección estén alineadas con un único precio objetivo especificado por el usuario.

Siempre que el parámetro **`ZenPrice`** esté por encima de cero, la estrategia lo compara con las cotizaciones de oferta/demanda en vivo:

- Para una posición **larga**: si `ZenPrice` es mayor que el precio solicitado, se coloca una orden límite de obtención de ganancias a ese precio; si `ZenPrice` es inferior a la oferta, en su lugar se registra una orden stop-loss.
- Para una posición **corta**: si `ZenPrice` es menor que la oferta, se convierte en la orden límite de obtención de ganancias; si `ZenPrice` es mayor que la demanda, se convierte en la orden stop-loss.

Cuando el precio cae entre la oferta y la demanda no se envía nada, por lo que la orden de protección anterior permanece intacta. Tan pronto como se cierra la posición o el parámetro se pone a cero, todas las órdenes de protección se cancelan automáticamente.

## como funciona

1. Suscríbase a los datos de Level1 para recibir cotizaciones de oferta y demanda actualizadas que se requieren para las verificaciones de dirección.
2. Realiza un seguimiento del volumen y la dirección de la posición de la estrategia actual. Se supone que las posiciones se crean manualmente o mediante otras estrategias.
3. En cada cotización, posición o actualización comercial personal, recalcula a qué lado del mercado pertenece el `ZenPrice` y crea el tipo de orden de protección correspondiente.
4. Normaliza el precio solicitado utilizando el paso del precio del instrumento y redondea el volumen de la orden a los límites del intercambio antes de enviar algo al conector comercial.
5. Utiliza `ReRegisterOrder` para modificar órdenes de protección ya activas en lugar de cancelarlas, coincidiendo con el comportamiento de la modificación local de MetaTrader.

## Parámetro

- **`ZenPrice`** – precio absoluto que debe usarse como nivel de límite de pérdidas o de obtención de ganancias. Establezca el valor en `0` para deshabilitar la automatización. Predeterminado: `0`.

## Notas practicas

- La estrategia nunca envía órdenes de entrada. Es seguro iniciarlo junto con terminales comerciales discrecionales u otras estrategias automatizadas.
- Las órdenes de protección se emiten solo después de que la primera instantánea de Level1 proporcione cotizaciones tanto de oferta como de demanda. Hasta entonces, el script espera, al igual que la versión original MQL se basaba en las comillas del terminal.
- Cuando solo un lado del mercado cumple la condición (por ejemplo, `ZenPrice` está por encima de la oferta pero no por debajo de la oferta), la otra orden de protección se cancela para evitar precios obsoletos.
- Todos los comentarios dentro del código están en inglés, mientras que esta documentación se proporciona en varios idiomas de acuerdo con las pautas del proyecto.

## Diferencias con el script MetaTrader

- El script original modifica los campos stop-loss y take-profit de un ticket de posición existente. StockSharp expone las órdenes de protección como órdenes de límite y stop explícitas, por lo que la conversión opera en órdenes visibles en el mercado.
- MetaTrader ajusta automáticamente el precio a la precisión del corredor. En este puerto, el mismo comportamiento se reproduce a través de `NormalizePrice`, que aprovecha el paso de precio del símbolo y la configuración decimal.
- El volumen de posición se redondea para intercambiar límites de lotes antes de enviar las órdenes de protección, lo que garantiza la compatibilidad con lugares que requieren pasos de lote específicos.
