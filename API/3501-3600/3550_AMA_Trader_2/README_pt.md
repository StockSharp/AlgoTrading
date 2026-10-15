# Estratégia AMA Trader 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia AMA Trader 2 replica o fluxo de trabalho médio do especialista MetaTrader original de Vladimir Karputov. Ele combina um filtro de tendência Kaufman Adaptive Moving Average (AMA) com um bloco de confirmação do Índice de Força Relativa (RSI). Quando o preço fecha acima da AMA e o RSI mergulha em território de sobrevenda, a estratégia adiciona exposição longa; a regra simétrica se aplica a negociações curtas quando o preço fecha abaixo da AMA enquanto RSI imprime uma leitura de sobrecompra. As negociações médias são enviadas em tamanhos de lote fixos e podem ser restringidas por meio de parâmetros de risco, como contagem máxima de posições, espaçamento mínimo de entrada e trailing stops de proteção.

## Suposições de mercado
- **Instrumento**: Projetado para símbolos FX/CFD negociados com spreads reduzidos, mas aplicável a qualquer instrumento líquido onde a média seja aceitável.
- **Dados**: Opera em velas baseadas em tempo concluídas. O intervalo de tempo é configurável através do parâmetro `CandleType` (padrão: 1 minuto).
- **Sessões**: janela intradiária opcional. A negociação pode ser limitada a um horário de início/término em UTC com a sinalização `UseTimeWindow`.

## Indicadores
1. **Média Móvel Adaptativa Kaufman (AMA)** – detecta a tendência predominante com constantes de suavização rápida/lenta configuráveis e comprimento médio.
2. **Índice de Força Relativa (RSI)** – valida extremos de momento. O número de leituras RSI consecutivas que devem confirmar um sinal é controlado por `StepLength` (0 se comporta como 1, correspondendo à versão MQL).

## Lógica de negociação
1. Processe apenas velas finalizadas e certifique-se de que a estratégia esteja online e com permissão para negociação.
2. Aplique o filtro de tempo opcional; pular o processamento fora da janela intradiária quando ativado.
3. Atualize a fila de valores RSI recentes e calcule ajustes de trailing stop para a exposição existente.
4. **Configuração longa**: preço de fechamento acima da AMA e pelo menos um dos valores inspecionados de RSI abaixo de `RsiLevelDown`. Se a posição longa ativa estiver perdendo dinheiro, uma ordem média será colocada na fila antes da entrada padrão, imitando o comportamento de “recuperação de perdas” do consultor especialista. Os sinais curtos seguem a regra simétrica (`RsiLevelUp`).
5. As inscrições homenageiam `MaxPositions`, `MinStep` e `OnlyOnePosition`. Quando `CloseOpposite` está habilitado, a estratégia primeiro compensa o lado oposto e só considera novas entradas após a confirmação da negociação de achatamento.
6. Cada nova posição pode anexar distâncias fixas de stop-loss/take-profit e, opcionalmente, permitir um trailing stop baseado em lucro com ativação, distância e limites de passo.

## Gestão de risco
- **Tamanho de lote fixo**: Todas as entradas utilizam `LotSize`, permitindo o dimensionamento da posição via parâmetro ou portfólio de hospedagem.
- **Profundidade média máxima**: `MaxPositions` limita quantas vezes a exposição pode ser aumentada por direção.
- **Controle de espaçamento**: `MinStep` impõe uma distância mínima de preço entre entradas consecutivas, reduzindo o agrupamento no mesmo nível.
- **Saídas de proteção**: as lógicas opcionais de stop-loss, take-profit e trailing replicam o kit de ferramentas de proteção do especialista MetaTrader.
- **Exposição oposta**: `CloseOpposite` força a estratégia a fechar posições vendidas antes de abrir uma posição longa (e vice-versa). `OnlyOnePosition` garante que a estratégia nunca mantenha os dois lados simultaneamente.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados/período de vela usado para cálculos. |
| `LotSize` | Volume para cada ordem de mercado. |
| `RsiLength` | RSI período médio. |
| `StepLength` | Número de leituras recentes de RSI inspecionadas (0 → 1). |
| `RsiLevelUp` | RSI limite de sobrecompra para entradas curtas. |
| `RsiLevelDown` | RSI limite de sobrevenda para entradas longas. |
| `AmaLength` | Comprimento de suavização AMA. |
| `AmaFastPeriod` | Constante de suavização rápida para AMA. |
| `AmaSlowPeriod` | Constante de suavização lenta para AMA. |
| `StopLoss` | Distância de parada fixa em unidades de preço (0 desabilita). |
| `TakeProfit` | Distância alvo fixa em unidades de preço (0 desabilita). |
| `TrailingActivation` | Lucro necessário para armar o trailing stop (0 desabilita). |
| `TrailingDistance` | Distância mantida pelo trailing stop. |
| `TrailingStep` | Melhoria mínima antes que o trailing stop seja apertado. |
| `MaxPositions` | Entradas médias máximas por direção (0 desabilita). |
| `MinStep` | Distância mínima entre entradas consecutivas (0 desabilita). |
| `CloseOpposite` | Feche a exposição oposta antes de abrir uma negociação. |
| `OnlyOnePosition` | Bloqueie novas entradas sempre que existir alguma posição. |
| `UseTimeWindow` | Ative a filtragem de horário de início/término intradiário. |
| `StartTime` | Hora de início da sessão (UTC) quando a janela está habilitada. |
| `EndTime` | Hora de término da sessão (UTC) quando a janela está habilitada. |

## Notas de implementação
- Apenas API de alto nível: as velas são inscritas via `SubscribeCandles`, AMA e RSI são vinculadas a `.Bind` e todos os cálculos acontecem no retorno de chamada vinculado sem usar getters de indicadores proibidos.
- A contabilidade de posição reflete o especialista MQL: acumuladores separados rastreiam volumes/preços médios longos e curtos para avaliar o lucro não realizado para decisões de cálculo médio.
- Os trailing stops reconfiguram a distância de stop-loss no nível da estratégia em vez de manipular diretamente as filas de pedidos, mantendo a compatibilidade com o modelo de execução StockSharp.
- Os sinais são restritos a uma execução por barra de cada lado, reproduzindo a verificação MetaTrader que evita entradas duplicadas na mesma vela.

## Diferenças do especialista MetaTrader
- Parâmetros específicos de MetaTrader, como números mágicos, desvio, verificações de nível de congelamento e emulação de retirada do testador são omitidos. O ambiente StockSharp gerencia o deslizamento de pedidos e taxas internamente.
- Os preços stop/limit são calculados a partir do fechamento da vela, em vez dos ticks de compra/venda. Isso corresponde ao fluxo de trabalho baseado em velas de StockSharp.
- O EA original usa configurações de margem da conta para calcular tamanhos de lote dinâmicos. A porta mantém um `LotSize` fixo, deixando o dimensionamento baseado em risco para o ambiente de hospedagem.
