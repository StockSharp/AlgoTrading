# Estratégia de Operações JS MA SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

JS MA SAR Trades converte o especialista do MetaTrader 5 "JS MA SAR Trades" para a API de alto nível do StockSharp. A estratégia procura mínimos de oscilação mais altos ou máximos de oscilação mais baixos detectados por meio de um filtro estilo ZigZag, confirma o impulso com duas médias móveis e, em seguida, entra na direção de um rompimento do Parabolic SAR. As posições são protegidas com stops clássicos, stops trailing opcionais e um filtro de sessão de trading explícito.

## Visão Geral da Lógica

1. **Estrutura de oscilação** – Os indicadores Highest/Lowest com a profundidade configurada aproximam o ZigZag original. Os dois mínimos e máximos de oscilação mais recentes são rastreados. Uma configuração comprada requer que o último mínimo seja mais alto que o anterior (estrutura ascendente), enquanto uma configuração vendida requer que o último máximo seja mais baixo que o anterior (estrutura descendente). Um filtro de desvio (em pips) e um backstep mínimo (barras entre pivôs) impedem que pivôs de ruído sejam aceitos.
2. **Confirmação de média móvel** – Ambas as médias móveis usam o mesmo tipo de suavização e preço aplicado que a versão MT5, incluindo deslocamentos positivos opcionais (barras à direita). Um sinal comprado precisa que a MA rápida deslocada permaneça acima da MA lenta deslocada; um sinal vendido requer a relação oposta.
3. **Gatilho Parabolic SAR** – Uma vez que as condições de oscilação e média móvel são satisfeitas, a operação é executada apenas se a vela fechar além do nível do Parabolic SAR: fechamento acima do SAR para comprados e abaixo para vendidos. Inversões do SAR para o outro lado fecham todas as posições existentes, mesmo fora da janela de entrada.
4. **Gestão de risco** – Os níveis de stop-loss e take-profit são calculados em pips (convertidos pelo passo de preço do instrumento). O trailing stop opcional imita a lógica do MT5: o stop só é deslocado depois que o preço se moveu a distância configurada de trailing stop mais trailing step a partir do preço de entrada.
5. **Filtro de sessão** – Quando ativado, as ordens são permitidas apenas entre as horas de início e fim especificadas (inclusive). As saídas de proteção (stop/take/trailing e reversão SAR) ainda são avaliadas em cada vela finalizada.

## Regras de Entrada e Saída

- **Entrada comprada**: mínimo de oscilação mais alto, Parabolic SAR abaixo do fechamento, MA rápida (com deslocamento) acima da MA lenta, e fechamento dentro da janela de trading. A estratégia compra `OrderVolume + |Position|` para fechar vendidos e abrir a posição comprada.
- **Entrada vendida**: máximo de oscilação mais baixo, Parabolic SAR acima do fechamento, MA rápida (com deslocamento) abaixo da MA lenta, e filtro de tempo satisfeito.
- **Saída comprada**:
  - O preço de fechamento cruza abaixo do Parabolic SAR;
  - O nível de stop-loss, trailing stop ou take-profit é atingido.
- **Saída vendida**:
  - O preço de fechamento cruza acima do Parabolic SAR;
  - O nível de stop-loss, trailing stop ou take-profit é atingido.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `OrderVolume` | `1` | Tamanho base da ordem para novas entradas; a estratégia adiciona a posição atual absoluta para reverter instantaneamente. |
| `StopLossPips` | `50` | Distância em pips entre o preço de entrada e o stop-loss. Defina como `0` para desabilitar. |
| `TakeProfitPips` | `50` | Distância em pips entre o preço de entrada e o take-profit. Defina como `0` para desabilitar. |
| `TrailingStopPips` | `5` | Distância de trailing stop em pips. Funciona junto com `TrailingStepPips`. |
| `TrailingStepPips` | `5` | Distância adicional que o preço deve percorrer (em pips) antes de o trailing stop ser ajustado. Deve ser positivo quando o trailing está habilitado. |
| `UseTimeFilter` | `true` | Habilitar o filtro de hora de início/fim para novas entradas. |
| `StartHour` | `19` | Início da janela de trading (inclusive, horário da bolsa). |
| `EndHour` | `22` | Fim da janela de trading (inclusive). |
| `FastMaPeriod` | `55` | Período da média móvel rápida. |
| `FastMaShift` | `3` | Deslocamento à frente (em barras) aplicado aos valores da média móvel rápida. |
| `SlowMaPeriod` | `120` | Período da média móvel lenta. |
| `SlowMaShift` | `0` | Deslocamento à frente (em barras) para a média móvel lenta. |
| `MaType` | `Smoothed` | Método de suavização da média móvel (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `Median` | Fonte de preço para ambas as médias móveis (Close, Open, High, Low, Median, Typical, Weighted). |
| `SarStep` | `0.02` | Fator de aceleração inicial do Parabolic SAR. |
| `SarMax` | `0.2` | Fator de aceleração máximo do Parabolic SAR. |
| `ZigZagDepth` | `12` | Janela de retrospectiva (barras) para detecção de oscilação. |
| `ZigZagDeviation` | `5` | Tamanho mínimo de oscilação medido em pips para aceitar um novo pivô. |
| `ZigZagBackstep` | `3` | Número mínimo de barras entre pivôs consecutivos do mesmo tipo. |
| `CandleType` | `H1` | Período de trading para assinatura de velas. |

## Notas

- A estratégia mantém a lógica de proteção ativa mesmo fora da janela de entrada, garantindo que stops e inversões de SAR sejam respeitados.
- O trailing stop reproduz a implementação do MT5: uma vez que o preço avança `TrailingStop + TrailingStep`, o stop é movido para `Close - TrailingStop` para comprados (espelhado para vendidos).
- As médias móveis são avaliadas no preço aplicado selecionado; o deslocamento emula o offset do indicador MT5.
- Certifique-se de que o instrumento tenha um `PriceStep` válido, caso contrário as distâncias baseadas em pips são ignoradas.
