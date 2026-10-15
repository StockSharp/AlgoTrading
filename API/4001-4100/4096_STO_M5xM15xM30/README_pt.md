# Estratégia STO M5xM15xM30
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão C# fiel do consultor especialista MetaTrader 4 "STO_m5xm15xm30". Ele usa três osciladores estocásticos calculados nos prazos M5, M15 e M30 para identificar mudanças de momento sincronizadas. A implementação StockSharp mantém a estrutura original de entrada/saída, substitui o gerenciamento manual de pedidos pelo API de alto nível e expõe cada constante de chave como um `StrategyParam` configurável.

## Lógica de negociação
1. **Confirmação de vários prazos**
   - O estocástico primário (padrão M5) deve mostrar um cruzamento de alta (`%K` cruzamentos acima de `%D`).
   - Os valores estocásticos intermediários (padrão M15) e lentos (padrão M30) já devem ser de alta (`%K` acima de `%D`).
   - Uma configuração de baixa requer condições espelhadas (`%K` abaixo de `%D`).
2. **Filtro de mudança**
   - O estocástico primário também verifica o estado das velas `ShiftBars` anteriormente. Um sinal de compra exige que o histórico `%K` esteja abaixo de `%D`, garantindo um novo cruzamento. Os sinais de venda exigem o oposto.
3. **Filtro de impulso de preço**
   - O último fechamento deve ser superior (para compras) ou inferior (para vendas) do que o fechamento anterior da vela concluída. Isso reflete a regra `Close[0] > Close[1]` do script MT4.
4. **Regras de entrada**
   - Se nenhuma posição estiver aberta e os critérios de alta forem atendidos, a estratégia abre uma ordem longa de mercado com o `TradeVolume` configurado.
   - Se existir uma posição curta quando um sinal de alta chegar, ela será achatada primeiro e uma posição longa será aberta depois. O inverso é verdadeiro para sinais de baixa.
5. **Regras de saída**
   - Um estocástico M5 dedicado com período `ExitKPeriod` verifica a vela anterior (`shift = 1`). Uma posição longa é fechada quando `%K` cai abaixo de `%D`; uma venda é fechada quando `%K` sobe acima de `%D`.
   - Depois que uma saída é acionada, a estratégia pula a reentrada imediata na mesma vela, replicando o comportamento do loop de pedidos MT4.

## Indicadores e assinaturas de dados
- Velas primárias: período padrão de 5 minutos (`CandleType`).
- Velas de confirmação intermediárias: período padrão de 15 minutos (`MiddleCandleType`).
- Velas de confirmação lentas: período padrão de 30 minutos (`SlowCandleType`).
- Osciladores Stochastic: todos usam suavização %K = 3 e suavização %D = 3, correspondendo aos parâmetros originais.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Velas de 5 minutos | Prazo de trabalho para entradas e saídas. |
| `MiddleCandleType` | Velas de 15 minutos | Prazo de confirmação nº 1. |
| `SlowCandleType` | Velas de 30 minutos | Prazo de confirmação nº 2. |
| `FastKPeriod` | 5 | Período %K para o estocástico primário. |
| `MiddleKPeriod` | 5 | Período %K para o estocástico médio. |
| `SlowKPeriod` | 5 | Período %K para o estocástico lento. |
| `ExitKPeriod` | 5 | Período %K para o estocástico de saída que opera na barra anterior. |
| `ShiftBars` | 3 | Número de barras entre o cruzamento de referência e a barra atual. |
| `TakeProfitPoints` | 30 | Distância protetora de take-profit (pontos). |
| `StopLossPoints` | 10 | Distância protetora de stop-loss (pontos). |
| `TradeVolume` | 0,1 | Volume de pedidos usado para novas entradas. |

Todos os parâmetros são expostos por meio de `StrategyParam<T>`, disponibilizando-os para otimização dentro do StockSharp Designer.

## Gestão de risco
`StartProtection()` traduz as entradas MT4 `TP` e `SL` em StockSharp ordens de proteção. Ambos podem ser desabilitados definindo o parâmetro correspondente como zero.

## Notas de implementação
- Os valores dos indicadores são obtidos exclusivamente por meio do `SubscribeCandles(...).BindEx(...)`, obedecendo às diretrizes de alto nível do API e evitando coletas manuais de indicadores.
- O auxiliar `StochasticShiftBuffer` imita o argumento `shift` do MT4 sem chamar `GetValue`, mantendo apenas o histórico de barras necessário.
- O processamento de entrada acontece uma vez por vela concluída. A avaliação de saída ocorre antes da lógica de entrada, correspondendo à ordem de processamento do EA original.
- Os comentários embutidos explicam cada etapa do processamento e esclarecem como a lógica MQL é mapeada para o código StockSharp.

## Uso
1. Adicione a estratégia a um esquema StockSharp ou projeto de designer.
2. Configure o símbolo desejado e garanta que os dados históricos das velas M5, M15 e M30 estejam disponíveis.
3. Ajuste os parâmetros para se adequar ao mercado-alvo ou ao cenário de otimização.
4. Inicie a estratégia; Os níveis protetores de stop-loss/take-profit são registrados automaticamente para cada posição.
