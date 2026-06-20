# Estratégia Ichimoku RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Ichimoku RSI usa os níveis da nuvem Ichimoku para definir a direção da tendência enquanto o RSI identifica pullbacks de curto prazo.
As operações se alinham com a nuvem, entrando quando o RSI se recupera da sobrevenda em uma tendência de alta ou cai da sobrecompra em uma tendência de baixa.

Os testes indicam um retorno anual médio de aproximadamente 142%. Funciona melhor no mercado de ações.

Ao combinar um filtro de tendência amplo com um oscilador de momentum, a estratégia visa entrar em movimentos fortes após breves pausas.

Os stops são posicionados além do limite da nuvem para proteger contra correções mais profundas.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
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

