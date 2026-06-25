# Estratégia ADX Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia ADX Expert** é uma conversão direta do expert advisor original do MetaTrader 4 "ADX Expert" (script MQL 20315). O expert procura cruzamentos entre as linhas do Directional Index positivo e negativo (+DI e -DI) enquanto o Average Directional Index (ADX) permanece abaixo de um limiar especificado, indicando que o mercado está em consolidação. Apenas uma posição pode estar aberta por vez, assim como no expert original.

## Lógica de trading
1. A estratégia se inscreve na série de candles selecionada (candles de 15 minutos por padrão) e calcula o Average Directional Index com o período configurado.
2. Uma ordem de compra é colocada quando:
   - A linha +DI cruza acima da linha -DI.
   - O valor do ADX permanece abaixo do limiar definido (padrão 20), sinalizando uma tendência fraca.
   - O spread atual está abaixo do filtro `MaxSpreadPoints`.
   - Nenhuma posição está aberta atualmente.
3. Uma ordem de venda é colocada quando:
   - A linha +DI cruza abaixo da linha -DI.
   - O valor do ADX ainda é menor que o limiar permitido.
   - O requisito de spread e a condição de posição flat são satisfeitos.
4. Níveis de stop-loss e take-profit protetores são atribuídos através de `StartProtection`, espelhando o stop fixo e o alvo da versão MQL. Eles são expressos em pontos de preço (passos de preço) e podem ser desabilitados definindo os valores como zero.

A estratégia depende de um fluxo de trabalho de posição única: novos sinais são ignorados até que a posição atual seja fechada por suas ordens protetoras.

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho de ordem usado para cada ordem de mercado. | 0.1 |
| `AdxPeriod` | Período para o cálculo do ADX. | 14 |
| `AdxThreshold` | Valor máximo do ADX que ainda permite uma operação. | 20 |
| `MaxSpreadPoints` | Spread máximo permitido em pontos de preço. Definir como 0 para desabilitar o filtro. | 20 |
| `StopLossPoints` | Distância de stop-loss em pontos de preço. | 200 |
| `TakeProfitPoints` | Distância de take-profit em pontos de preço. | 400 |
| `CandleType` | Tipo de candle para cálculos de indicadores (candles de 15 minutos por padrão). | Período de 15 minutos |

## Notas adicionais
- O filtro de spread requer atualizações do livro de ordens para ler os melhores preços de bid e ask. Certifique-se de que seu provedor de dados fornece essas informações.
- Todos os comentários e registros são escritos em inglês para maior clareza, cumprindo com as diretrizes do repositório.
- A estratégia é destinada a fins educacionais. Teste-a completamente em um ambiente simulado antes de implantá-la no trading ao vivo.
