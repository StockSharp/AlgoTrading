# Estratégia AMA Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia AMA Trader replica o comportamento do expert original MetaTrader 5 "AMA Trader". Combina a Média Móvel Adaptativa de Kaufman (AMA) com o Índice de Força Relativa (RSI) para fazer averaging em trades contra pullbacks de curto prazo enquanto o preço permanece no lado prevalecente do filtro de tendência adaptativa. A implementação StockSharp usa a API de alto nível com assinaturas de velas e vinculação de indicadores para ficar próxima da lógica original enquanto permanece totalmente compatível com o modelo de execução StockSharp.

## Premissas de Mercado
- **Tipo de instrumento**: projetado para FX spot ou CFD, mas aplicável a qualquer instrumento de tendência que suporte averaging.
- **Período**: velas de um minuto por padrão, configurável através do parâmetro `CandleType`.
- **Sessões**: sem tratamento de sessão explícito. Sinais são avaliados em cada vela terminada.

## Indicadores
1. **Média Móvel Adaptativa de Kaufman (AMA)**
   - Suaviza a ação do preço com parâmetros para as constantes de suavização rápida e lenta (`AmaFastPeriod`, `AmaSlowPeriod`) e o comprimento de averaging (`AmaLength`).
   - Define a direção principal da tendência. Trades comprados são considerados apenas quando o preço de fechamento está acima da AMA; trades vendidos apenas quando está abaixo.
2. **Índice de Força Relativa (RSI)**
   - Avaliado com período `RsiLength` no fechamento da vela.
   - `StepLength` controla quantos valores recentes do RSI devem confirmar um estado de sobrecompra/sobrevenda. Um valor de 0 retorna para verificar apenas a última barra, imitando a implementação MQL onde `StepLength == 0` é tratado como 1.
   - `RsiLevelDown` (padrão 30) e `RsiLevelUp` (padrão 70) definem limites de sobrevenda e sobrecompra respectivamente.

## Lógica de Negociação
1. **Validação da barra**
   - Trades são avaliados apenas em velas terminadas e quando a estratégia está online e com permissão para negociar.
2. **Gerenciamento de lucro antes de novas entradas**
   - Se o lucro não realizado de todas as posições abertas exceder `ProfitTarget`, a estratégia fecha cada posição aberta e aguarda o próximo sinal.
   - Se o lucro realizado desde o último reinício crescer mais de `WithdrawalAmount`, todas as posições são fechadas e o ponto de controle do lucro realizado é atualizado. Isso imita a mecânica de retirada do expert original (nenhum dinheiro real é removido; apenas o ponto de controle é reiniciado).
3. **Entradas compradas**
   - Condição: preço de fechamento > AMA e pelo menos um dos valores de RSI inspecionados está abaixo de `RsiLevelDown`.
   - Ação: enviar uma ordem de compra a mercado. Se a exposição comprada atual estiver perdendo dinheiro (PnL não realizado negativo baseado no preço médio de entrada rastreado), uma ordem de compra de averaging adicional é enviada.
4. **Entradas vendidas**
   - Condição: preço de fechamento < AMA e pelo menos um dos valores de RSI inspecionados está acima de `RsiLevelUp`.
   - Ação: enviar uma ordem de venda a mercado. Se a exposição vendida atual estiver perdendo, uma ordem de venda de averaging adicional é enviada.
5. **Rastreamento de posição**
   - Execuções são processadas em `OnOwnTradeReceived`. Preços médios e volumes separados são rastreados para exposição comprada e vendida, permitindo estimativas precisas de PnL não realizado mesmo quando o mercado alterna entre compras e vendas.

## Gerenciamento de Risco
- **Volume de averaging**: cada entrada usa o `LotSize` fixo. Quando ocorrem perdas, o algoritmo dobra adicionando uma ordem extra na mesma direção.
- **Alvo de lucro não realizado**: `ProfitTarget` (padrão 50 unidades monetárias) força uma saída completa quando os lucros flutuantes atingem o nível especificado.
- **Ponto de controle de lucro realizado**: `WithdrawalAmount` (padrão 1000) fecha todas as posições assim que o PnL realizado acumulado excede o limite, após o qual o ponto de controle é reiniciado para o PnL realizado atual.
- **Proteção manual**: nenhum stop-loss ou take-profit automático é configurado além do alvo de lucro não realizado. Os usuários podem habilitar controles de risco externos se necessário.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados de vela ou período para cálculos de indicadores. |
| `LotSize` | Volume fixo para cada ordem de mercado. |
| `RsiLength` | Período de averaging RSI. |
| `StepLength` | Número de valores recentes do RSI examinados (0 retorna para 1). |
| `RsiLevelUp` | Limite de sobrecompra do RSI para sinais vendidos. |
| `RsiLevelDown` | Limite de sobrevenda do RSI para sinais comprados. |
| `AmaLength` | Período de suavização AMA. |
| `AmaFastPeriod` | Constante de suavização rápida AMA. |
| `AmaSlowPeriod` | Constante de suavização lenta AMA. |
| `ProfitTarget` | Lucro não realizado necessário para achatar todas as posições (0 desabilita a regra). |
| `WithdrawalAmount` | Incremento de lucro realizado que aciona uma saída completa (0 desabilita a regra). |

## Notas de Implementação
- Uso da API de alto nível: velas são assinadas via `SubscribeCandles`, e AMA/RSI são vinculados à assinatura via `.Bind`. O delegado de processamento recebe valores decimais brutos, evitando acesso manual a valores do indicador.
- O monitoramento de posição depende de acumuladores privados atualizados dentro de `OnOwnTradeReceived`. Isso reflete a inspeção de posições do expert MQL sem recorrer a getters agregadores proibidos.
- Ordens são enviadas com `BuyMarket` e `SellMarket`, usando o `LotSize` atual. O achatamento usa argumentos de volume explícitos para que tanto a exposição comprada quanto a vendida possam ser limpas.
- A versão StockSharp usa o preço de fechamento da vela em vez da verificação de ask/bid do MetaTrader ao avaliar a relação AMA, que é a informação mais próxima disponível dentro de um fluxo de trabalho baseado em velas.

## Diferenças do Expert MetaTrader
- `WithdrawalAmount` atualiza um ponto de controle interno em vez de chamar `TesterWithdrawal`, porque o backtester do StockSharp não suporta retiradas sintéticas.
- As opções de deslocamento AMA e preço aplicado do EA original não são expostas. Os indicadores do StockSharp operam com preços de fechamento de velas.
- Comissões e swaps não são explicitamente adicionados ao cálculo do PnL não realizado; o ambiente de execução do StockSharp lida com taxas internamente quando as negociações são liquidadas.

## Dicas de Uso
- Considere combinar a estratégia com limites de risco em nível de portfólio ou o módulo de proteção integrado se o averaging for muito agressivo para o instrumento negociado.
- Otimize as configurações de AMA e RSI por instrumento. Períodos mais baixos geralmente se beneficiam de períodos AMA mais curtos e limites RSI mais amplos.
- Monitore drawdowns quando `StepLength` > 1, pois o averaging pode ser acionado várias vezes durante movimentos fortes contra a tendência.
