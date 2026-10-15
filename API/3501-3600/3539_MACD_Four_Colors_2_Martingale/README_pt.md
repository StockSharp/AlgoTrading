# MACD Quatro Cores 2 Martingale Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

# MACD Quatro Cores 2 Martingale

A estratégia transporta o consultor especialista "MACD Four Colors 2 Martingale" de MetaTrader para StockSharp. Ele mantém a lógica original construída em torno da interpretação de "cor" MACD e de um modelo de dimensionamento de posição martingale.

## Visão geral

O indicador subjacente pinta o histograma MACD com cinco cores. No "novo" esquema de cores padrão, o histograma muda de cor dependendo se a linha MACD sobe ou desce acima/abaixo da linha zero. O Expert Advisor abre uma posição sempre que as cores transitam de prata para amarelo (negativo MACD diminuindo novamente) ou de vermelho para azul (positivo MACD rolando). A versão StockSharp reproduz esta sequência reconstruindo as cores a partir de valores MACD.

Apenas uma cesta direcional de negociações está ativa a qualquer momento. Uma nova negociação só é permitida se o seu preço melhorar a entrada média da cesta atual (preço mais baixo para posições compradas, preço mais alto para posições vendidas). Cada nova entrada multiplica o último volume preenchido por um coeficiente de lote configurável, implementando a média martingale do EA original.

## Regras de negociação

- **Lógica do indicador**: um indicador `MovingAverageConvergenceDivergenceSignal` com a configuração clássica 26/12/9 gera valores MACD.
- **Reconstrução de cores**: a estratégia compara os dois valores MACD mais recentes. O negativo crescente MACD mapeia para a cor 1 (prata), o positivo crescente para a cor 2 (vermelho), o positivo decrescente para a cor 3 (azul) e o negativo descendente para a cor 4 (amarelo).
- **Entrada longa**: acionada quando as cores reconstruídas se movem de 1 para 4 enquanto o MACD na barra anterior permanece abaixo de zero. A negociação é executada apenas se não houver exposição curta e o novo preço for inferior a qualquer entrada longa existente.
- **Entrada curta**: acionada quando as cores passam de 2 para 3 enquanto o MACD na barra anterior permanece acima de zero. A negociação só dispara se não houver exposição longa e o novo preço for superior a qualquer entrada curta existente.
- **Gerenciamento de volume**: o primeiro pedido usa `InitialVolume`. Cada pedido subsequente dentro da mesma cesta multiplica o último volume executado por `LotCoefficient`. Definir o coeficiente ≤ 0 desativa o multiplicador.
- **Controle de lucros e perdas**: O PnL flutuante é avaliado em cada vela finalizada. Acertar `TargetProfit` fecha todas as posições e reinicia o ciclo martingale. A violação de `MaxDrawdown` (interpretada como um limite de perda) também fecha tudo e reinicia o ciclo. Limites negativos são suportados como no código original.
- **Saída de posição**: Além das metas monetárias, não há paradas automáticas. As posições permanecem abertas até que um limite de risco seja atingido ou o usuário intervenha manualmente.

## Parâmetros

- `CandleType` *(DataType, padrão 1h)* – período para o cálculo de MACD.
- `InitialVolume` *(decimal, padrão 1)* – volume do primeiro pedido em uma cesta.
- `LotCoefficient` *(decimal, padrão 2)* – multiplicador aplicado ao volume anterior quando o martingale está ativo.
- `MaxDrawdown` *(decimal, padrão 50)* – limite de perda flutuante (em dinheiro) que força a liquidação. Valores positivos observam `-MaxDrawdown`, valores negativos usam o valor exato.
- `TargetProfit` *(decimal, padrão 150)* – meta de lucro flutuante (em dinheiro) que fecha a cesta. Valores negativos invertem a comparação como na versão MQL.
- `FastEmaPeriod` *(int, padrão 12)* – comprimento do EMA rápido para MACD.
- `SlowEmaPeriod` *(int, padrão 26)* – duração da lentidão EMA para MACD.
- `SignalPeriod` *(int, padrão 9)* – comprimento do sinal EMA para suavização MACD.

## Notas de uso

- Funciona em qualquer instrumento que defina `PriceStep` e `StepPrice`, porque o PnL não realizado é calculado a partir das especificações da exchange.
- O dimensionamento do martingale pode aumentar a posição rapidamente. Valide os limites de risco antes de permitir a negociação em uma conta real.
- Para análise visual anexe a área do gráfico criada pela estratégia. Ele traça velas, o indicador MACD e as negociações executadas.

## Filtros de catálogo

- **Categoria**: Média de Tendência/Momentum
- **Direção**: Ambas (cestas longas e curtas)
- **Indicadores**: MACD
- **Paradas**: somente saída baseada em dinheiro
- **Prazo**: Configurável (padrão 1h)
- **Complexidade**: Intermediário
- **Risco**: Alto devido à escala de martingale
- **Automação**: Totalmente automatizado depois de iniciado
