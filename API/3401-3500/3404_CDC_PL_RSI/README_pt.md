# Estratégia CDC PL RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **CDC PL RSI** replica o MQL Expert Advisor *Expert_ADC_PL_RSI* dentro do ecossistema StockSharp. O sistema verifica as velas finalizadas em busca de padrões de reversão de velas japonesas e confirma as entradas com o Índice de Força Relativa (RSI). As negociações longas dependem do padrão *Piercing Line* durante condições de sobrevenda RSI, enquanto as negociações curtas exigem o padrão *Dark Cloud Cover* combinado com leituras de sobrecompra RSI. A abordagem mantém o conceito original de gerenciamento de dinheiro simples, usando o volume da estratégia e deixando StockSharp lidar com o dimensionamento da posição.

## Lógica de padrões e indicadores
- **Padrões de velas**: A estratégia reconstrói a lógica MetaTrader analisando as duas últimas velas concluídas. As regras Piercing Line e Dark Cloud Cover refletem o código original, incluindo verificações de lacunas, corpos longos em relação a uma média corporal adaptativa e a direção da tendência subjacente.
- **RSI filtro**: um RSI de 20 períodos (otimizável) confirma o impulso. Leituras de sobrevenda (`RSI < 40`) desbloqueiam entradas longas e leituras de sobrecompra (`RSI > 60`) desbloqueiam entradas curtas. O histórico RSI também é usado para detectar saídas quando o oscilador cruza os 30 ou 70 níveis na direção oposta.
- **Média corporal e filtro de tendência**: uma média móvel simples dos tamanhos dos corpos das velas e outro SMA de preços de fechamento replicam as funções auxiliares MetaTrader (`AvgBody` e `CloseAvg`). Essas médias evitam sinais durante o ruído e garantem que os padrões apareçam após um movimento claro.

## Regras de negociação
### Configuração longa
1. Detecte um padrão de linha perfurante nas duas últimas velas concluídas.
2. Exige que RSI da vela finalizada anterior esteja abaixo de 40.
3. Se as condições se mantiverem, compre no mercado. Quando existe uma posição oposta, a estratégia inverte comprando o tamanho absoluto da posição mais o volume configurado.

### Configuração curta
1. Detecte um padrão de Dark Cloud Cover nas duas velas mais recentes.
2. Exige que RSI da vela finalizada anterior esteja acima de 60.
3. Se as condições se mantiverem, venda no mercado. Uma posição oposta é fechada e revertida usando a mesma lógica de volume.

### Condições de saída
- Feche as posições longas quando RSI cruzar para baixo até 70 ou cruzar para cima até 30, sinalizando que o impulso diminuiu ou foi revertido.
- Feche as posições curtas quando RSI ultrapassar 30 ou descer até 70, espelhando a implementação de MetaTrader.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `RsiPeriod` | 20 | RSI comprimento de retrospectiva. Otimizável entre 10 e 40 em passos de 5. |
| `BodyAveragePeriod` | 14 | Período para o tamanho médio do corpo da vela e para o filtro de tendência de preço de fechamento. Otimizável entre 10 e 30 em passos de 5. |
| `CandleType` | Período de 1 hora | Série de velas usadas para cálculos. Qualquer tipo de vela compatível com StockSharp pode ser selecionado. |
| `Volume` (classe base) | - | Volume de negociação definido na instância da estratégia antes do lançamento. |

## Uso
1. Anexe a estratégia a um portfólio e segurança em StockSharp Designer, Shell ou Runner.
2. Configure o tipo e o volume da vela de acordo com o mercado que está sendo negociado.
3. Opcionalmente, ajuste os períodos RSI e a média corporal para corresponder à volatilidade do instrumento ou execute otimizações usando o StockSharp Optimizer.
4. Inicie a estratégia e monitore as sobreposições do gráfico (velas, RSI e linha de média próxima) para revisar as confirmações de padrão e as negociações executadas.

## Notas
- A estratégia chama `StartProtection()` para que as rotinas de proteção integradas possam ser configuradas, se necessário (stop-loss, take-profit, trailing, etc.).
- Apenas velas concluídas são processadas, mantendo a lógica consistente com o Expert Advisor MQL.
- Nenhuma coleção adicional é armazenada; instâncias de indicadores carregam os cálculos de janela deslizante necessários para as verificações de padrão.
