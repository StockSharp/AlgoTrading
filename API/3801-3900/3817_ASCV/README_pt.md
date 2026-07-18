# Estratégia de ruptura do pivô ASCV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia ASCV Pivot Breakout é uma porta StockSharp de alto nível do MetaTrader 4 consultor especialista "ASCV" (arquivo `Avpb.mq4`). O robô original combina dois indicadores personalizados (ASCTrend1sig e BrainTrend1Sig), um filtro de desvio padrão, níveis de pivô diários e aceleração de volume intradiário para negociar configurações de continuação de breakout dentro de uma janela de negociação restrita. Como os indicadores personalizados proprietários não estão disponíveis em StockSharp, a conversão recria seu comportamento por meio de uma combinação de médias móveis, impulso estocástico e análise dinâmica diária, preservando as regras de gerenciamento de EA.

## Lógica de negociação

1. **Filtro de sessão** – as negociações são permitidas apenas entre os horários de início e término configurados (padrão 02h00–20h00 horário da corretora). As redefinições por hora reproduzem a lógica MQL que limpa sinalizadores de entrada sempre que `Minute()==0`.
2. **Porta de volatilidade** – um indicador de desvio padrão construído no período selecionado deve estar acima de um limite configurável. Isso reflete a chamada `iStdDev` original que exigia um mercado ativo antes que as entradas fossem consideradas.
3. **Confirmação de tendência** – uma média móvel simples rápida e lenta estima o filtro direcional fornecido pelo ASCTrend/BrainTrend. Um sinal longo exige que a média rápida esteja acima da lenta e que a vela feche acima do pivô diário. Shorts esperam a configuração oposta.
4. **Confirmação de impulso** – um oscilador estocástico garante que os rompimentos de alta ocorram com impulso positivo `%K-%D` e que as oportunidades de baixa tenham impulso negativo. O spread absoluto entre `%K` e `%D` é reutilizado como um gatilho de saída adaptativo, assim como o EA depende da diferença das linhas principais/sinal estocásticas.
5. **Aceleração de volume** – o volume da vela deve exceder o volume da vela anterior pelo delta configurado (padrão 30 contratos) para aproximar o filtro `Volume[0]-Volume[1]`.
6. **Colocação de ordens** – a estratégia utiliza ordens de mercado (`BuyMarket`/`SellMarket`) com volume fixo. Apenas uma negociação por direção é permitida por hora, de acordo com o consultor especialista.
7. **Paradas e alvos** – as paradas são colocadas no suporte/resistência do pivô mais próximo (S1/S2 ou R1/R2). Se esses níveis estiverem muito próximos, serão aplicadas distâncias de fallback expressas em etapas de preços. As metas de lucro seguem a mesma hierarquia: R2/R1/Pivot para posições compradas e S2/S1/Pivot para posições vendidas. Uma distância de fallback emula o comportamento EA quando os pivôs não estavam disponíveis.
8. **Gerenciamento dinâmico** – o spread estocástico impulsiona saídas antecipadas em caso de perda de impulso. Um trailing stop medido em etapas de preço reflete as modificações progressivas do stop loss da versão MQL.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Prazo para cálculos de indicadores e processamento de sinal. | Velas de 15 minutos |
| `StartHour` / `EndHour` | Limites horários inclusivos da sessão de negociação. | 20/02 |
| `FastMaLength` | Período do filtro de tendência rápida SMA. | 10 |
| `SlowMaLength` | Período do filtro de tendência lenta SMA. | 40 |
| `StdDevLength` | Comprimento de lookback do filtro de volatilidade de desvio padrão. | 10 |
| `StdDevThreshold` | Desvio padrão mínimo exigido para negociar. | 0,0005 |
| `VolumeDeltaThreshold` | Diferença mínima entre o volume da vela atual e anterior. | 30 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Períodos do oscilador estocástico. | 5/3/3 |
| `StochasticExitDelta` | Spread absoluto `%K-%D` que aciona saídas de impulso. | 5 |
| `TrailingStopSteps` | Distância do trailing stop em etapas de preço. | 30 |
| `MinPivotDistanceSteps` | Distância mínima (em passos) necessária para alvos baseados em pivôs. | 50 |
| `StopFallbackSteps` | Distância de parada quando nenhum suporte/resistência do pivô for suficiente. | 33 |
| `TakeProfitBufferSteps` | Fallback Take Profit Distance em etapas de preço. | 50 |
| `OrderVolume` | Volume para cada ordem de mercado. | 1 |

Todas as distâncias são definidas em etapas de preço do instrumento para garantir compatibilidade com as especificações da bolsa.

## Notas de implementação

- A estratégia usa o padrão `SubscribeCandles().BindEx(...)` de alto nível. Os indicadores **não** são adicionados a `Strategy.Indicators`, correspondendo à orientação de StockSharp.
- Os níveis de pivô são recalculados uma vez por dia de negociação usando a máxima, a mínima e o fechamento do dia anterior. O primeiro dia apenas coleta dados e começa a negociar assim que o segundo dia começa.
- `StartProtection()` está habilitado para proteger automaticamente contra desconexões inesperadas, replicando a rede de segurança do EA.
- Comentários XML e embutidos dentro do código C# explicam o mapeamento de cada bloco para a lógica MQL original.
- Os valores de stop loss e takeprofit são definidos por meio de `SetStopLoss`/`SetTakeProfit` usando conversões de etapas de preço para permanecer independente do corretor.

## Dicas de uso

1. Execute a estratégia em um instrumento que exponha os dados e o volume da vela porque o filtro de aceleração de volume é essencial.
2. Ao otimizar, concentre-se primeiro nos filtros de volatilidade (`StdDevThreshold`) e volume (`VolumeDeltaThreshold`) — o EA original era muito sensível a mercados calmos.
3. Ajuste as distâncias dos pivôs para corresponder ao perfil de volatilidade do símbolo negociado. Para instrumentos de tamanho de tick alto, aumente `MinPivotDistanceSteps` para evitar saídas prematuras.
4. Se o spread estocástico produzir muitas saídas, amplie `StochasticExitDelta` para que o trailing stop se torne a condição de saída dominante.

## Arquivos

- `CS/AscvStrategy.cs` – a implementação C# da estratégia.
- `README.md` – esta documentação.
- `README_ru.md` – Tradução russa.
- `README_zh.md` – tradução chinesa.
