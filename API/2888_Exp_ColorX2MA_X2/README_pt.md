# Estratégia Exp ColorX2MA X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia recria o especialista de duplo período "Exp_ColorX2MA_X2" para o StockSharp. Ela utiliza dois filtros ColorX2MA: um mapa de tendência no período superior e um gatilho de entrada no período inferior. Ambos os valores ColorX2MA são construídos em cascata com duas médias móveis configuráveis e, em seguida, coloridos de acordo com a inclinação atual. As decisões de trading são executadas quando a cor do período inferior muda na direção da tendência do período superior.

A implementação suporta as opções de preço aplicado originais e os modos de suavização mais comuns (SMA, EMA, SMMA, LWMA, Jurik). Quando o indicador Jurik expõe uma propriedade `Phase`, ela é atualizada com o valor de fase configurado.

## Regras de Trading
- **Entrada comprado**
  - A cor ColorX2MA do período superior é de alta (trend direction > 0).
  - A cor ColorX2MA do período inferior mudou de alta na barra anterior para neutro ou de baixa na última barra completada (`Clr[1] == 1` e `Clr[0] != 1`).
  - O trading comprado está habilitado.
- **Entrada vendido**
  - A cor ColorX2MA do período superior é de baixa (trend direction < 0).
  - A cor ColorX2MA do período inferior mudou de baixa na barra anterior para neutro ou de alta na última barra completada (`Clr[1] == 2` e `Clr[0] != 2`).
  - O trading vendido está habilitado.
- **Saída comprado**
  - Quando uma cor de baixa aparece no período inferior (`Clr[1] == 2`) e a permissão de fechamento de compra secundária está habilitada, **ou** a tendência do período superior vira de baixa enquanto a permissão de fechamento de compra primária está habilitada.
- **Saída vendido**
  - Quando uma cor de alta aparece no período inferior (`Clr[1] == 1`) e a permissão de fechamento de venda secundária está habilitada, **ou** a tendência do período superior vira de alta enquanto a permissão de fechamento de venda primária está habilitada.
- **Stops**
  - As distâncias opcionais de stop loss e take profit são especificadas em pontos (multiplicadas pelo passo de preço do instrumento). São avaliadas em cada vela de sinal finalizada comparando os extremos da vela com o preço médio da posição.

## Valores padrão
- **Período de tendência**: velas de 6 horas.
- **Período de sinal**: velas de 30 minutos.
- **Suavização de tendência**: SMA(12) alimentando Jurik(5, fase 15).
- **Suavização de sinal**: SMA(12) alimentando Jurik(5, fase 15).
- **Preço aplicado**: Fechamento.
- **Deslocamento de sinal**: 1 barra em ambos os períodos.
- **Permissões**: entradas e saídas comprado/vendido habilitadas.
- **Stop loss**: 1000 pontos (convertido usando o passo de preço).
- **Take profit**: 2000 pontos (convertido usando o passo de preço).

## Filtros e Notas
- Direção: opera comprado e vendido, controlado via flags de permissão.
- Período: duplo período (tendência no HTF, entradas no LTF).
- Indicadores: ColorX2MA de dois níveis com métodos de suavização configuráveis.
- Suporte de suavização: `Sma`, `Ema`, `Smma`, `Lwma`, `Jurik`. Outros modos da biblioteca original não estão implementados.
- Preços aplicados: todas as 12 fórmulas originais incluindo preços TrendFollow e Demark.
- Stops: stop loss e take profit opcional a distância fixa.
- Complexidade: intermediário porque sincroniza dois períodos e buffers de cor.
- Adequado para: configurações de seguidor de tendência em FX, índices ou cripto onde o indicador ColorX2MA é preferido.

## Dicas de Uso
- Manter o período superior significativamente maior que o período de sinal para evitar whipsaws frequentes.
- Ajustar o parâmetro de deslocamento de sinal (`SignalSignalBar`) para reagir mais rápido ou aumentá-lo para suavizar mais.
- Se o instrumento não fornece `PriceStep`, as distâncias de stop/take são interpretadas diretamente em unidades de preço.
- A suavização Jurik requer um pacote de indicadores StockSharp licenciado; quando indisponível, a estratégia ainda funciona com as outras opções de suavização.
