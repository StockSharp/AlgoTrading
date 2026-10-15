# Estratégia de reversão à média Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão do MetaTrader consultor especialista `MeanReversion.mq5`. Ela negocia um padrão simples de reversão à média: sempre que o preço imprime um novo mínimo dentro da janela de lookback selecionada, a estratégia abre uma posição longa, visando o ponto médio da faixa recente. Quando uma nova máxima aparece, a estratégia reflete a lógica do lado vendido. O tamanho da posição é calculado a partir da porcentagem de risco e da distância de parada, replicando de perto o cálculo do lote executado pelo EA original.

## Lógica de negociação
1. Crie um canal Donchian usando o tipo de vela configurado e o período de lookback. A faixa superior marca o máximo mais alto e a faixa inferior marca o mínimo mais baixo sobre a janela. O ponto médio `(upper + lower) / 2` atua como o alvo de reversão à média.
2. Se a vela finalizada atual atingir um novo mínimo (`Low <= LowerBand`) e nenhuma posição estiver aberta, a estratégia compra no mercado. O stop protetor é refletido em torno do preço de entrada para que o ponto médio se torne a meta de lucro, correspondendo ao cálculo MetaTrader `sl = 2 * Ask - tp`.
3. Se a vela atingir uma nova máxima (`High >= UpperBand`) e nenhuma posição estiver aberta, a estratégia vende a mercado com um stop simétrico acima do preço. O ponto médio novamente atua como o nível de lucro.
4. O stop-loss e o take-profit são monitorados em cada vela finalizada. Um rompimento além do stop fecha a posição imediatamente, enquanto tocar o ponto médio sai da negociação no alvo pretendido. O estado interno é redefinido automaticamente sempre que a posição é plana.

## Dimensionamento de posições
* O risco por negociação é igual a `Portfolio.CurrentValue * (RiskPercent / 100)`. Se os dados da carteira não estiverem disponíveis, a estratégia volta ao volume negociável mínimo.
* O risco do contrato é medido como `|EntryPrice - StopPrice|`. O volume bruto é `RiskAmount / perUnitRisk` e é normalizado para a etapa de volume do instrumento. As restrições cambiais mínimas e máximas são respeitadas. Quando o volume normalizado é menor que o tamanho mínimo negociável, o mínimo é usado.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo de vela e período usado para construir o canal Donchian. | Período de 15 minutos |
| `LookbackPeriod` | Número de velas usadas para calcular a máxima mais alta e a mínima mais baixa. | 200 |
| `RiskPercent` | Porcentagem do patrimônio do portfólio arriscado por negociação. | 1% |

Todos os parâmetros suportam otimização por meio do otimizador integrado.

## Notas adicionais
* A estratégia negocia apenas uma posição por vez, replicando a guarda `PositionsTotal()>0` da versão MQL.
* Os preços stop-loss e take-profit são mantidos internamente em vez de enviar pedidos separados, o que mantém a lógica próxima do Expert Advisor original, permanecendo compatível com o API de alto nível.
* Quando faltam informações sobre o patrimônio do portfólio ou sobre o volume do instrumento, a estratégia ainda negocia usando o menor volume possível para manter o comportamento determinístico.
