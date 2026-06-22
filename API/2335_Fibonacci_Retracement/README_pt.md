# Estratégia de Retração de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos de níveis de retração de Fibonacci derivados de pivôs ZigZag.

## Ideia

1. Detectar máximos e mínimos de swing usando uma abordagem ZigZag.
2. Construir níveis de retração de Fibonacci (23.6%, 38.2%, 61.8%, 76.4%) entre os dois últimos pivôs.
3. Em uma tendência de alta, a estratégia compra quando o preço fecha acima de qualquer nível de Fibonacci.
4. Em uma tendência de baixa, a estratégia vende quando o preço fecha abaixo de qualquer nível de Fibonacci.
5. Cada ordem é protegida com um stop-loss fixo e um take-profit baseado no intervalo do swing.
6. Após o fechamento de uma posição, a estratégia aguarda um número de barras antes de negociar novamente.

## Parâmetros

- `ZigzagDepth` – profundidade usada para pesquisar novos pivôs.
- `SafetyBuffer` – distância em pontos que o preço deve mover além do nível.
- `TrendPrecision` – diferença mínima entre pivôs para detectar a direção da tendência.
- `CloseBarPause` – número de barras a aguardar após fechar uma operação.
- `TakeProfitFactor` – fração do intervalo do swing usada como extensão do take-profit.
- `StopLossPoints` – distância do stop-loss a partir do preço de entrada em pontos.
- `CandleType` – tipo de vela usado para cálculos.

## Notas

Este arquivo contém apenas a implementação em C#. Uma versão em Python ainda não foi fornecida.
