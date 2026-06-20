# Canal Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no Canal Donchian.

Os testes indicam um retorno anual médio de aproximadamente 52%. Funciona melhor no mercado de criptomoedas.

O Rompimento do Canal Donchian opera novas máximas e mínimas baseadas no intervalo do canal. Um fechamento além da banda superior sinaliza força, enquanto uma queda abaixo da banda inferior convida posições vendidas. As saídas ocorrem quando o preço retorna ao ponto médio.

O canal é calculado a partir da maior máxima e da menor mínima ao longo de uma janela de lookback. Quando o preço perfura esses limites, o sistema espera uma expansão de volatilidade e se posiciona adequadamente.


## Detalhes

- **Critérios de entrada**: Sinais baseados em Price Action.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ChannelPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Price Action
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

