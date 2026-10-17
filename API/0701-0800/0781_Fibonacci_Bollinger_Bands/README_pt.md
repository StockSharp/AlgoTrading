# Estratégia Fibonacci e Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Reversão à média apenas comprada, com bandas Bollinger e extensão Fibonacci abaixo do intervalo anterior.

Padrões: `BollingerPeriod = 20`, `BollingerDeviation = 2`, `FibLookback = 50`, `FibonacciLevel = 0.618`, `CandleType = 1 minuto`. Apenas velas concluídas. As bandas usam os últimos 20 fechamentos, incluindo a vela de sinal, e desvio padrão populacional. O intervalo usa máximo High e mínimo Low das 50 velas anteriores, excluindo a vela de sinal.

Extensão inferior: `F = previousLow − FibonacciLevel × (previousHigh − previousLow)`. Aceita coeficientes positivos acima de 1, como 1.618. Sem posição, compra estritamente com `Close < lowerBand` e `Close > F`. Fecha toda a compra com `Close >= upperBand` ou `Close >= previousHigh`. Igualdade com F não gera entrada. Sem vendas a descoberto, ordens stop ou velas futuras.
