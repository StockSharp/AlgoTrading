# Estratégia do Sistema de Lucro Forex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reproduz o clássico MetaTrader consultor especialista "Forex Profit System" dentro do StockSharp API de alto nível. Ele usa
três médias móveis exponenciais (EMA 10, 25 e 50) no preço médio da vela combinadas com um filtro Parabolic SAR. O
A combinação detecta explosões de impulso de curta duração que aparecem depois que a média rápida cruza a linha de tendência lenta enquanto o Parabolic
SAR já mudou para o mesmo lado do preço.

## Lógica de negociação

1. **Pilha de indicadores**
   - O preço médio derivado da vela finalizada impulsiona todos os indicadores para que os resultados correspondam ao original MetaTrader "PRICE_MEDIAN"
entrada.
   - EMA rápida (comprimento 10) reage rapidamente às mudanças de impulso de curto prazo.
   - Médio EMA (comprimento 25) e EMA lenta (comprimento 50) definem o viés direcional.
   - Parabolic SAR com etapa 0,02 e máximo 0,2 confirma que o preço já quebrou para o novo lado da tendência.
2. **Entrada longa**
   - EMA(10) é maior que EMA(25) e EMA(50).
   - EMA(10) estava abaixo de EMA(50) na vela fechada anterior (confirmação cruzada).
   - O valor Parabolic SAR está abaixo do fechamento da vela, o que significa que os pontos mudaram para o modo de alta.
   - Não existe posição aberta e a estratégia pode ser negociada (online + permissões).
3. **Entrada curta**
   - EMA(10) é inferior a EMA(25) e EMA(50).
   - EMA(10) estava acima de EMA(50) na vela fechada anterior (confirmação cruzada).
   - Parabolic SAR está acima do fechamento da vela.
4. **Gerenciamento de saídas**
   - O hard stop loss e o take-profit são aplicados imediatamente após a entrada com configurações assimétricas para negociações longas e curtas.
   - Um trailing stop é armado quando o preço se move o suficiente a favor da posição. A parada é puxada para `current price -/+ trailing`
distância dependendo da direção.
   - A saída antecipada ocorre quando EMA(10) inverte a direção (cai abaixo de seu valor anterior para posições compradas ou sobe acima para posições vendidas) e o
o lucro aberto excede uma distância mínima de disparo.

## Valores de parâmetro padrão

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Período de 15 minutos | Série de velas processada pela estratégia. |
| `FastEmaLength` | 10 | Período do EMA rápida. |
| `MediumEmaLength` | 25 | Período do meio EMA. |
| `SlowEmaLength` | 50 | Período da lentidão EMA. |
| `SarStep` | 0,02 | Aceleração inicial para Parabolic SAR. |
| `SarMax` | 0,2 | Aceleração máxima para Parabolic SAR. |
| `Volume` | 0,1 | Volume negociado em lotes/contratos. |
| `LongTakeProfitPoints` | 50 | Distância de lucro para negociações longas, medida em faixas de preço. |
| `ShortTakeProfitPoints` | 50 | Distância de lucro para negociações curtas, medida em faixas de preço. |
| `LongStopLossPoints` | 30 | Distância de stop-loss para negociações longas, medida em faixas de preço. |
| `ShortStopLossPoints` | 30 | Distância de stop-loss para negociações curtas, medida em faixas de preço. |
| `LongTrailingStopPoints` | 10 | Distância de acionamento do trailing stop para negociações longas. |
| `ShortTrailingStopPoints` | 10 | Distância de acionamento do trailing stop para negociações curtas. |
| `LongProfitTriggerPoints` | 10 | Lucro mínimo aberto (pontos) necessário antes que uma negociação longa possa ser fechada na reversão de EMA. |
| `ShortProfitTriggerPoints` | 5 | Lucro mínimo aberto (pontos) necessário antes que uma negociação curta possa ser fechada na reversão de EMA. |

## Notas de implementação

- A estratégia usa assinaturas de velas e vinculação de indicadores no API de alto nível, mantendo todo o controle de risco dentro do
aula de estratégia. Nenhum acesso de baixo nível ao livro de pedidos é necessário.
- Todas as distâncias de gerenciamento comercial são convertidas de pontos em compensações de preços reais usando o instrumento `PriceStep`. Se `PriceStep`
não está disponível, o valor do ponto bruto é usado, portanto o algoritmo ainda funciona em instrumentos sintéticos.
- Paradas de proteção (`SetStopLoss`, `SetTakeProfit`) são definidas usando a posição resultante após a ordem de mercado ser enviada para permanecer em
sincronizar com possíveis preenchimentos parciais.
- O estado interno monitora o último preço de entrada por direção para que as saídas finais e baseadas em EMA possam avaliar o realizado
progredir com precisão.
- Como toda lógica é executada em velas concluídas, não há risco de repintura e os sinais refletem o comportamento original MetaTrader que
calculou tudo em `start()` preços de fechamento.

## Uso sugerido

- O método é adequado para pares de FX líquidos em gráficos intradiários (padrão de 15 minutos). Prazos maiores podem ser usados ajustando o
EMA períodos e distâncias de gerenciamento comercial de acordo.
- Para ativos com diferentes tamanhos de tick ou níveis de volatilidade, ajuste os parâmetros baseados em pontos (`StopLoss`, `TakeProfit`,
`TrailingStop`, `ProfitTrigger`) para que as distâncias correspondam ao perfil do instrumento.
- Combine com filtros de spread ou sessão se o local tiver grandes spreads durante determinados horários; a estratégia espera razoável
execução para concretizar as explosões de impulso de curto prazo.
