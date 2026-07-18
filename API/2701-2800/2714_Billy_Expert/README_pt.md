# Billy Expert Comprador de Retração
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Billy Expert é uma estratégia de retração somente comprada convertida do Expert Advisor MetaTrader 5 «Billy expert». Ela aguarda uma sequência de máximos decrescentes e abre no período base, depois verifica confirmações de alta de dois osciladores Estocásticos calculados em diferentes períodos superiores. Quando ambos os osciladores concordam que há momentum de alta presente, o sistema adiciona uma nova posição comprada, até um limite configurável.

A conversão segue as diretrizes da API de alto nível do StockSharp. Volume de negociação, máximo de entradas simultâneas, stops protetores e take profits são controlados por parâmetros de estratégia para que o comportamento corresponda à lógica MQL original.

## Como funciona
1. Subscrever a série de velas primária (padrão 1 minuto) e dois períodos superiores para os osciladores Estocásticos (padrão 5 e 6 minutos).
2. Rastrear as últimas quatro velas concluídas no período base. Uma retração válida requer máximos *e* aberturas estritamente decrescentes ao longo dessas quatro barras.
3. Avaliar os osciladores Estocásticos rápido e lento. A estratégia exige que para cada oscilador tanto o último quanto o anterior valor de %K permaneça acima de %D, sinalizando que o momentum já inverteu para cima em ambos os períodos.
4. Se a retração e os filtros de momentum confirmarem e o número de operações compradas abertas estiver abaixo de `MaxPositions`, enviar uma ordem de compra a mercado com tamanho `TradeVolume`.
5. Os níveis opcionais de stop-loss e take-profit, expressos em pips, são convertidos em distâncias de preço absolutas usando o `PriceStep` do instrumento. Se qualquer distância for definida como zero, a ordem protetora correspondente é omitida.
6. As posições são fechadas apenas através dessas ordens protetoras, imitando o comportamento do expert advisor original.

## Parâmetros
- `TradeVolume` – tamanho da ordem para cada entrada (padrão `0.01`).
- `StopLossPips` – distância de stop em pips (padrão `0`, desabilitado).
- `TakeProfitPips` – alvo de lucro em pips (padrão `32`).
- `MaxPositions` – máximo de operações compradas simultâneas (padrão `6`).
- `Signal Candle` – período base usado para padrões de preço (padrão `1` minuto).
- `Fast Stochastic TF` – período para o oscilador rápido (padrão `5` minutos).
- `Slow Stochastic TF` – período para o oscilador lento (padrão `6` minutos). Deve ser mais longo que o período rápido.

## Filtros e comportamento
- **Direção**: Somente comprado.
- **Gatilho de entrada**: Retração de quatro barras com aberturas e máximos decrescentes.
- **Filtro de momentum**: Oscilador Estocástico duplo com %K acima de %D nas leituras atual e anterior.
- **Gestão de riscos**: Stop-loss e take-profit opcionais baseados em pips. Sem lógica de trailing.
- **Dimensionamento de posição**: `TradeVolume` fixo por entrada, limitado por `MaxPositions`.
- **Mercados**: Projetado para pares de forex cotados com pips fracionários, mas funciona com qualquer instrumento que forneça um `PriceStep` válido.

## Notas de uso
- Certifique-se de que `Fast Stochastic TF` seja estritamente mais curto que `Slow Stochastic TF`, caso contrário a estratégia para ao iniciar.
- Como as saídas dependem exclusivamente de ordens protetoras, ajuste `StopLossPips` e `TakeProfitPips` à volatilidade do instrumento.
- A estratégia ignora sinais de baixa e não reduz gradualmente; use controles de risco em nível de carteira para proteção adicional.
- Para backtesting, forneça velas de aquecimento suficientes para que ambos os osciladores Estocásticos possam se formar antes da primeira operação.
