# Estratégia de Padrão MACD AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma fiel portagem para StockSharp do consultor especialista FORTRADER `MACD.mq5`. Implementa o padrão "AOP" que observa o oscilador MACD em busca de excursões profundas afastadas da linha zero seguidas de um gancho de retorno em direção à neutralidade. Quando o gancho é confirmado, a estratégia entra na direção da reversão esperada e aplica imediatamente alvos fixos de stop-loss e take-profit expressos em pips.

## Lógica da estratégia
### Preparação de dados
- Opera na série de velas selecionada pelo parâmetro `CandleType` (velas de 5 minutos por padrão).
- Utiliza um indicador MACD padrão com períodos rápido, lento e sinal configuráveis (padrões 12/26/9).
- Armazena os valores da linha principal MACD das três velas concluídas mais recentes para reproduzir o acesso baseado em índice do MQL (`iMACD(...,1..3)`).

### Configuração curta (gancho de baixa)
1. **Armação** – quando a linha principal MACD da última vela fechada cai abaixo de `BearishExtremeLevel` (padrão −0.0015), a estratégia começa a observar uma reversão.
2. **Recuo neutro** – quando o MACD sobe de volta acima de `BearishNeutralLevel` (padrão −0.0005), a etapa de validação do gancho fica ativa.
3. **Confirmação do gancho** – os três valores MACD anteriores devem formar um máximo local (`macd₁ < macd₂ > macd₃`) enquanto o valor mais recente permanece abaixo do nível neutro e o valor mais antigo permanece acima. Isso recria o padrão original que garante que o momentum está desvanecendo.
4. **Entrada** – se nenhuma posição comprada estiver aberta (`Position <= 0`), uma ordem de venda a mercado de `OrderVolume` é enviada. Os níveis de proteção são calculados imediatamente: stop-loss acima da entrada em `StopLossPips` e take-profit abaixo em `TakeProfitPips` (convertidos para preço por `GetPipSize`).
5. Qualquer leitura positiva do MACD cancela a configuração e reinicia a máquina de estado de baixa até que apareça um novo trecho negativo profundo.

### Configuração longa (gancho de alta)
1. **Armação** – quando o MACD sobe acima de `BullishExtremeLevel` (padrão +0.0015), o modo de observação de alta é ativado.
2. **Cancelamento imediato** – se o MACD cair abaixo de zero, o cenário de alta é abandonado, espelhando a lógica do MQL.
3. **Recuo neutro** – uma queda de volta abaixo de `BullishNeutralLevel` (padrão +0.0005) prepara a confirmação do gancho.
4. **Confirmação do gancho** – os três valores MACD armazenados devem criar um mínimo local (`macd₁ > macd₂ < macd₃`) respeitando os limiares neutros.
5. **Entrada** – se não houver exposição curta (`Position >= 0`), a estratégia compra a mercado com `OrderVolume` e define stop-loss e take-profit em torno da entrada simetricamente às regras curtas.

### Gestão de risco
- O stop-loss e o take-profit estão sempre ativos via `_stopPrice` e `_takePrice`. São avaliados em cada vela concluída usando a máxima/mínima registrada para emular a execução do lado do broker no EA original.
- Os pips são convertidos para preços absolutos usando `Security.PriceStep`. Para símbolos FX de 3 e 5 dígitos, o passo é multiplicado por 10 para corresponder ao ajuste do MQL para pips fracionários.
- Sempre que a estratégia sai de uma posição por causa dos níveis de proteção, os limpa imediatamente e aguarda uma nova configuração nas próximas velas.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Série de dados de velas processada pela estratégia. | Período de 5 minutos |
| `OrderVolume` | Volume enviado com cada ordem de mercado. | 0.1 |
| `TakeProfitPips` | Distância ao alvo de lucro em pips. Marcado para otimização. | 60 |
| `StopLossPips` | Distância ao stop-loss em pips. Marcado para otimização. | 70 |
| `MacdFastPeriod` | Comprimento da EMA rápida para MACD. | 12 |
| `MacdSlowPeriod` | Comprimento da EMA lenta para MACD. | 26 |
| `MacdSignalPeriod` | Comprimento da EMA sinal para MACD. | 9 |
| `BearishExtremeLevel` | Limiar negativo do MACD que arma oportunidades curtas. | −0.0015 |
| `BearishNeutralLevel` | Limiar negativo do MACD usado para validar o gancho de baixa. | −0.0005 |
| `BullishExtremeLevel` | Limiar positivo do MACD que arma oportunidades longas. | +0.0015 |
| `BullishNeutralLevel` | Limiar positivo do MACD usado para validar o gancho de alta. | +0.0005 |

## Notas adicionais
- A estratégia reage apenas uma vez por vela concluída, imitando o guardião `PrevBars` original no MQL.
- A gestão de stop-loss/take-profit é puramente baseada em preço; não há ajustes de trailing ou reentradas até que o ciclo completo da máquina de estado se repita.
- Projetado para contas de cobertura no EA fonte, mas esta portagem impõe uma única posição líquida verificando `Position` antes de enviar novas ordens.
- Nenhuma versão Python foi fornecida conforme solicitado.
