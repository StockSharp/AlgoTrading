# Martin Para Pequenos Depósitos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o expert de médias "Martin for small deposits" no StockSharp. Analisa 15 velas concluídas e abre uma posição somente quando o fechamento mais novo está abaixo (para comprados) ou acima (para vendidos) do fechamento registrado 14 barras antes. Todas as operações são executadas a mercado usando a API de estratégias de alto nível, e a lógica é aplicada uma vez por vela terminada.

## Lógica de entrada
- Um buffer deslizante mantém os últimos 15 fechamentos de velas concluídas.
- Quando não há posições abertas ou pendentes, a estratégia compara o fechamento mais recente com o fechamento de 14 barras atrás.
- Se o último fechamento é mais baixo, uma grade comprada é iniciada; se é mais alto, uma grade vendida é iniciada.
- O volume da operação para a primeira ordem é igual a **Initial Volume**. As entradas subsequentes no mesmo lado usam o multiplicador de martingale antes de serem normalizadas para o passo de volume do instrumento.

## Gerenciamento de posição
- Enquanto existe uma posição, a estratégia aguarda **Bars To Skip** velas terminadas antes de considerar outra operação de médias.
- Ordens adicionais são enviadas somente se o preço se mover contra a direção atual em pelo menos **Step (pips)**, convertidos para unidades de preço usando o tamanho de pip detectado.
- Cada execução atualiza estatísticas internas: volume agregado, preço médio de entrada, preço de entrada mais baixo (para comprados) ou mais alto (para vendidos), e o preço do último preenchimento.
- O volume nunca excede **Max Volume** nem o volume máximo definido pelo exchange. Se o tamanho normalizado cair abaixo do volume mínimo permitido, a ordem é ignorada.

## Condições de saída
- Quando o lucro líquido não realizado (diferença entre o fechamento atual e o preço médio de entrada, multiplicado pelo volume da posição) supera **Min Profit**, todas as ordens abertas são niveladas.
- Se **Take Profit (pips)** for maior que zero e o preço atingir essa distância desde a última entrada na direção favorável, toda a grade é fechada.
- As solicitações de fechamento são rastreadas; nenhuma nova ordem é enviada até que as ordens de saída estejam completamente preenchidas. Após atingir um estado plano, todos os contadores internos são redefinidos para que o próximo sinal inicie uma grade nova.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| Initial Volume | 0.01 | Tamanho de lote base para a primeira operação. |
| Take Profit (pips) | 65 | Distância em pips desde o último preenchimento que aciona uma saída total. Use 0 para desabilitar esta verificação. |
| Step (pips) | 15 | Movimento adverso em pips necessário antes de médiar na posição. |
| Bars To Skip | 45 | Número mínimo de velas terminadas a aguardar entre ordens de médias. |
| Increase Factor | 1.7 | Multiplicador aplicado ao volume da operação cada vez que uma nova ordem é adicionada no mesmo lado. |
| Max Volume | 6 | Limite superior para o volume agregado (antes da normalização pelos limites do mercado). |
| Min Profit | 10 | Meta de lucro usada para fechar toda a grade quando o lucro líquido supera este valor. |
| Candle Type | 1 hora | Período usado para assinatura de velas e cálculos de sinal. |

## Notas de implementação
- O tamanho de pip é derivado de `Security.PriceStep` e precisão decimal. Para instrumentos cotados com 3 ou 5 casas decimais, o código multiplica o passo de preço por 10 para corresponder ao conceito MQL de um pip.
- O lucro não realizado é aproximado a partir de diferenças de preço e não inclui ajustes de swap ou comissão que estavam presentes no expert original.
- Operações de médias adicionais são ignoradas enquanto ordens de saída estão ativas, preservando o fluxo de execução sequencial da lógica MQL original.
- Quando **Step (pips)** é zero, a estratégia nunca faz médias; quando **Take Profit (pips)** é zero, apenas a condição **Min Profit** fecha a grade.
