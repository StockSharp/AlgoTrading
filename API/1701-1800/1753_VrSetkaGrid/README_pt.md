# Estratégia de Grade VR Setka
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma implementação StockSharp do sistema de grade "VR---SETKAa3hM" do MetaTrader. Ela abre uma sequência de ordens de compra ou venda com base na variação percentual em relação ao intervalo diário e opcionalmente aumenta o volume usando um multiplicador de martingale. O preço médio de entrada de todas as ordens abertas é rastreado para colocar um alvo unificado de take-profit.

## Parâmetros
- `Distance`: Distância de preço em pontos entre os níveis da grade.
- `TakeProfit`: Meta de lucro em pontos para a ordem inicial.
- `Correction`: Lucro extra em pontos adicionado ao preço médio quando mais de uma ordem está aberta.
- `SignalPercent`: Limiar percentual usado para detectar desvio do intervalo diário.
- `UseMartingale`: Multiplicar o volume pelo número de ordens abertas.
- `CandleType`: Período de candle usado para cálculos de sinal.

## Lógica
1. Quando um candle finalizado aparece, o fechamento atual é calculado em relação à máxima e mínima do dia.
2. Se o candle anterior era de alta e o fechamento está suficientemente abaixo da máxima do dia, uma grade de compra é iniciada ou continuada.
3. Se o candle anterior era de baixa e o fechamento está suficientemente acima da mínima do dia, uma grade de venda é iniciada ou continuada.
4. Ordens adicionais são colocadas sempre que o preço se mover contra a posição em `Distance` pontos.
5. Uma vez que o preço retorna ao preço médio de entrada mais `Correction` para compras ou menos `Correction` para vendas, todas as posições são fechadas com uma ordem a mercado.
