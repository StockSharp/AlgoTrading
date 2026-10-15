# Estratégia StellarLite TIC EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
StellarLite ICT EA é um algoritmo de estilo discricionário que traduz o manual da empresa de apoio "Stellar Lite" em StockSharp. A estratégia mescla dois modelos de entrada do Inner Circle Trader (ICT) – Silver Bullet e o modelo 2022 – e automatiza o plano de obtenção de lucro parcial usado no consultor especialista MetaTrader original. Ele funciona em qualquer instrumento que forneça informações sobre velas, etapas de preço e etapas de volume.

## Fluxo de trabalho principal
1. **Viés direcional do período de tempo mais alto** – uma média móvel no período de tempo mais alto selecionado deve inclinar-se na direção da negociação e o preço deve fechar além da média. Somente após a confirmação do viés é que a lógica do timeframe inferior será avaliada.
2. **Confirmação de varredura de liquidez** – a estratégia monitora uma janela de lookback configurável e procura quebras de máximos ou mínimos recentes. Silver Bullet requer uma varredura na direção comercial, enquanto o modelo 2022 requer uma varredura de incentivo na direção oposta.
3. **Mudança na Estrutura de Mercado (MSS)** – as últimas três velas concluídas devem confirmar uma mudança: um fechamento mais alto acima da máxima anterior para negociações longas ou um fechamento mais baixo abaixo da mínima anterior para negociações curtas.
4. **Detecção de Fair Value Gap (FVG)** – a estratégia verifica as dez velas mais recentes em busca de desequilíbrios de alta ou baixa criados por velas de deslocamento. A entrada só é permitida quando o fechamento atual estiver dentro do intervalo detectado.
5. **Filtro NDOG / NWOG** – a vela atual deve ser uma barra de faixa estreita. Seu intervalo alto-baixo não pode exceder `AtrThreshold` multiplicado pelo valor `AverageTrueRange`.
6. **Entrada, stop e metas** – o preço de entrada é colocado no meio do gap ou na retração OTE (Optimal Trade Entry) definida pelo parâmetro de proporção Fibonacci. O stop protetor está localizado além da liquidez de oscilação recente, e três níveis de take-profit são projetados usando os índices de risco-recompensa configurados.
7. **Gestão de negociação** – a posição é dimensionada de acordo com o percentual de risco selecionado ou depende do volume da estratégia. Quando TP1, TP2 e TP3 são atingidos, a estratégia fecha 50%, 25% e 25% da posição por padrão, move o stop para o ponto de equilíbrio após TP1 (com um deslocamento opcional), ativa um trailing stop após TP2 e liquida o restante em TP3 ou após um stop atingido.

## Parâmetros
- **Vela de entrada (`CandleType`)** – velas de período inferior usadas para sinais de entrada.
- **Período mais alto (`HigherTimeframeType`)** – velas alimentando a média móvel tendenciosa.
- **Período MA mais alto (`HigherMaPeriod`)** – comprimento médio móvel para detecção de viés.
- **ATR Período (`AtrPeriod`)** – lookback para o filtro de consolidação ATR.
- **Lookback de liquidez (`LiquidityLookback`)** – número de velas inspecionadas para localizar pools de liquidez.
- **ATR Limite (`AtrThreshold`)** – intervalo máximo de velas permitido como uma fração de ATR.
- **TP1/TP2/TP3 Risk Reward (`Tp1Ratio`, `Tp2Ratio`, `Tp3Ratio`)** – multiplicadores de risco-recompensa para metas.
- **% de fechamento de TP1/TP2/TP3 (`Tp1Percent`, `Tp2Percent`, `Tp3Percent`)** – porcentagens de fechamento parcial.
- **Break Even After TP1 (`MoveToBreakEven`)** – alterna o ajuste do ponto de equilíbrio.
- **Compensação do ponto de equilíbrio (`BreakEvenOffset`)** – número de etapas de preço adicionadas ou subtraídas ao mover o stop.
- **Trailing Distance (`TrailingDistance`)** – distância do trailing stop (em etapas de preço) ativada após TP2.
- **Use Silver Bullet / Use modelo 2022 (`UseSilverBullet`, `Use2022Model`)** – habilite ou desabilite cada configuração.
- **Use entrada OTE (`UseOteEntry`)** – calcule a entrada dentro da zona de entrada comercial ideal.
- **% de risco (`RiskPercent`)** – porcentagem do patrimônio arriscado por negociação para derivar o tamanho da posição.
- **OTE Inferior (`OteLowerLevel`)** – coeficiente Fibonacci para o nível OTE.

## Notas práticas
- A estratégia requer velas prontas; garantir que o feed de dados forneça preços próximos e etapas de volume.
- O dimensionamento da posição recorre ao parâmetro da estratégia `Volume` quando o valor do portfólio ou as informações do valor do tick não estão disponíveis.
- A detecção de liquidez e a lógica MSS dependem do cache histórico mais recente (20 velas por padrão); permitir que a estratégia colete dados suficientes antes de esperar sinais.
- As saídas parciais respeitam o passo de volume do instrumento; se a fração solicitada for menor que o volume mínimo negociável, o fechamento será ignorado.
- A lógica de trailing continua atualizando o stop apenas na direção do lucro e nunca afrouxa os controles de risco existentes.

## Arquivos
- `CS/StellarLiteIctEaStrategy.cs` – implementação da estratégia StockSharp.
- `README.md` – Documentação em inglês.
- `README_zh.md` – Documentação em chinês simplificado.
- `README_ru.md` – Documentação russa.
