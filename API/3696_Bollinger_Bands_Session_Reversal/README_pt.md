# Bollinger Reversão de sessão de bandas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão C# do consultor especialista MetaTrader **BollingerBandsEA (ver. 3.0)**. Ele negocia configurações de reversão à média que ocorrem após o preço ultrapassar as bandas Bollinger durante a sessão de negociação ativa.

## Lógica de negociação

1. Assine a série primária de velas intradiárias (velas de 15 minutos por padrão) e uma série diária de velas usada para construir o filtro de tendência.
2. Calcule Bollinger bandas (comprimento 20, largura 2,0) na série intradiária e um período de 100 SMA nos fechamentos diários.
3. Acompanhe os máximos/mínimos do dia atual e anterior e mantenha os valores da banda Bollinger anteriores para avaliação do sinal.
4. Permitir entradas apenas dentro da janela da sessão de negociação: de `SessionStartOffsetMinutes` após a abertura do dia de negociação até `SessionEndOffsetMinutes` antes do final do dia de negociação.
5. Ignore a negociação assim que o PnL cumulativo do dia atual se tornar positivo, imitando o stop diário de EA.
6. Entre em posição curta quando a vela anterior for de baixa, fechada acima da banda superior, o fechamento atual permanecer acima dessa banda, a largura da banda for ampla o suficiente, o preço estiver abaixo do diário SMA e o preço for negociado acima da alta diária atual ou anterior.
7. Entre em posição comprada quando a vela anterior for de alta, fechada abaixo da banda inferior, o fechamento atual permanecer abaixo dessa banda, a largura da banda for ampla o suficiente, o preço estiver acima do diário SMA e o preço for negociado abaixo da mínima diária atual ou anterior.
8. O tamanho da posição é determinado pelo volume fixo configurado ou pelo dimensionamento baseado em risco que utiliza a distância até o stop loss em pontos.
9. As saídas são realizadas verificando stop-loss, take-profit, fechamento opcional na banda intermediária, um trailing stop opcional e a lógica opcional de ponto de equilíbrio. As negociações perdedoras também podem ser liquidadas após um tempo de retenção configurável.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Série de velas intradiárias usadas para negociação. |
| `BollingerLength` | Período da média móvel das bandas Bollinger. |
| `BollingerWidth` | Multiplicador de largura das bandas Bollinger. |
| `DailyMaLength` | Duração do filtro diário SMA. |
| `StopLossPoints` | Distância de stop-loss expressa em pontos do instrumento. |
| `UseRiskVolume` | Permite dimensionamento de posição baseado em risco. |
| `RiskPercent` | Porcentagem da conta usada para dimensionamento baseado em risco. |
| `FixedVolume` | Volume fixo de fallback quando o dimensionamento de risco está desabilitado ou não é possível. |
| `SessionStartOffsetMinutes` | Minutos após o início da sessão antes que as entradas sejam permitidas. |
| `SessionEndOffsetMinutes` | Minutos antes do final da sessão, quando as entradas são bloqueadas. |
| `CloseOnMiddleBand` | Posição de saída quando o preço cruzar a banda intermediária Bollinger. |
| `EnableTrailing` | Permite ajustes de trailing stop. |
| `TrailingFactor` | Multiplicador de distância necessário antes de seguir a parada. |
| `EnableBreakEven` | Permite mover o stop para o preço de entrada. |
| `BreakEvenFactor` | Múltiplo de lucro necessário para mover o stop para o ponto de equilíbrio. |
| `CloseLosingAfterMinutes` | Fecha negociações perdedoras após mantê-las durante os minutos especificados. |

## Notas

- As ordens protetoras de stop-loss e take-profit são simuladas verificando os extremos das velas em cada atualização. Ajuste esta seção se forem necessárias ordens de proteção do lado da bolsa.
- O dimensionamento baseado em risco depende de `Security.Step` e `Security.StepPrice`. Se esses valores estiverem faltando, a estratégia voltará ao volume fixo.
- O stop de lucro diário utiliza a estratégia PnL, portanto o PnL realizado e flutuante precisam estar na mesma moeda da carteira.
