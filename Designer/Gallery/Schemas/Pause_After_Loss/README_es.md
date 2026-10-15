# Diagrama de la estrategia de pausa tras una pérdida
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama de momentum sencillo incorpora una regla que decide cuándo está permitido operar. El bloque P&L change informa del resultado realizado de la cuenta, de modo que cada operación cerrada puede leerse como ganancia o pérdida sin medir precios. Las pérdidas que se suceden una tras otra se cuentan y, cuando el recuento alcanza su límite, se activa un bloque Flag que mantiene cerrada la negociación durante un número fijo de velas. Las entradas se reanudan solo cuando termina la cuenta atrás y se borra el flag.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas horarias terminadas alimentan un Rate of Change de un período, que es la variación porcentual del precio de cierre frente al cierre anterior, de modo que un único indicador soporta tanto el umbral de entrada como el de salida.
- Dos comparaciones contrastan ese porcentaje con dos variables: un umbral superior para un movimiento al alza y otro inferior, negativo, para un movimiento a la baja.
- El bloque Position se compara con cero tres veces —igual, mayor y menor—, lo que da una comprobación de posición plana para las entradas y una comprobación de largo y otra de corto para las salidas.
- Una entrada es un AND lógico de tres señales: momentum en la dirección buscada, posición plana y ninguna pausa en curso. Ambos bloques de entrada están configurados solo para abrir, por lo que el diagrama mantiene una única posición a la vez y nunca la incrementa.
- Una salida es un AND lógico de momentum en la dirección contraria y una posición en ese lado; dispara un bloque Position modify configurado para cerrar, que toma el volumen de lo que se mantiene abierto.
- El bloque P&L change informa del resultado realizado. Un bloque Previous value conserva la cifra que había antes del último cambio y dos comparaciones indican si el resultado bajó o subió, es decir, si la operación cerrada fue perdedora o ganadora.
- Una pérdida hace pasar la racha almacenada por una fórmula que suma uno y vuelve a escribir el total en la misma variable; una ganancia escribe cero sobre ella. La comparación del nuevo recuento con el límite es la señal que inicia una pausa.
- Esa señal activa un Flag y arma un bloque N values que cuenta velas terminadas. Mientras el flag está activo, una variable de estado almacenada que se lee en cada vela indica «en pausa», un NOT lógico la convierte en «permitido de nuevo», y esa es la tercera entrada de ambas puertas de entrada.

## Reglas de entrada y salida

- **Entrada en largo**: El Rate of Change de la vela terminada está por encima del umbral largo, la posición está plana y no hay ninguna pausa en curso. Position modify compra el volumen de la orden a mercado, solo en apertura.
- **Entrada en corto**: El Rate of Change de la vela terminada está por debajo del umbral corto, la posición está plana y no hay ninguna pausa en curso. Position modify vende el volumen de la orden a mercado, solo en apertura.
- **Salida**: La posición se abandona en cuanto el momentum se vuelve en su contra: un largo se cierra cuando el Rate of Change cae por debajo del umbral corto, y un corto cuando sube por encima del umbral largo. El bloque está configurado para cerrar posición, así que el volumen de la orden procede de la propia posición y el diagrama nunca gira de un lado al otro con una sola orden: el lado opuesto solo puede abrirse en una vela posterior, partiendo de posición plana. La pausa nunca retiene las salidas; solo afecta a las entradas.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 01:00:00 | Marco temporal de las velas sobre las que trabaja todo el diagrama. La pausa también se cuenta en estas velas, así que una vela más larga alarga la pausa en tiempo de reloj. |
| Rate Of Change Length | 1 | Cuántas velas atrás mide el Rate of Change. Con uno es la variación porcentual respecto al cierre anterior, que es contra lo que están escritos ambos umbrales; una longitud mayor lo convierte en una medida de momentum más amplia y los umbrales deben ampliarse con ella. |
| Long Threshold, % | 0.3 | Variación porcentual que abre un largo y cierra un corto. Aumentarla hace que ambos sean menos frecuentes y el diagrama más selectivo. |
| Short Threshold, % | -0.3 | Variación porcentual que abre un corto y cierra un largo, escrita como número negativo. No tiene por qué reflejar el umbral largo; valores asimétricos inclinan el diagrama hacia un lado. |
| Order Volume | 1 | Tamaño de cada orden de entrada, en unidades del instrumento. Las salidas toman su volumen de la posición, por lo que este valor no se repite allí. |
| Consecutive Losses | 3 | Cuántas operaciones cerradas seguidas deben ser perdedoras antes de que se suspenda la negociación. Una operación ganadora devuelve el recuento a cero, así que esto cuenta una racha y no un total; con uno, cada operación perdedora inicia una pausa. |
| Pause Candles | 8 | Cuántas velas terminadas dura una pausa. Las entradas se rechazan durante todo el recuento, tras el cual se borra el flag y el contador de pérdidas vuelve a cero. |

## Detalles del diagrama

- Nada del lado de la negociación alimenta la pausa: el contador de la racha y el flag se rigen únicamente por el resultado de la cuenta, de modo que el diagrama no tiene bucle y la pausa solo puede retirar el permiso, nunca concederlo.
- El bloque Flag emite solo en el momento en que se activa por primera vez, y el bloque N values ignora un disparo mientras ya está contando, así que una nueva señal de pérdida durante una pausa en curso ni reinicia ni prolonga la cuenta atrás.
- La cuenta atrás se mide en velas terminadas del marco temporal de trabajo, no en eventos de la cuenta, por lo que un tramo tranquilo y otro agitado producen una pausa de la misma duración.
- Las puertas de entrada leen la pausa desde una variable almacenada y no desde el propio flag. El flag informa de un instante; la variable guarda un estado: se escribe como verdadero cuando el flag se activa, como falso cuando termina la cuenta atrás, y se emite en cada vela, de modo que ambas puertas disponen siempre de un valor actualizado que combinar con las otras dos señales.
- El panel del gráfico dibuja las velas, el Rate of Change, el resultado realizado, las órdenes de entrada y salida y cada ejecución, así que un tramo en el que se alcanzaron señales pero no siguió ninguna orden se reconoce fácilmente como una pausa.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
