# Estratégia Bobnaley
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Bobnaley reproduz o MetaTrader 5 consultor especialista "bobnaley" usando o StockSharp API de alto nível. Ele combina um filtro de tendência de média móvel simples com o oscilador estocástico para procurar oportunidades de reversão. O especialista original avaliou os preços dos ticks; o porto usa velas concluídas e mantém intactas as regras de gerenciamento de pedidos.

## Como funciona
1. **Indicadores**
   - Uma média móvel simples com o período configurado filtra a direção predominante.
   - Um oscilador estocástico (linhas principal e de sinal) identifica situações de sobrevenda e sobrecompra. Apenas a linha principal é necessária para sinais; a linha de sinal é calculada para ser completada.
2. **Condições de entrada**
   - A estratégia espera até que a vela atual termine e todos os indicadores sejam formados.
   - As entradas longas exigem que a média móvel diminua estritamente durante as últimas três amostras, enquanto o preço fecha acima da média mais recente. Ao mesmo tempo, a linha principal estocástica deve estar abaixo do nível de sobrevenda e seu valor anterior deve ser superior ao anterior, refletindo o requisito original EA `stochVal[1] > stochVal[2]`.
   - As entradas curtas são a imagem espelhada: a média móvel deve estar subindo nas últimas três amostras enquanto o preço fecha abaixo dela, e a linha principal estocástica deve estar acima do nível de sobrecompra enquanto seu valor anterior é inferior ao anterior.
   - Novas negociações são abertas somente quando nenhuma posição está ativa no momento, replicando a proteção `PositionSelect` de MetaTrader.
3. **Gerenciamento de Riscos**
   - Quando uma posição é aberta, a estratégia depende do serviço de proteção de StockSharp para colocar um take-profit e um stop-loss em unidades de preço absoluto. Essas distâncias correspondem às entradas MetaTrader (0,007 e 0,0035 por padrão).
   - Antes de cada decisão, o valor do portfólio é comparado com o parâmetro `Minimum Balance`, espelhando o filtro de margem livre (`ACCOUNT_FREEMARGIN > 5000`) do código original. Se o valor da conta for conhecido e estiver abaixo do limite, a entrada será ignorada.
4. **Manuseio de Volume**
   - Os pedidos usam um parâmetro `Base Volume` fixo. Isso reproduz a configuração de lote que o script MetaTrader usou após aplicar sua própria rotina de arredondamento.

## Parâmetros
| Categoria | Nome | Descrição | Padrão |
| --- | --- | --- | --- |
| Geral | Tipo de vela | Tipo de dados Candle usado para cálculos de indicadores. | Período de 5 minutos |
| Negociação | Volume básico | Volume de pedido fixo aplicado a cada nova posição. | 5 |
| Indicadores | Período MA | Comprimento da média móvel simples. | 76 |
| Indicadores | Stochastic Período | Lookback para a linha principal estocástica. | 5 |
| Indicadores | Stochastic %K | Suavização do comprimento da linha %K. | 3 |
| Indicadores | Stochastic %D | Suavização do comprimento da linha %D. | 3 |
| Indicadores | Stochastic Sobrevenda | Limite que define o território de sobrevenda para a linha principal. | 30 |
| Indicadores | Stochastic Sobrecomprado | Limiar que define território de sobrecompra para a linha principal. | 70 |
| Gestão de Risco | Obtenha lucro | Distância entre o preço de entrada e o take-profit em unidades de preço. | 0,007 |
| Gestão de Risco | Parar perda | Distância entre o preço de entrada e o stop loss em unidades de preço. | 0,0035 |
| Gestão de Risco | Saldo Mínimo | Valor mínimo do portfólio necessário antes que um novo pedido possa ser enviado. | 5.000 |

## Notas
- O especialista original usou cotações Bid/Ask; em StockSharp o fechamento da vela é usado como proxy do preço de execução.
- Nenhuma saída final é implementada – a negociação é fechada apenas pelas ordens de proteção.
- Os cálculos de Stochastic seguem as configurações padrão de MetaTrader (5/3/3), mas podem ser otimizados por meio dos parâmetros expostos.
