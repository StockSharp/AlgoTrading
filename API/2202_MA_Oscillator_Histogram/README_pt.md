# Estratégia de Histograma do Oscilador MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma tradução do consultor especialista MQL5 **Exp_MAOscillatorHist.mq5**. Utiliza a diferença entre uma Média Móvel Simples (SMA) rápida e uma lenta para formar um oscilador. Os sinais de negociação são gerados quando o oscilador forma mínimos ou máximos locais, que são interpretados como potenciais reversões de tendência.

## Lógica de negociação
1. Duas SMAs são calculadas no período de velas selecionado:
   - **SMA rápida** com um período mais curto.
   - **SMA lenta** com um período mais longo.
2. O valor do oscilador é a SMA rápida menos a SMA lenta.
3. A estratégia acompanha os três últimos valores do oscilador. Um mínimo local ocorre quando o valor mais antigo é maior que o anterior e o anterior é menor que o atual. Um máximo local é o oposto.
4. Quando um mínimo local é detetado:
   - Fechar posições vendidas (se permitido).
   - Abrir uma nova posição comprada (se permitido).
5. Quando um máximo local é detetado:
   - Fechar posições compradas (se permitido).
   - Abrir uma nova posição vendida (se permitido).

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| **Fast Period** | Período da SMA rápida. |
| **Slow Period** | Período da SMA lenta. |
| **Enable Buy Open** | Se verdadeiro, posições compradas podem ser abertas. |
| **Enable Sell Open** | Se verdadeiro, posições vendidas podem ser abertas. |
| **Enable Buy Close** | Se verdadeiro, posições compradas podem ser fechadas em sinais opostos. |
| **Enable Sell Close** | Se verdadeiro, posições vendidas podem ser fechadas em sinais opostos. |
| **Candle Type** | Período das velas utilizadas para os cálculos. |

## Notas
- A estratégia utiliza a API de alto nível do StockSharp com `SubscribeCandles` e ligação de indicadores.
- `StartProtection` está ativado com ordens de mercado para execução mais segura.
- Nenhuma versão Python disponível.
