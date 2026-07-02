# Estratégia de comércio 2 da Cloudzs
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Cloudzs Trade 2** é uma versão StockSharp do MetaTrader 4 consultor especialista `cloudzs_trade_2`. O robô original combina reversões de osciladores estocásticos com um filtro de confirmação de fractal duplo e usa lógica de rastreamento agressiva para proteger posições abertas. Esta versão C# recria o fluxo de sinal e as regras de gerenciamento comercial enquanto expõe os parâmetros como objetos `StrategyParam` para que possam ser otimizados ou ajustados na IU do StockSharp.

A estratégia observa uma única série de velas (período configurável) e avalia duas condições independentes:

1. **Stochastic reversão** – é acionada quando a linha %D sai de uma zona extrema (>= 80 para vendas, <= 20 para compras) enquanto confirma que %D cruzou a linha %K na vela anterior, correspondendo de perto à lógica original MQL.
2. **Confirmação de fractal duplo** – espera até que dois sinais fractais consecutivos do mesmo tipo apareçam (dois fractais superiores para vendas ou dois fractais inferiores para compras).

Se alguma das condições gerar uma solicitação de compra ou venda, a estratégia entra nessa direção (desde que nenhuma negociação esteja ativa e a saída anterior tenha sido em um dia diferente). Quando já estiver em uma negociação, as mesmas condições podem ser usadas para sair mais cedo se `CloseOnOpposite` estiver habilitado.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `LotSplitter` | Coeficiente usado para aproximar o volume comercial do valor da conta corrente. | `0.1` |
| `MaxVolume` | Limite superior para o volume calculado (0 desativa o limite). | `0` |
| `TakeProfitOffset` | Distância fixa de lucro em unidades de preço absoluto. | `0` |
| `TrailingStopOffset` | Distância do trailing stop em unidades de preço. | `0.01` |
| `StopLossOffset` | Distância fixa de stop-loss em unidades de preço. | `0.05` |
| `MinProfitOffset` | Lucro mínimo a ser mantido após uma excursão favorável quando `ProfitPointsOffset` for alcançado. | `0` |
| `ProfitPointsOffset` | Movimento favorável necessário antes que `MinProfitOffset` seja aplicado. | `0` |
| `%K Period` / `%D Period` / `Slowing` | Configuração do oscilador Stochastic. | `8 / 8 / 4` |
| `Method` | Identificador de método estocástico MT4 original (informativo, não usado porque StockSharp expõe uma única implementação). | `3` |
| `PriceMode` | Identificador original do modo de preço MT4 (apenas informativo). | `1` |
| `UseStochasticCondition` | Habilite a geração de sinal com base estocástica. | `true` |
| `UseFractalCondition` | Habilite a geração de sinal baseada em fractal. | `true` |
| `CloseOnOpposite` | Feche a posição ativa quando o sinal oposto aparecer. | `true` |
| `CandleType` | Período/tipo de dados usado para cálculos. | `15-minute time frame` |

## Sinais de negociação
### Entrada longa
- A linha %D está abaixo ou igual a 20 e cruza abaixo de %K (correspondendo à comparação da vela anterior do MT4).
- **OU** dois fractais inferiores sequenciais são detectados.
- Nenhuma posição aberta e a última saída ocorreu em um dia diferente.

### Entrada curta
- A linha %D está acima ou igual a 80 e cruza acima de %K.
- **OU** aparecem dois fractais superiores sequenciais.
- Nenhuma posição aberta e a última saída ocorreu em um dia diferente.

### Regras de saída
- Os níveis de hard stop-loss ou take-profit são alcançados (se configurados).
- O trailing stop se move a favor da negociação e o preço toca o nível de stop atualizado.
- Após a posição experimentar um movimento favorável de `ProfitPointsOffset`, um retrocesso para `MinProfitOffset` fecha a negociação.
- Reversão antecipada opcional: se `CloseOnOpposite` for verdadeiro e o sinal oposto for acionado, a negociação será fechada.

## Gestão de risco
- As distâncias de stop-loss e take-profit imitam as compensações brutas de pip do código MT4 (interpretadas aqui como diferenças de preço).
- Os trailing stops são atualizados usando o preço de fechamento e se movem apenas na direção lucrativa.
- O parâmetro `LotSplitter` tenta seguir a fórmula de volume original, dimensionando o tamanho da negociação por valor da conta e limitando-o com `MaxVolume`.

## Notas e Limitações
- O StockSharp `StochasticOscillator` expõe uma única implementação de suavização; portanto, os parâmetros `Method` e `PriceMode` são mantidos como referência, mas não alteram o comportamento do indicador.
- O script MT4 original funcionou passo a passo. Esta porta avalia sinais em velas finalizadas para se alinhar com StockSharp práticas recomendadas.
- O cálculo do volume depende dos valores disponíveis da carteira; se não existirem informações da conta, ela retornará ao valor `LotSplitter`.

## Uso
1. Adicione a estratégia ao seu projeto StockSharp e selecione o instrumento que deseja negociar.
2. Configure o período da vela e ajuste as configurações estocásticas/fractais, se necessário.
3. Forneça compensações realistas de stop-loss/take-profit que correspondam ao tamanho do tick do instrumento.
4. Inicie a estratégia no Designer, Runner ou via API e monitore as mensagens de log para obter informações de sinal.
