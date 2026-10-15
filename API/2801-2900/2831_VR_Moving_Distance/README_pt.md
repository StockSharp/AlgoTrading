# Estratégia VR Moving Distance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia do StockSharp replica o consultor especialista VR-Moving do MetaTrader 5. Ela monitora uma média móvel configurável e reage quando o preço se afasta além de uma distância fixa em pips. O algoritmo pode escalar em tendências multiplicando o volume base da ordem para negociações subsequentes e aplica lógica simples de take profit enquanto apenas uma posição está aberta.

## Visão geral
- Opera o instrumento atribuído à estratégia usando uma única assinatura de candles.
- Calcula uma média móvel com comprimento, tipo de suavização e fonte de preço selecionáveis.
- Converte as configurações de distância e take profit de pips em deslocamentos de preço usando o passo de preço do ativo.
- Adiciona posições compradas quando o preço sobe o suficiente acima da média móvel, ou posições vendidas quando o preço cai abaixo dela.
- Reverte a exposição líquida atual antes de abrir uma posição na direção oposta para manter a estratégia compatível com o netting do portfólio.

## Indicadores e dados
- Uma média móvel (`Simple`, `Exponential`, `Smoothed`, `Weighted` ou `VolumeWeighted`).
- Os candles chegam com o `Candle Type` configurado; o mesmo fluxo alimenta os valores do indicador e as decisões de negociação.

## Lógica de entrada
1. A cada candle finalizado a estratégia aguarda a média móvel estar completamente formada.
2. Se a máxima da barra estiver pelo menos `DistancePips` acima da média móvel, uma entrada comprada é acionada.
3. Se a mínima da barra estiver pelo menos `DistancePips` abaixo da média móvel, uma entrada vendida é acionada.
4. Ao mudar de direção a estratégia fecha a exposição existente adicionando o volume oposto à nova ordem a mercado.

## Escalonamento e gestão de volume
- A primeira ordem usa o `BaseVolume` configurado.
- Ordens subsequentes na mesma direção usam `BaseVolume * VolumeMultiplier`.
- O maior preço executado no lado comprado e o menor no lado vendido são rastreados. Cada nova ordem de escalonamento requer que o preço se estenda mais `DistancePips` a partir desse extremo antes de ser disparada.

## Lógica de saída
- Quando exatamente uma posição comprada está aberta, um alvo de lucro é colocado no preço de entrada mais `TakeProfitPips` (convertidos em unidades de preço). Se a máxima de um candle tocar o alvo, a posição é fechada.
- Da mesma forma, uma única posição vendida recebe um alvo de lucro na entrada menos `TakeProfitPips` e fecha quando a mínima do candle o toca.
- Uma vez que múltiplas entradas existam a estratégia mantém as posições abertas e aguarda novos sinais de escalonamento; nenhuma saída média é tentada neste port.

## Notas de gestão de risco
- `StartProtection()` é ativado na inicialização para se conectar aos subsistemas de proteção padrão do StockSharp.
- Os valores de distância e take profit são medidos em pips. Para símbolos cotados com 3 ou 5 casas decimais a estratégia multiplica o passo de preço por 10 para corresponder à semântica de pips do MetaTrader.
- Não há stop-loss automático; o risco deve ser controlado através dos parâmetros escolhidos e limites externos do portfólio.

## Parâmetros
- **Candle Type** – Tipo de dados usado para assinatura de candles.
- **MA Length** – Período da média móvel.
- **MA Type** – Método de suavização da média móvel.
- **Price Source** – Preço do candle usado para calcular a média móvil.
- **Distance (pips)** – Lacuna mínima em pips entre o preço e a média móvel para acionar entradas.
- **Take Profit (pips)** – Distância do alvo de lucro aplicada quando apenas uma posição está aberta.
- **Volume Multiplier** – Multiplicador aplicado ao volume base para entradas adicionais.
- **Base Volume** – Quantidade da negociação inicial.
