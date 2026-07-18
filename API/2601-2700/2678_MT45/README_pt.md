# Estratégia MT45
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MT45 é uma conversão direta do consultor especializado original do MetaTrader. Ela alterna entre posições compradas e vendidas de mercado em cada barra completada, enquanto protege cada operação com as mesmas distâncias fixas de take-profit e stop-loss usadas na implementação MQL. O dimensionamento de posição segue uma regra de recuperação estilo martingale, de modo que a próxima operação aumenta seu volume somente após um resultado perdedor.

## Lógica de trading
1. A estratégia se inscreve em uma única série de velas definida pelo parâmetro **Candle Type** e aguarda velas completadas para evitar ruído intrabar.
2. Quando nenhuma posição está aberta e a ordem de entrada anterior foi completamente processada, o algoritmo envia uma ordem a mercado na direção programada para este turno (compra, depois venda, depois compra, ...).
3. A direção alterna somente após a ordem correspondente ser executada, garantindo que a alternância coincida com o comportamento do especialista MQL onde cada operação completada muda o lado para o próximo sinal.
4. As ordens protetoras de stop-loss e take-profit são gerenciadas automaticamente através de `StartProtection`, de modo que a estratégia sai do mercado quando qualquer uma das distâncias é alcançada.

## Dimensionamento de posição
* **Base Volume** define o tamanho de lote inicial. É restaurado após cada operação lucrativa ou de equilíbrio.
* Após uma operação perdedora, o volume da próxima entrada é multiplicado pelo **Martingale Multiplier**. Se o valor escalado excedesse **Max Volume**, a estratégia volta ao volume base para evitar crescimento descontrolado.
* O lucro ou perda realizado é medido comparando o preço de saída com o preço de entrada armazenado, o que reproduz a função `Lot()` do consultor especializado original.

## Gestão de risco
* **Stop Points** e **Take Points** são expressos em passos de preço, espelhando o multiplicador `_Point` que era usado no MetaTrader. A estratégia converte esses valores em distâncias de preço absolutas via `PriceStep` do instrumento antes de habilitar `StartProtection`.
* As ordens protetoras são anexadas automaticamente a cada posição e são colocadas simetricamente tanto para operações compradas quanto vendidas.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| Stop Points | Distância ao stop protetor em passos de preço do instrumento. | 600 |
| Take Points | Distância ao alvo de take-profit em passos de preço do instrumento. | 700 |
| Base Volume | Volume base usado para novas posições após ganhos. | 0.01 |
| Martingale Multiplier | Multiplicador de volume aplicado após perdas. | 2 |
| Max Volume | Volume máximo permitido para o escalonamento martingale. | 10 |
| Candle Type | Série de velas usada para detectar a conclusão de barras (padrão: 1 minuto). | 1 minuto |

## Notas de uso
* Escolha o período de velas que corresponda ao período do gráfico do especialista original. A lógica opera estritamente em velas completadas.
* A estratégia não enfileira outra entrada enquanto houver uma ordem pendente ou uma posição ativa; sempre aguarda a operação existente ser fechada por stop-loss ou take-profit.
* Não há uma versão Python separada para esta estratégia no momento, seguindo as diretrizes do projeto.
