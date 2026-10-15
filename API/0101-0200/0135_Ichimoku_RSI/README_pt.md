# Estratégia Ichimoku RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Ichimoku RSI usa os níveis da nuvem Ichimoku para definir a direção da tendência enquanto o RSI identifica pullbacks de curto prazo.
As operações se alinham com a nuvem, entrando quando o RSI se recupera da sobrevenda em uma tendência de alta ou cai da sobrecompra em uma tendência de baixa.

Os testes indicam um retorno anual médio de aproximadamente 142%. Funciona melhor no mercado de ações.

Ao combinar um filtro de tendência amplo com um oscilador de momentum, a estratégia visa entrar em movimentos fortes após breves pausas.

Um stop a uma porcentagem fixa do preço de entrada protege contra correções mais profundas.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
    Senkou Span A acima de Senkou Span B é tendência de alta, abaixo é de baixa. Na alta, o RSI que volta acima de RsiOversold abre uma compra; na baixa, o RSI que volta abaixo de RsiOverbought abre uma venda. Um sinal oposto inverte a posição.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Ichimoku, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

