# Estratégia BreakRevert Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

BreakRevert Pro é a conversão StockSharp do consultor especialista MetaTrader 5 *BreakRevertPro.mq5*. A estratégia combina a confirmação do rompimento no período de um minuto com uma tendência mais ampla e um contexto de volatilidade dos gráficos de 15 minutos e de uma hora. As pontuações de estilo de probabilidade são reproduzidas por meio de aproximações baseadas em indicadores para que o comportamento permaneça próximo do EA original enquanto segue StockSharp padrões API de alto nível.

## Lógica principal

1. **Período principal (1 minuto)**
   - Average True Range (ATR) estima a volatilidade intradiária.
   - Uma média móvel de preços de fechamento mede o viés direcional de curto prazo.
   - Uma segunda média móvel rastreia a frequência de grandes movimentos de vela a vela, representando a probabilidade de rompimento de Poisson do código MQL.
   - Uma média móvel exponencial de movimentos de preços absolutos produz a probabilidade de estilo exponencial usada pelo filtro de segurança original.
2. **Prazo de confirmação (15 minutos)**
   - Uma média móvel simples mede a direção da tendência de médio prazo e bloqueia as negociações contra o fluxo dominante.
3. **Período de contexto (1 hora)**
   - As velas horárias fornecem a tendência de período de tempo mais alto e a faixa de volatilidade necessária para validação de rompimento e verificações de achatamento de reversão à média.

Quando as probabilidades proxy de Poisson e Weibull excedem o limite de rompimento, as tendências de 1 minuto e 15 minutos estão alinhadas com o lado positivo e a volatilidade horária é elevada, a estratégia entra em uma negociação de rompimento longo. Por outro lado, quando as probabilidades caem abaixo do limite de reversão à média e a tendência horária é estável, a estratégia vende a descoberto, visando retrocessos de volta ao intervalo. As ordens de mercado são usadas para espelhar o estilo de execução imediata do consultor especialista original.

## Gestão de risco

- Um atraso de negociação configurável evita negociações excessivas, impondo uma pausa entre entradas consecutivas.
- `MaxPositions` limita o número de posições abertas simultâneas. Ao reverter de uma negociação oposta, a estratégia fecha a exposição atual e abre a nova direção em uma única ordem de mercado.
- A estimativa dinâmica de volume usa o saldo da conta, a distância de parada derivada de ATR e a porcentagem de `RiskPerTrade` para produzir um tamanho de lote conservador. Se o cálculo falhar, o volume mínimo do passo será usado como padrão seguro.
- As negociações de segurança opcionais podem ser habilitadas para ambientes de validação ou teste onde pelo menos uma negociação deve aparecer. A direção do comércio de segurança segue a estimativa de tendência combinada de curto e médio prazo.
- `StartProtection()` ativa o bloco de proteção integrado de StockSharp para que problemas inesperados de conexão não deixem as posições sem gerenciamento.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `RiskPerTrade` | Risco por negociação em porcentagem do valor do portfólio (usado para cálculo dinâmico de lote). |
| `LookbackPeriod` | Número de velas concluídas usadas para médias móveis e cálculos ATR em todos os intervalos de tempo. |
| `BreakoutThreshold` | Probabilidade composta mínima necessária para uma entrada de fuga. |
| `MeanReversionThreshold` | Probabilidade máxima que ainda permite posições vendidas com reversão à média. |
| `TradeDelaySeconds` | Número mínimo de segundos entre entradas consecutivas. |
| `MaxPositions` | Posições simultâneas máximas (usadas para exposição longa e curta). |
| `EnableSafetyTrade` | Permite negociações de segurança de validação opcionais quando nenhuma posição está aberta. |
| `SafetyTradeIntervalSeconds` | Período de espera entre verificações comerciais de segurança. |
| `CandleType` | Período primário usado para a assinatura do sinal principal (padrão: 1 minuto). |

## Notas de uso

1. Anexe a estratégia a um instrumento que suporte dados de 1 minuto e forneça velas de 15 minutos e 1 hora (StockSharp agregará frames mais altos automaticamente quando o corretor fornecer barras de minutos).
2. Defina a propriedade `Volume` se um tamanho de pedido fixo for necessário. Caso contrário, a estratégia deriva um tamanho conservador do saldo da conta e ATR.
3. Ajuste os limites e comprimentos de lookback de acordo com o perfil de volatilidade do mercado-alvo. Pares de volatilidade mais alta podem se beneficiar de limites maiores para evitar falsos rompimentos frequentes.
4. As negociações de segurança destinam-se principalmente a cenários de validação em que o EA original executou pelo menos uma negociação, mesmo sem sinal. Desative-os para ambientes normais de negociação ao vivo.

A conversão mantém a ideia original de combinar detecção de fuga com salvaguardas de reversão, ao mesmo tempo em que conta com a estrutura de indicadores de alto nível do StockSharp para permanecer eficiente e fácil de testar.
