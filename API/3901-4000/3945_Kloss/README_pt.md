# Estratégia de Kloss MQL/8186
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Kloss MQL/8186** é uma conversão direta do MetaTrader 4 consultor especialista `Kloss.mq4`. Ele combina um índice de canal de commodities (CCI), um oscilador Stochastic e um filtro de preço típico deslocado para cronometrar reversões de posição única. A versão StockSharp mantém os limites de entrada originais, distâncias de stop-loss e take-profit e lógica de volume (tamanho de lote fixo ou dimensionamento baseado em porcentagem) enquanto usa a assinatura de vela de alto nível API.

## Lógica de negociação

- **Dados**: Candles concluídas do período configurado (padrão 5 minutos). Os indicadores são calculados na mesma série.
- **Indicadores**:
  - CCI com período 10. O valor absoluto é comparado com `±CciThreshold` (padrão 120).
  - Oscilador Stochastic com `%K=5`, `%D=3`, suavização `=3`. A linha principal `%K` é verificada em relação às faixas de sobrevenda/sobrecompra.
  - Preço típico ((máximo + mínimo + fechamento) / 3) atrasado por cinco velas concluídas para replicar o LWMA alterado do consultor especialista.
- **Entrada longa**:
  - CCI <= `-CciThreshold`.
  - Stochastic %K <`StochasticOversold` (padrão 30).
  - Abertura da vela anterior > preço típico de cinco velas atrás.
  - Nenhuma posição longa existente (`Position <= 0`). Qualquer posição curta aberta é fechada e revertida em posição longa em uma única ordem de mercado.
- **Entrada curta**:
  - CCI >= `CciThreshold`.
  - Stochastic %K > `StochasticOverbought` (padrão 70).
  - Fechamento da vela anterior <preço típico de cinco velas atrás.
  - Nenhuma posição curta existente (`Position >= 0`). Qualquer posição comprada aberta é fechada e revertida em posição vendida com uma ordem de mercado.
- **Gerenciamento de posição**: StockSharp de `StartProtection` emite ordens de stop-loss e take-profit automaticamente usando as distâncias de pontos especificadas. Caso contrário, a estratégia mantém sempre uma única posição (plana, longa ou curta).

## Dimensionamento de posições

- **Volume Fixo**: Se `FixedVolume > 0`, a estratégia sempre negocia nesse volume exato (após alinhar com `VolumeStep` e `MinVolume` do instrumento).
- **Porcentagem de risco**: quando `FixedVolume = 0`, a estratégia aloca `RiskPercent` (padrão 0,2) do valor da conta dividido pelo último tamanho próximo ao pedido estimado. O volume é limitado por `MaxVolume` (padrão 5) e arredondado para o passo do instrumento.
- **Salvaguardas**: O método volta ao volume mínimo negociável se faltarem informações da conta ou se o valor calculado não for positivo.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CciPeriod` | Número de velas usadas para calcular o Commodity Channel Index. | 10 |
| `CciThreshold` | Nível CCI absoluto que aciona entradas. | 120 |
| `StochasticKPeriod` | Período %K do oscilador Stochastic. | 5 |
| `StochasticDPeriod` | Período de suavização %D. | 3 |
| `StochasticSmooth` | Suavização adicional aplicada a %K antes do sinal. | 3 |
| `StochasticOversold` | Limite de %K para confirmar entradas longas. | 30 |
| `StochasticOverbought` | Limite de %K para confirmar entradas curtas. | 70 |
| `StopLossPoints` | Distância em faixas de preço para o stop protetor. | 48 |
| `TakeProfitPoints` | Distância em faixas de preço para a meta de lucro. | 152 |
| `FixedVolume` | O valor positivo força um volume de negociação fixo. | 0 |
| `RiskPercent` | Fração da carteira convertida em volume quando `FixedVolume` é zero. | 0,2 |
| `MaxVolume` | Volume máximo de negociação permitido. | 5 |
| `CandleType` | Tipo/prazo de vela para cálculos de indicadores. | Período de 5 minutos |

## Notas de Execução

- **Posição Única**: Apenas uma posição é mantida aberta. As reversões fecham a posição existente e abrem a nova com uma única ordem de mercado.
- **Sincronização de Indicadores**: A mudança de preço utiliza as últimas cinco velas concluídas; pelo menos seis velas devem ser processadas antes que a primeira negociação possa aparecer.
- **Paradas/Metas**: `StartProtection` converte distâncias baseadas em pontos em compensações de preços absolutos usando o `PriceStep` do instrumento. Se `PriceStep` for desconhecido, o valor do ponto bruto será aplicado.
- **Requisitos de dados**: Funciona com qualquer instrumento que forneça OHLC velas; o alinhamento de volume respeita `MinVolume` e `VolumeStep` quando disponível.
- **Diferenças em relação ao MT4**: MetaTrader os cálculos de margem são aproximados por meio do patrimônio da conta (`Portfolio.CurrentValue`). Quando os dados sobre ações não estão disponíveis, a estratégia reverte para o volume negociável mínimo.

## Dicas de uso

1. Ajuste `CandleType` para a sessão de mercado usada em MetaTrader (M5 no modelo original).
2. Revise as distâncias de parada em relação ao tamanho do tick; a conversão ponto-a-preço acontece automaticamente, mas os valores podem precisar de ajuste para instrumentos não forex.
3. Para tamanhos de contrato fixos, defina `FixedVolume` como o lote desejado e `RiskPercent` como zero.
4. Habilite a otimização dos limites do indicador ao calibrar a estratégia em novos símbolos.
