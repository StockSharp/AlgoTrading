# Estratégia BykovTrend + ColorX2MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o indicador de tendência de cor BykovTrend V2 com o filtro de inclinação de média móvel de dupla suavização ColorX2MA. Ambos os blocos lógicos operam no mesmo símbolo e podem emitir ordens de forma independente, o que permite que a posição líquida reflita o último acordo entre as duas fontes de sinal.

## Visão geral

- **Viés de mercado**: Funciona em qualquer instrumento que suporte dados de candles. O período padrão para ambos os blocos é 4 horas (H4), espelhando o Consultor Especialista original.
- **Indicadores**:
  - *BykovTrend V2*: Usa Williams %R para colorir os candles de acordo com a tendência predominante.
  - *ColorX2MA*: Aplica duas médias móveis consecutivas a uma fonte de preço configurável e classifica a direção da inclinação.
- **Sinais**: Entradas e saídas são geradas separadamente por cada bloco. A posição final é a soma de todos os trades executados.

## Bloco BykovTrend

1. Williams %R é calculado usando o período configurado (padrão 9).
2. Os limiares são deslocados por `33 - Risk`. Quando %R sobe acima de `-Risk`, a tendência local se torna altista; quando cai abaixo de `-100 + (33 - Risk)`, a tendência se torna baixista.
3. Cores de candle:
   - Verde/teal (códigos 0, 1): tendência altista.
   - Cinza (código 2): neutro, sem mudança de tendência.
   - Chocolate/dourado (códigos 3, 4): tendência baixista.
4. Os sinais são avaliados no candle que está `SignalBar` passos atrás da última barra fechada. Um valor de 1 significa o candle completado anterior, que corresponde à implementação MetaTrader.
5. Lógica de trading:
   - **Entrada comprada**: Cor atual < 2 (altista) e cor anterior > 1 (era neutro/baixista). Opcional via *Bykov Allow Long Entries*.
   - **Saída vendida**: Cor atual < 2. Opcional via *Bykov Allow Short Exits*.
   - **Entrada vendida**: Cor atual > 2 (baixista) e cor anterior < 3 (era neutro/altista). Opcional via *Bykov Allow Short Entries*.
   - **Saída comprada**: Cor atual > 2. Opcional via *Bykov Allow Long Exits*.

## Bloco ColorX2MA

1. Uma primeira média móvel suaviza o preço aplicado selecionado (fechamento por padrão) usando o método e comprimento escolhidos.
2. Uma segunda média móvel suaviza a saída da primeira MA, novamente com método e comprimento configuráveis.
3. A inclinação da segunda suavização define o fluxo de cor:
   - 1 (magenta): o valor aumentou desde o candle anterior.
   - 2 (violeta): o valor diminuiu.
   - 0 (cinza): sem mudança.
4. Os sinais são avaliados no candle que está `SignalBar` passos atrás do último fechamento.
5. Lógica de trading:
   - **Entrada comprada**: Cor atual = 1 e cor anterior ≠ 1. Opcional via *Color Allow Long Entries*.
   - **Saída vendida**: Cor atual = 1. Opcional via *Color Allow Short Exits*.
   - **Entrada vendida**: Cor atual = 2 e cor anterior ≠ 2. Opcional via *Color Allow Short Entries*.
   - **Saída comprada**: Cor atual = 2. Opcional via *Color Allow Long Exits*.

## Gestão de Posição

- As ordens são ordens de mercado. Ao mudar de direção, a estratégia compra/vende contratos suficientes para neutralizar a posição existente e estabelecer uma nova de tamanho `Volume`.
- Cada bloco pode acionar uma saída mesmo se o outro bloco ainda favorece o lado atual; o efeito líquido é uma luta gradual entre os dois módulos.
- Nenhum stop-loss ou take-profit automático é aplicado. O gerenciamento de risco deve ser tratado externamente ou ajustando os flags de permissão.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **BykovTrend Candle** | Tipo de dados (período) para o cálculo BykovTrend. |
| **Williams %R Period** | Retrospectiva para Williams %R. |
| **Risk Offset** | Desloca os limiares de Williams %R (`33 - Risk`). Valores maiores apertam os limiares altistas e afrouxam os baixistas. |
| **Signal Bar** | Atraso (número de candles completados) antes de agir sobre uma cor BykovTrend. |
| **Allow Long/Short Entries** | Habilitar ou desabilitar entradas impulsionadas por BykovTrend. |
| **Allow Long/Short Exits** | Habilitar ou desabilitar saídas impulsionadas por BykovTrend. |
| **ColorX2MA Candle** | Tipo de dados (período) para o bloco ColorX2MA. |
| **First/Second MA Method** | Método de suavização para cada etapa (SMA, EMA, SMMA, LWMA, Jurik). |
| **First/Second MA Length** | Comprimento de período para cada etapa de suavização. |
| **First/Second MA Phase** | Parâmetro de compatibilidade mantido do EA original; a implementação atual o mantém para documentação, mas a suavização Jurik usa seus padrões internos. |
| **Applied Price** | Fonte de preço para ColorX2MA (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado, simples, quarto, variações de seguimento de tendência, DeMark). |
| **Color Signal Bar** | Atraso antes de agir sobre as cores ColorX2MA. |
| **Allow Long/Short Entries/Exits** | Habilitar ou desabilitar ações impulsionadas por ColorX2MA. |

## Notas e Limitações

- Apenas os tipos de média móvel disponíveis no StockSharp são suportados. Suavizações exóticas da biblioteca MetaTrader (JurX, Parabolic, T3, VIDYA, AMA) não são reproduzidas; escolher entre SMA, EMA, SMMA, LWMA ou Jurik.
- Os parâmetros de fase são preservados como referência, mas não alteram os indicadores incorporados do StockSharp.
- A estratégia assume que a propriedade `Volume` está configurada; caso contrário, as entradas não colocarão ordens.
- Como ambos os módulos podem operar de forma independente, o fluxo de ordens resultante pode diferir das instalações MetaTrader que segregam trades por números mágicos.
