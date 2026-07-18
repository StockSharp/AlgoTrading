# Estratégia de Rompimento dos Níveis da Vela Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o consultor especialista MetaTrader "Previous Candle Breakdown". Ela aguarda o preço romper acima ou abaixo da vela de referência anterior com um recuo configurável medido em passos de preço. A implementação depende das APIs de alto nível do StockSharp com assinaturas de velas para cálculos de níveis e assinaturas de ticks para decisões de execução.

## Lógica de negociação
1. Ao fechamento de cada vela de referência (padrão de 4 horas), a estratégia armazena o máximo e mínimo da vela anterior e os desloca por `IndentSteps * Security.PriceStep` para construir níveis de rompimento.
2. Os preços de tick (últimas negociações) são monitorados. Uma entrada comprada é acionada quando o preço atinge o nível superior e uma entrada vendida quando o preço cai pelo nível inferior.
3. Um filtro de média móvel opcional requer que a MA rápida (com deslocamento opcional para frente) permaneça acima da MA lenta para negociações compradas e abaixo para vendidas. Definir qualquer período de MA como zero desabilita o filtro.
4. As negociações são permitidas apenas dentro da janela de sessão configurada entre `StartTime` e `EndTime`. Sessões que cruzam meia-noite são suportadas.
5. O lucro flutuante é monitorado continuamente: stops, alvos e regras de trailing fecham posições existentes antes que um sinal de rompimento possa acionar reversões.

## Gestão de risco
- **StopLossSteps / TakeProfitSteps** — distâncias em passos de preço do preço de entrada. Os passos são convertidos via `distance = steps * Security.PriceStep`.
- **TrailingStopSteps / TrailingStepSteps** — habilita uma saída de trailing depois que a posição se move a favor em pelo menos a distância de trailing. O stop é movido mais somente quando o lucro avança pelo passo de trailing.
- **ProfitClose** — fecha todas as posições assim que o lucro não realizado (`Position * (último preço - PositionPrice)`) excede o limiar. Definir como `0` para desabilitar.
- **MaxNetPosition** — limita a posição líquida absoluta para que a estratégia não possa fazer pirâmide além dessa quantidade. O tamanho da posição em si é controlado pela propriedade `Volume` da estratégia.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Período de referência usado para calcular os níveis de rompimento. |
| `IndentSteps` | Deslocamento acima/abaixo do máximo/mínimo da vela anterior expresso em passos de preço. |
| `FastMaPeriod` / `FastMaShift` | Comprimento da MA rápida e deslocamento opcional para frente (barras). |
| `SlowMaPeriod` / `SlowMaShift` | Comprimento da MA lenta e deslocamento opcional para frente (barras). |
| `StopLossSteps` | Distância de stop loss em passos de preço. |
| `TakeProfitSteps` | Distância de take profit em passos de preço. |
| `TrailingStopSteps` | Distância do trailing stop (0 desabilita o trailing). |
| `TrailingStepSteps` | Ganho mínimo necessário antes que o trailing stop avance. Deve ser > 0 quando o trailing estiver em uso. |
| `ProfitClose` | Alvo de lucro flutuante que fecha todas as posições. |
| `MaxNetPosition` | Posição líquida absoluta máxima permitida. |
| `StartTime` / `EndTime` | Limites da janela de negociação. |

## Notas de uso
- Defina a propriedade `Volume` da instância de estratégia para controlar o tamanho da ordem. O dimensionamento de posição baseado em risco da versão MetaTrader não está portado intencionalmente.
- As médias móveis usam médias móveis simples (`SMA`). Se outros modos de suavização forem necessários, estenda a estratégia adequadamente.
- O limiar de fechamento por lucro usa o PnL não realizado calculado a partir do preço médio de posição em vez da moeda da conta, porque dados específicos de swap/comissão do broker não estão disponíveis diretamente.
- A estratégia opera em um ambiente de netting; as negociações de reversão enviam ordens de mercado na direção oposta, fechando automaticamente a exposição atual primeiro.
- O trailing stop requer um valor positivo de `TrailingStepSteps`; caso contrário, a estratégia lança uma exceção durante a inicialização.

## Diferenças da versão MQL original
- O gerenciamento de dinheiro baseado em lotes fixos ou porcentagem de risco não está implementado; os usuários do StockSharp devem gerenciar o tamanho através da propriedade `Volume` ou gestores de portfólio externos.
- Apenas médias móveis simples são suportadas; o original permitia diferentes tipos de MA.
- A lógica de fechamento por lucro usa o PnL flutuante calculado a partir do preço médio de posição em vez da moeda da conta, porque dados específicos de swap/comissão do broker não estão diretamente disponíveis.
- O log é tratado pelo StockSharp; mensagens detalhadas de resultado de negociação do MetaTrader são omitidas.
