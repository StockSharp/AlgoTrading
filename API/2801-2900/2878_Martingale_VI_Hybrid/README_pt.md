# Estratégia Híbrida Martingale VI (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Martingale VI Hybrid é uma conversão do consultor especializado original do MetaTrader para a API de alto nível do StockSharp. Combina um filtro de médias móveis rápida/lenta com confirmação MACD e escala em posições usando um multiplicador de martingale. A estratégia acumula posições quando o preço se move contra a última entrada por uma distância fixa em pips e unifica o take profit de todo o cluster no nível definido pela ordem mais recente. As saídas globais adicionais incluem lucro fixo em dinheiro, lucro como percentual do capital inicial e um trailing stop em dinheiro.

## Lógica de negociação
1. **Filtro de sinal** – são utilizados os valores da vela anterior para as SMAs rápida e lenta e o histograma MACD. Um ciclo comprado começa quando a SMA rápida estava acima da SMA lenta e a linha principal MACD estava abaixo de sua linha de sinal. Um ciclo vendido começa quando a SMA rápida estava abaixo da SMA lenta enquanto a linha principal MACD estava acima da linha de sinal.
2. **Posição inicial** – quando um novo ciclo começa e nenhuma posição está aberta, a estratégia envia uma ordem a mercado com o `Initial Volume`.
3. **Adições de martingale** – enquanto uma posição está aberta, a estratégia monitora o último preço de entrada. Se o preço se mover contra a posição `Pip Step` pips, adiciona outra ordem a mercado cujo volume é `volume da ordem anterior × Volume Multiplier`. O número de ordens ativas é limitado por `Max Trades`. Quando o limite é atingido e `Close Max Orders` está habilitado, toda a posição é fechada imediatamente.
4. **Take profit compartilhado** – cada nova ordem atualiza o nível comum de take profit para `preço de entrada ± Take Profit (pips)` dependendo da direção. Quando a máxima da vela (para comprados) ou a mínima (para vendidos) toca este nível, todas as ordens são fechadas juntas.
5. **Saídas globais** – o lucro flutuante é continuamente avaliado:
   - Se `Use Money TP` estiver habilitado e o lucro atingir `Money TP`, a posição é fechada.
   - Se `Use Percent TP` estiver habilitado e o lucro atingir `Percent TP` por cento do valor inicial do portfólio, a posição é fechada.
   - Se `Enable Trailing` estiver ativo, um trailing stop em dinheiro é aplicado quando o lucro supera `Trailing Activation`. A posição é fechada se o lucro cair `Trailing Drawdown` a partir do pico.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `Candle Type` | Série de velas primária usada para atualizações de indicadores.
| `Fast MA`, `Slow MA` | Períodos das médias móveis simples que definem o filtro de tendência.
| `MACD Fast`, `MACD Slow`, `MACD Signal` | Parâmetros do indicador MACD usados para confirmação.
| `Initial Volume` | Volume da primeira ordem em um ciclo de martingale.
| `Volume Multiplier` | Multiplicador aplicado ao volume da ordem anterior a cada adição.
| `Max Trades` | Número máximo de ordens simultâneas na sequência de martingale.
| `Take Profit (pips)` | Distância do take profit para cada ordem; a ordem mais recente define o preço de take profit compartilhado.
| `Pip Step` | Movimento de preço contra o ciclo atual que aciona a próxima adição.
| `Use Money TP`, `Money TP` | Habilita e define a meta de lucro na moeda da conta.
| `Use Percent TP`, `Percent TP` | Habilita e define a meta de lucro como percentual do valor inicial do portfólio.
| `Enable Trailing`, `Trailing Activation`, `Trailing Drawdown` | Parâmetros do trailing stop baseado em dinheiro que protege o lucro acumulado.
| `Close Max Orders` | Quando habilitado, toda a posição é fechada assim que o limite de ordens de martingale é atingido.

## Gestão de risco
- A estratégia suporta metas de lucro tanto absolutas quanto baseadas em percentual para travar ganhos antecipadamente.
- O trailing stop em dinheiro evita que a posição devolva mais do que o drawdown configurado após uma sequência lucrativa.
- Limitar o número total de etapas de martingale evita crescimento ilimitado da posição; habilitar `Close Max Orders` força uma saída de emergência quando a sequência atinge seu limite configurado.

## Notas de implementação
- A estratégia usa a API de alto nível `SubscribeCandles` do StockSharp com indicadores vinculados via `BindEx` para MACD e processamento manual para as médias móveis.
- O tamanho do pip é derivado do passo de preço do instrumento, incluindo suporte para cotações de 5 e 3 dígitos.
- Os cálculos de lucro dependem de `Security.PriceStep`, `Security.StepPrice` e `PositionAvgPrice`, garantindo compatibilidade com instrumentos que fornecem os metadados necessários.
