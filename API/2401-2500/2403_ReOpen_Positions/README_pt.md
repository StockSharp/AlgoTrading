# Estratégia de Reabertura de Posições
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do exemplo MQL5 `Exp_ReOpenPositions`. Demonstra como reabrir posições quando a negociação atual se torna lucrativa.

## Lógica

1. A estratégia abre uma posição comprada inicial no início.
2. Quando o preço avança `ProfitThreshold` pontos a partir do último preço de entrada, abre outra posição comprada.
3. Cada nova entrada atualiza os níveis de stop loss e take profit relativos ao seu próprio preço.
4. Se o preço atingir o stop loss ou o take profit, todas as posições são fechadas e o ciclo é reiniciado.

As mesmas regras se aplicam a negociações vendidas se a primeira posição for vendida.

## Parâmetros

- `ProfitThreshold` – movimento de preço em pontos necessário para adicionar uma nova posição.
- `MaxPositions` – número máximo de posições abertas.
- `StopLossPoints` – distância da entrada até o stop de proteção.
- `TakeProfitPoints` – distância da entrada até o lucro alvo.
- `CandleType` – tipo de dados de velas para processamento.

## Notas

O exemplo é simplificado para fins educacionais e não gerencia volume de negociação ou gestão de dinheiro como no script original.
