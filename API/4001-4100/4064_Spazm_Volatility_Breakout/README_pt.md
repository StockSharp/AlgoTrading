# Estratégia de ruptura de volatilidade Spazm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Conversão do consultor especialista MetaTrader 4 **Spazm (8683)** para o StockSharp API de alto nível.
- Negocia rompimentos adaptativos comparando o fechamento mais recente com envelopes do tamanho da volatilidade em torno da oscilação máxima e mínima mais recente.
- Mantém anotações de gráfico opcionais que unem pivôs consecutivos de alta e baixa, assim como a visualização original MQL.

## Preparação de Dados
1. A estratégia assina a série de velas especificada pelo parâmetro `CandleType` para o título ativo.
2. Cada vela finalizada fornece a amostra bruta usada para estimativa de volatilidade:
   - Por padrão, o intervalo é igual a `High - Low`.
   - Quando `UseOpenCloseRange` está ativado, o tamanho absoluto do corpo `|Open - Close|` é usado.
3. A amostra de intervalo é convertida em etapas de preço usando o instrumento `PriceStep` para que a lógica permaneça invariante entre os símbolos.
4. O indicador definido por `UseWeightedVolatility` processa a sequência de amostras de intervalo:
   - Desativado → média móvel simples com comprimento `VolatilityPeriod`.
   - Ativado → média móvel ponderada linear (mais peso para velas recentes).
5. O intervalo suavizado (expresso em etapas) é multiplicado por `VolatilityMultiplier` e finalmente reduzido para unidades de preço. O valor resultante é o limite de ruptura adaptativo aplicado a ambos os lados do mercado.
6. Durante a fase de aquecimento, a estratégia também registra os máximos e mínimos extremos mais recentes, juntamente com seus carimbos de data e hora. Depois que `VolatilityPeriod * 3` velas são processadas, o tempo relativo desses extremos determina a direção da tendência inicial.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `1` | Volume de ordens enviado sempre que a estratégia abre ou reverte uma posição. |
| `VolatilityMultiplier` | `5` | Multiplicador aplicado à volatilidade média para construir a distância de rompimento. |
| `VolatilityPeriod` | `24` | Número de velas usadas tanto para o estimador de volatilidade quanto para semear os extremos iniciais da oscilação. |
| `UseWeightedVolatility` | `false` | Muda o estimador de volatilidade de uma média móvel ponderada simples para uma média linear ponderada. |
| `UseOpenCloseRange` | `false` | Usa o movimento absoluto de abertura e fechamento em vez da faixa máxima-baixa ao medir a volatilidade. |
| `StopLossMultiplier` | `0` | Multiplicador aplicado ao limite de ruptura para calcular uma distância de parada protetora. Um mínimo de três etapas de preço é aplicado. Defina como `0` para desativar paradas. |
| `DrawSwingLines` | `true` | Quando ativada, a estratégia traça uma linha entre os últimos pivôs de alta e baixa, imitando os objetos MQL. |
| `CandleType` | `4 hour time frame` | Tipo de vela (período de tempo ou outro tipo de dado) que alimenta os cálculos. |

## Lógica de negociação
1. **Inicialização**
   - Enquanto as primeiras velas `VolatilityPeriod * 3` são processadas, a estratégia atualiza `_highestPrice`, `_lowestPrice`, `_highestTime` e `_lowestTime` para capturar os extremos mais recentes.
   - Após a chegada de velas suficientes, o mais recente dos dois extremos define a tendência inicial: se a última mínima for mais recente que a última máxima, a estratégia começa no modo de alta, caso contrário, começa em baixa.
   - Os extremos também são armazenados como o primeiro par de âncoras de balanço, para que as linhas do gráfico possam ser desenhadas imediatamente após o aquecimento.
2. **Acompanhamento de volatilidade**
   - Cada vela finalizada empurra seu alcance para a média móvel selecionada para produzir o limite adaptativo.
   - O limite é sempre de pelo menos uma etapa de preço para evitar envelopes de distância zero.
3. **Manutenção do balanço**
   - Em cada vela, o algoritmo atualiza a oscilação máxima e mínima armazenada sempre que uma nova máxima ou mínima absoluta é impressa.
   - Quando a tendência muda, o extremo relevante é registrado como um pivô e, se o gráfico estiver habilitado, conectado ao pivô oposto por uma linha.
4. **Regras de discussão**
   - Regime de alta (`_isTrendUp == true`): um fechamento abaixo de `_highestPrice - threshold` desencadeia uma reversão para venda. O tamanho do pedido é igual a `Volume + |Position|`, portanto a exposição existente é nivelada e uma nova posição curta é aberta em uma chamada.
   - Regime de baixa (`_isTrendUp == false`): um fechamento acima de `_lowestPrice + threshold` reflete a lógica e reverte para longo.
5. **Parar gerenciamento**
   - Quando `StopLossMultiplier` é maior que zero, o preço de entrada é compensado por `threshold * StopLossMultiplier` (limitado a pelo menos três etapas de preço) para derivar um nível de stop sintético.
   - Se uma vela perfurar o stop longo com sua mínima ou o stop curto com sua máxima, a posição será achatada por meio de uma ordem de mercado.
6. **Infraestrutura**
   - `StartProtection()` ativa mecanismos de segurança StockSharp integrados assim que a estratégia é lançada.
   - Todas as ações são conduzidas por velas finalizadas para emular o ciclo de recálculo barra por barra do consultor especialista original.

## Diferenças da versão MQL
- O especialista MetaTrader recalcula a cada tick, enquanto esta porta opera em velas concluídas porque as assinaturas de velas são a fonte de dados idiomática no nível superior API.
- Restrições específicas do corretor, como `MODE_STOPLEVEL`, não estão disponíveis; em vez disso, a compensação do stop é limitada por três etapas de preço para fornecer uma alternativa conservadora.
- Os pedidos são revertidos combinando as quantidades de fechamento e abertura em uma única chamada `BuyMarket`/`SellMarket` em vez de iterar sobre as posições existentes.
- A visualização depende de StockSharp gráficos primitivos (`DrawLine`) em vez de objetos de plataforma, mas o arranjo das linhas pivô a pivô corresponde à saída do indicador original.

## Notas para uso
- Certifique-se de que a segurança selecionada exponha um `PriceStep` válido. Quando ausente, o código padrão é `1`, o que pode precisar de ajustes para determinados instrumentos.
- Como a estratégia depende de velas concluídas, prazos extremamente pequenos reduzem a confiabilidade da estimativa de volatilidade. Considere alinhar `CandleType` com o período originalmente usado pelo EA (H4 por padrão).
- As paradas são opcionais. Deixar `StopLossMultiplier` em zero replica o gerenciamento de risco ilimitado do script MQL.
- O algoritmo segue tendências por design e não impõe metas de lucro; as saídas ocorrem apenas por reversão de regime ou ativação de stop-loss.
