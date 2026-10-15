# Estratégia sem censura de Tuyul
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Tuyul Uncensored é uma estratégia de seguimento de swing que reconstrói o consultor especialista MetaTrader 5 original com o StockSharp de alto nível de API. O sistema observa as oscilações do ZigZag, alinha as entradas com um filtro de tendência de média móvel e coloca ordens de limite na retração Fibonacci de 57% da perna mais recente. Quando o preço revisita esse nível, a estratégia tenta aderir à oscilação dominante enquanto protege a negociação com níveis de stop-loss e take-profit derivados da mesma perna.

## Lógica de negociação
1. **Preparação de dados**
   - Uma assinatura de vela definida pelo parâmetro `Candle Type` selecionado.
   - Um indicador ZigZag (Profundidade/Desvio/Backstep) é usado para rastrear a última oscilação máxima e mínima confirmada.
   - EMAs rápidos e lentos (padrão 9/21) fornecem o filtro direcional.
2. **Detecção de sinal**
   - Quando o ZigZag confirma um novo pivô (seja uma nova máxima ou uma nova mínima), a estratégia atualiza o par de swing mais recente.
   - Se nenhuma ordem estiver ativa e não houver posição aberta, os valores EMA anteriores determinam a tendência:
     - EMA rápida acima do EMA lenta → contexto de alta.
     - EMA rápida abaixo do EMA lenta → contexto de baixa.
3. **Colocação de pedido**
   - Em um contexto de alta, a estratégia coloca uma ordem de **limite de compra** na retração de 57% entre a última oscilação mínima e a oscilação máxima.
   - Em um contexto de baixa, ele coloca uma ordem de **limite de venda** na retração simétrica de 57%, da oscilação máxima para a oscilação mínima.
   - O stop loss está ancorado no extremo oposto do ZigZag, enquanto o take-profit é igual à distância do stop multiplicada por `Take Profit Multiplier` (padrão 1,2).
   - Os pedidos permanecem ativos para `Wait Bars After Signal` velas; depois eles são cancelados para aguardar um novo sinal.
4. **Gerenciamento de posição**
   - Depois que um pedido é atendido, a estratégia observa as velas subsequentes. Uma posição longa é fechada quando o preço atinge o stop-loss ou o take-profit predefinidos. A mesma lógica espelhada se aplica às posições curtas.
   - A negociação pode ser limitada a dias da semana específicos. Fora dos dias permitidos, todas as ordens pendentes são removidas, mas as posições existentes permanecem intactas, seguindo o comportamento original do consultor.

## Parâmetros
| Nome | Descrição |
|------|-------------|
| `Volume Per Trade` | Volume de pedidos enviado com cada entrada. |
| `TP Multiplier` | Multiplicador aplicado à distância de parada para calcular a compensação do lucro. |
| `ZigZag Depth` | Número de velas examinadas ao confirmar uma oscilação. |
| `ZigZag Deviation` | Desvio mínimo (em pontos) necessário antes que o ZigZag valide um novo pivô. |
| `ZigZag Backstep` | Número mínimo de velas entre pivôs ZigZag opostos. |
| `Wait Bars After Signal` | Máximo de velas para manter a ordem pendente ativa antes do cancelamento. |
| `Fast EMA` | Período da média móvel exponencial rápida usada como filtro de tendência. |
| `Slow EMA` | Período da média móvel exponencial lenta usada como filtro de tendência. |
| `Allow Monday … Allow Friday` | Alterna que ativa ou desativa a negociação em dias da semana individuais. |
| `Candle Type` | Série de velas usada para todos os cálculos de indicadores e decisões de negociação. |

## Notas
- A taxa de retração Fibonacci é fixada em 57% como na fonte EA.
- Os níveis de stop-loss e take-profit são monitorados no fechamento das velas; picos intrabar além dos limites desencadeiam saídas de mercado na próxima avaliação.
- A estratégia mantém uma única ordem pendente por vez, refletindo a implementação original.
