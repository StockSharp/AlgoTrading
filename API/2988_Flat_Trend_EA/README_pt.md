# Estratégia Flat Trend EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Flat Trend EA Strategy é um port StockSharp do consultor especialista MQL5 "Flat Trend EA". O algoritmo combina o indicador Parabolic SAR com o Índice de Direção Médio (ADX) para detectar quatro estados de mercado: tendência de alta, tendência de baixa, fim de compra e fim de venda. A estratégia reage apenas a candles completados do período configurado e espelha a lógica original de fechar posições opostas antes de abrir uma nova.

## Lógica de negociação
- **Sinal de compra**: o ponto do Parabolic SAR é impresso abaixo do preço de fechamento e a linha +DI do ADX está acima da linha -DI. Qualquer exposição vendida é fechada imediatamente, e um novo comprado é aberto quando nenhuma posição está ativa.
- **Sinal de venda**: o ponto do Parabolic SAR é impresso acima do preço de fechamento e +DI é menor ou igual a -DI. Qualquer exposição comprada é fechada antes de abrir uma negociação vendida.
- **Filtros de fim de tendência**: quando o SAR está acima do preço enquanto +DI é maior que -DI, a estratégia marca o fim de uma tendência vendida; quando o SAR está abaixo do preço enquanto +DI é menor ou igual a -DI, marca o fim de uma tendência comprada. Ambos os eventos forçam o fechamento de posições existentes sem abrir uma nova negociação.
- **Janela de negociação**: filtros de sessão opcionais restringem entradas ao intervalo `[StartHour, EndHour)`. Sinais fora da sessão ainda podem fechar negociações, mas novas entradas são ignoradas.

## Gestão de risco
- As distâncias de **stop-loss e take-profit** são medidas em pips (escaladas automaticamente para instrumentos de três e cinco dígitos). Os preços são normalizados para o passo do instrumento.
- O **trailing stop** é ativado depois que a posição ganha mais que `TrailingStopPips + TrailingStepPips`. Posições compradas trailam abaixo do último fechamento, vendidas acima. Quando o trailing está desabilitado, o nível de stop permanece fixo.
- **Saídas protetoras**: em cada candle finalizado a estratégia verifica os preços baixo/alto contra os níveis de stop-loss, take-profit e trailing. Qualquer violação fecha a posição e reinicia o rastreamento de risco.

## Parâmetros
- `StopLossPips` – distância ao stop protetor em pips.
- `TakeProfitPips` – distância ao alvo em pips.
- `TrailingStopPips` – distância do trailing stop em pips (definir 0 para desabilitar o trailing).
- `TrailingStepPips` – progresso adicional necessário antes que o trailing stop se mova; deve ser positivo quando o trailing está habilitado.
- `UseTradingHours` – habilita o filtro de janela de negociação.
- `StartHour` / `EndHour` – hora de início inclusiva e hora de fim exclusiva para entradas (hora da bolsa).
- `AdxPeriod` – período de suavização para o ADX, que controla a sensibilidade de +DI e -DI.
- `SarStart`, `SarIncrement`, `SarMaximum` – configurações de aceleração do Parabolic SAR correspondendo ao indicador original (0.02 / 0.02 / 0.2 por padrão).
- `CandleType` – período usado para assinaturas de candles e cálculos de indicadores.
- `Volume` – herdado de `Strategy`, representa o tamanho da ordem usada ao entrar em novas posições.

## Indicadores
- **Índice de Direção Médio (ADX)** fornece os componentes +DI e -DI usados para determinar a direção atual da tendência.
- **Parabolic SAR** define se a estrutura do mercado é altista ou baixista e fornece o nível do ponto para a lógica de trailing.

## Notas adicionais
- O tamanho do pip é calculado a partir das configurações do instrumento: para instrumentos com três e cinco decimais, o passo de preço é multiplicado por dez para corresponder à definição MQL de um pip.
- A estratégia sempre fecha posições existentes quando sinais opostos ou de fim aparecem antes de avaliar novas entradas, reproduzindo o fluxo de trabalho original do EA.
- Apenas a implementação em C# é fornecida; nenhuma versão ou pasta Python é criada, conforme solicitado.
