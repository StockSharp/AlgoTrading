# Estratégia de Rompimento AIS1 EURUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especializado original AIS1 "A System: EURUSD Daily Metrics" usando a API de alto nível do StockSharp. Opera rompimentos de EURUSD comparando a ação do preço atual com o intervalo do dia anterior e gerencia operações com dimensionamento adaptativo de posição mais um trailing stop de quatro horas.

## Visão geral da estratégia

- **Mercado**: Instrumentos spot/CFD/forex de EURUSD.
- **Período primário**: As velas diárias fornecem o máximo, mínimo e fechamento de referência.
- **Período secundário**: As velas de 4 horas impulsionam as atualizações do trailing stop e as verificações de entrada.
- **Direção**: Operações compradas e vendidas são permitidas.
- **Estilo**: Continuação de rompimento com alvos e stops escalados por volatilidade.

## Lógica de trading

1. Rastrear a vela diária anterior completada. Calcular o ponto médio, o intervalo e as distâncias derivadas de stop/take usando multiplicadores configuráveis (`StopFactor`, `TakeFactor`).
2. Avaliar cada vela de 4 horas completada:
   - **Entrada comprada**: O fechamento diário anterior está acima do ponto médio e o máximo de 4 horas rompe acima do máximo diário anterior.
   - **Entrada vendida**: O fechamento diário anterior está abaixo do ponto médio e o mínimo de 4 horas rompe abaixo do mínimo diário anterior.
3. O tamanho da posição é determinado a partir do patrimônio atual do portfólio e da participação de risco configurada (`OrderReserve`). O volume é arredondado para os passos de negociação do instrumento.
4. Para posições abertas, a estratégia aplica três camadas de controle de saída:
   - Stop-loss fixo no lado oposto do intervalo diário escalado por `StopFactor`.
   - Take-profit fixo a uma distância de `TakeFactor` × intervalo diário.
   - Trailing stop dinâmico usando o intervalo de 4 horas anterior multiplicado por `TrailFactor`. O trailing stop se ativa apenas depois que a operação se move em lucro.
5. Um período de espera de cinco segundos após qualquer operação ou saída reflete o comportamento do EA original e previne modificações rápidas.

## Gerenciamento de risco

- `OrderReserve` define a fração do patrimônio atual que pode ser arriscada na próxima operação. Se o tamanho calculado estiver abaixo do mínimo do instrumento, a operação é ignorada.
- `AccountReserve` rastreia o patrimônio de pico e para de abrir ou gerenciar operações assim que o drawdown do patrimônio excede `AccountReserve - OrderReserve` (16% com os parâmetros padrão).
- As saídas de trailing e os alvos fixos garantem que as posições sejam fechadas mesmo que novas operações sejam bloqueadas pelo guard de drawdown.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `AccountReserve` | Porção do patrimônio excluída do trading, usada para calcular o drawdown permitido antes de o trading pausar. |
| `OrderReserve` | Participação do patrimônio arriscada por operação. Determina a perda máxima usando a distância do stop. |
| `TakeFactor` | Multiplicador aplicado ao intervalo diário anterior para definir a distância do take-profit. |
| `StopFactor` | Multiplicador aplicado ao intervalo diário anterior para definir a distância do stop-loss. |
| `TrailFactor` | Multiplicador aplicado ao intervalo de 4 horas anterior para mover o trailing stop assim que a posição for lucrativa. |
| `EntryCandleType` | Tipo de vela (diário por padrão) usado para os níveis de rompimento. |
| `TrailCandleType` | Tipo de vela (4 horas por padrão) usado para avaliação intradiária e trailing. |

## Notas sobre a conversão

- A versão StockSharp aciona entradas e atualizações de trailing em velas de 4 horas completadas. O consultor especializado MQL original reagia a cada tick; usar velas mantém a lógica robusta dentro da API de alto nível.
- Stop-loss, take-profit e saídas de trailing são executados com ordens de mercado quando os respectivos níveis de preço são tocados dentro da vela processada.
- As verificações de margem da versão MQL são substituídas por dimensionamento baseado em patrimônio para permanecer neutro à plataforma enquanto respeita as restrições de risco originais.
