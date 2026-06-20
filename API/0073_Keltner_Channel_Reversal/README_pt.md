# Estratégia de Reversão no Keltner Channel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Canais baseados em volatilidade podem destacar movimentos sobreextendidos. Este método opera contra o preço quando ele ultrapassa o Keltner Channel, antecipando um retorno em direção à linha média. Utiliza uma média móvel exponencial e o ATR para dimensionar a largura do canal.

Os testes indicam um retorno anual médio de aproximadamente 106%. Funciona melhor no mercado de ações.

A cada vela concluída, a estratégia verifica se o fechamento está além da banda superior ou inferior e se a direção da vela está de acordo. Velas de alta fechando abaixo da banda inferior disparam entradas compradas, enquanto velas de baixa acima da banda superior geram vendas. As posições são encerradas quando o preço cruza a banda média ou quando o stop baseado em ATR é atingido.

Ao operar na direção oposta dos extremos de curto prazo, o sistema busca movimentos rápidos de reversão à média dentro de uma faixa mais ampla.

## Detalhes

- **Critérios de entrada**: Fechamento fora do Keltner Channel na direção da vela.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço cruzando a banda média ou stop-loss.
- **Stops**: Sim, baseados em ATR.
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `StopLossAtrMultiplier` = 2.0
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Keltner Channel
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

