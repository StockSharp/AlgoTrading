# Estratégia RobotPowerM5 Meta4 V12
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia RobotPowerM5 Meta4 V12 é uma porta C# do MetaTrader 4 consultor especialista `RobotPowerM5_meta4V12.mq4`. O original EA
foi projetado para gráficos Forex de cinco minutos e avalia o equilíbrio entre Bulls Power e Bears Power para decidir se um novo
a posição longa ou curta deve ser aberta. A versão StockSharp mantém o comportamento de uma posição por vez, reproduz o ponto
com base em configurações de stop-loss/take-profit e reimplementa a lógica de trailing-stop que gradativamente bloqueia os lucros assim que o mercado
move-se a favor do comércio.

## Lógica de negociação
1. **Motor indicador**
   - Velas de cinco minutos são assinadas por padrão (o período é configurável através do parâmetro `CandleType`).
   - Um par de indicadores StockSharp, `BullsPower` e `BearsPower`, são atualizados em cada vela finalizada usando o configurado
período médio.
   - O valor combinado `BullsPower + BearsPower` é armazenado com um atraso de uma barra para imitar as chamadas `shift=1` do
Código MQL, que sempre opera na última barra totalmente fechada.
2. **Regras de entrada**
   - Quando nenhuma posição está aberta e a soma atrasada do Bulls/Bears Power é **positiva**, uma ordem de compra de mercado é emitida.
   - Quando nenhuma posição está aberta e a soma atrasada é **negativa**, uma ordem de venda a mercado é emitida.
   - Os sinais são ignorados enquanto uma posição está ativa; o comércio é gerido exclusivamente através de saídas protetoras.
3. **Manuseio de volume**
   - O parâmetro `Volume` representa o tamanho do lote solicitado. É passado diretamente para `BuyMarket` / `SellMarket`, permitindo que o
conector para arredondar para a etapa do lote do instrumento, se necessário.

## Gestão de risco
- **Stop-loss** – O stop inicial é colocado a `StopLossPoints` MetaTrader pontos de distância do preço médio de preenchimento. O nível é
monitorado com mínimas de velas (para posições compradas) ou máximas (para posições vendidas); uma vez tocada a estratégia sai no mercado.
- **Take-profit** – A meta de lucro é de `TakeProfitPoints` pontos a partir da entrada e é avaliada nas máximas/mínimas das velas, correspondendo
como o MT4 fecha posições quando um alvo é atingido intrabar.
- **Trailing stop** – Depois que o preço se move a favor da negociação em mais de `TrailingStopPoints`, um trailing stop é ativado.
Para posições longas o stop é deslocado para `referencePrice - trailingDistance`, onde a referência é o máximo do candle
perto e alto. Para vendas, o stop segue `referencePrice + trailingDistance`, usando o mínimo do fechamento e da mínima da vela.
Isso reproduz o comportamento final do EA que foi originalmente implementado com `OrderModify`.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `BullBearPeriod` | Período médio fornecido para os indicadores Bulls Power e Bears Power. | `5` | Aumentar o valor suaviza o filtro de impulso. |
| `Volume` | Tamanho do lote solicitado para cada entrada. | `1` | O volume real negociado depende da etapa e dos limites do lote da corretora. |
| `StopLossPoints` | Distância inicial de parada de proteção em MetaTrader pontos. | `45` | Defina como `0` para desativar o stop loss. |
| `TakeProfitPoints` | Distância de lucro em MetaTrader pontos. | `150` | Defina como `0` para negociar sem uma meta de lucro fixa. |
| `TrailingStopPoints` | Distância usada pelo trailing stop quando a negociação é lucrativa. | `15` | Defina como `0` para desativar o rastreamento. |
| `CandleType` | Prazo utilizado para cálculos de indicadores. | `5m time frame` | Qualquer outro `DataType` pode ser selecionado, se necessário. |

## Notas de implementação
- A estratégia armazena todos os níveis de risco (stop-loss, take-profit, trailing stop) internamente e emite saídas de mercado quando as velas
confirmar que um limite de preço foi violado. Isso reflete a abordagem MT4, onde os pedidos foram modificados tick a tick.
- As assinaturas de indicadores são conectadas via `Subscription.Bind`, que alimenta Bulls Power e Bears Power em um único retorno de chamada.
- O tamanho do ponto é derivado de `Security.PriceStep`, mantendo os parâmetros compatíveis com instrumentos que cotam em ticks,
pips ou centavos.
- As verificações de entrada sempre usam os valores do indicador *anterior*, garantindo que velas parcialmente formadas nunca acionem ordens.

## Diferenças versus a versão MQL
- A gestão comercial utiliza saídas explícitas do mercado em vez de modificar a ordem de stop-loss em vigor; isso é mais robusto em
diferentes conectores StockSharp enquanto produzem o mesmo resultado.
- Os intervalos de parâmetros são validados por meio de auxiliares `StrategyParam` para que valores inválidos (por exemplo, pontos finais negativos) sejam
rejeitado no momento da configuração.
- Ganchos de registro detalhados, saída de gráfico e assinaturas de velas aproveitam o API de alto nível do API em vez de loops de tick manuais.
- A string do identificador especialista presente no script MT4 não é necessária em StockSharp e, portanto, foi omitida.
